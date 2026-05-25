using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using static Veldrith.Vk.VulkanDispatch;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkPipeline.
/// </summary>
internal unsafe class VkPipeline : Pipeline {

    /// <summary>
    /// Stores the device pipeline state used by this instance.
    /// </summary>
    private readonly global::Vortice.Vulkan.VkPipeline _devicePipeline;

    /// <summary>
    /// Stores the pipeline layout state used by this instance.
    /// </summary>
    private readonly VkPipelineLayout _pipelineLayout;

    /// <summary>
    /// Stores the render pass state used by this instance.
    /// </summary>
    private readonly VkRenderPass _renderPass;

    /// <summary>
    /// Stores the shader stages that are allowed to read push constants from this pipeline.
    /// </summary>
    private readonly VkShaderStageFlags _pushConstantStages;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkPipeline" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkPipeline(VkGraphicsDevice gd, ref GraphicsPipelineDescription description) : base(ref description) {
        this._gd = gd;
        this.IsComputePipeline = false;
        this.RefCount = new ResourceRefCount(this.DisposeCore);

        VkGraphicsPipelineCreateInfo pipelineCi = new VkGraphicsPipelineCreateInfo();

        // Blend State
        VkPipelineColorBlendStateCreateInfo blendStateCi = new VkPipelineColorBlendStateCreateInfo();
        int attachmentsCount = description.BlendState.AttachmentStates.Length;
        VkPipelineColorBlendAttachmentState* attachmentsPtr = stackalloc VkPipelineColorBlendAttachmentState[attachmentsCount];

        for (int i = 0; i < attachmentsCount; i++) {
            BlendAttachmentDescription vdDesc = description.BlendState.AttachmentStates[i];
            VkPipelineColorBlendAttachmentState attachmentState = new() {
                srcColorBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.SourceColorFactor),
                dstColorBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.DestinationColorFactor),
                colorBlendOp = VkFormats.VdToVkBlendOp(vdDesc.ColorFunction),
                srcAlphaBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.SourceAlphaFactor),
                dstAlphaBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.DestinationAlphaFactor),
                alphaBlendOp = VkFormats.VdToVkBlendOp(vdDesc.AlphaFunction),
                colorWriteMask = VkFormats.VdToVkColorWriteMask(vdDesc.ColorWriteMask.GetOrDefault()),
                blendEnable = vdDesc.BlendEnabled
            };
            attachmentsPtr[i] = attachmentState;
        }

        blendStateCi.attachmentCount = (uint)attachmentsCount;
        blendStateCi.pAttachments = attachmentsPtr;
        RgbaFloat blendFactor = description.BlendState.BlendFactor;
        blendStateCi.blendConstants[0] = blendFactor.R;
        blendStateCi.blendConstants[1] = blendFactor.G;
        blendStateCi.blendConstants[2] = blendFactor.B;
        blendStateCi.blendConstants[3] = blendFactor.A;

        pipelineCi.pColorBlendState = &blendStateCi;

        // Rasterizer State
        RasterizerStateDescription rsDesc = description.RasterizerState;
        VkPipelineRasterizationStateCreateInfo rsCi = new VkPipelineRasterizationStateCreateInfo();
        rsCi.cullMode = VkFormats.VdToVkCullMode(rsDesc.CullMode);
        rsCi.polygonMode = VkFormats.VdToVkPolygonMode(rsDesc.FillMode);
        rsCi.depthClampEnable = !rsDesc.DepthClipEnabled;
        rsCi.frontFace = rsDesc.FrontFace == FrontFace.Clockwise ? VkFrontFace.Clockwise : VkFrontFace.CounterClockwise;
        rsCi.lineWidth = 1f;

        pipelineCi.pRasterizationState = &rsCi;

        this.ScissorTestEnabled = rsDesc.ScissorTestEnabled;

        // Dynamic State
        VkPipelineDynamicStateCreateInfo dynamicStateCi = new VkPipelineDynamicStateCreateInfo();
        VkDynamicState* dynamicStates = stackalloc VkDynamicState[2];
        dynamicStates[0] = VkDynamicState.Viewport;
        dynamicStates[1] = VkDynamicState.Scissor;
        dynamicStateCi.dynamicStateCount = 2;
        dynamicStateCi.pDynamicStates = dynamicStates;

        pipelineCi.pDynamicState = &dynamicStateCi;

        // Depth Stencil State
        DepthStencilStateDescription vdDssDesc = description.DepthStencilState;
        VkPipelineDepthStencilStateCreateInfo dssCi = new VkPipelineDepthStencilStateCreateInfo();
        dssCi.depthWriteEnable = vdDssDesc.DepthWriteEnabled;
        dssCi.depthTestEnable = vdDssDesc.DepthTestEnabled;
        dssCi.depthCompareOp = VkFormats.VdToVkCompareOp(vdDssDesc.DepthComparison);
        dssCi.stencilTestEnable = vdDssDesc.StencilTestEnabled;

        dssCi.front.failOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.Fail);
        dssCi.front.passOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.Pass);
        dssCi.front.depthFailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.DepthFail);
        dssCi.front.compareOp = VkFormats.VdToVkCompareOp(vdDssDesc.StencilFront.Comparison);
        dssCi.front.compareMask = vdDssDesc.StencilReadMask;
        dssCi.front.writeMask = vdDssDesc.StencilWriteMask;
        dssCi.front.reference = vdDssDesc.StencilReference;

        dssCi.back.failOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.Fail);
        dssCi.back.passOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.Pass);
        dssCi.back.depthFailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.DepthFail);
        dssCi.back.compareOp = VkFormats.VdToVkCompareOp(vdDssDesc.StencilBack.Comparison);
        dssCi.back.compareMask = vdDssDesc.StencilReadMask;
        dssCi.back.writeMask = vdDssDesc.StencilWriteMask;
        dssCi.back.reference = vdDssDesc.StencilReference;

        pipelineCi.pDepthStencilState = &dssCi;

        // Multisample
        VkPipelineMultisampleStateCreateInfo multisampleCi = new VkPipelineMultisampleStateCreateInfo();
        VkSampleCountFlags vkSampleCount = VkFormats.VdToVkSampleCount(description.Outputs.SampleCount);
        multisampleCi.rasterizationSamples = vkSampleCount;
        multisampleCi.alphaToCoverageEnable = description.BlendState.AlphaToCoverageEnabled;

        pipelineCi.pMultisampleState = &multisampleCi;

        // Input Assembly
        VkPipelineInputAssemblyStateCreateInfo inputAssemblyCi = new VkPipelineInputAssemblyStateCreateInfo();
        inputAssemblyCi.topology = VkFormats.VdToVkPrimitiveTopology(description.PrimitiveTopology);

        pipelineCi.pInputAssemblyState = &inputAssemblyCi;

        // Vertex Input State
        VkPipelineVertexInputStateCreateInfo vertexInputCi = new VkPipelineVertexInputStateCreateInfo();

        VertexLayoutDescription[] inputDescriptions = description.ShaderSet.VertexLayouts;
        uint bindingCount = (uint)inputDescriptions.Length;
        uint attributeCount = 0;
        for (int i = 0; i < inputDescriptions.Length; i++) {
            attributeCount += (uint)inputDescriptions[i].Elements.Length;
        }

        VkVertexInputBindingDescription* bindingDescs = stackalloc VkVertexInputBindingDescription[(int)bindingCount];
        VkVertexInputAttributeDescription* attributeDescs = stackalloc VkVertexInputAttributeDescription[(int)attributeCount];

        int targetIndex = 0;
        int targetLocation = 0;

        for (int binding = 0; binding < inputDescriptions.Length; binding++) {
            VertexLayoutDescription inputDesc = inputDescriptions[binding];
            bindingDescs[binding] = new VkVertexInputBindingDescription {
                binding = (uint)binding,
                inputRate = inputDesc.InstanceStepRate != 0 ? VkVertexInputRate.Instance : VkVertexInputRate.Vertex,
                stride = inputDesc.Stride
            };

            uint currentOffset = 0;

            for (int location = 0; location < inputDesc.Elements.Length; location++) {
                VertexElementDescription inputElement = inputDesc.Elements[location];

                attributeDescs[targetIndex] = new VkVertexInputAttributeDescription {
                    format = VkFormats.VdToVkVertexElementFormat(inputElement.Format),
                    binding = (uint)binding,
                    location = (uint)(targetLocation + location),
                    offset = inputElement.Offset != 0 ? inputElement.Offset : currentOffset
                };

                targetIndex += 1;
                currentOffset += FormatSizeHelpers.GetSizeInBytes(inputElement.Format);
            }

            targetLocation += inputDesc.Elements.Length;
        }

        vertexInputCi.vertexBindingDescriptionCount = bindingCount;
        vertexInputCi.pVertexBindingDescriptions = bindingDescs;
        vertexInputCi.vertexAttributeDescriptionCount = attributeCount;
        vertexInputCi.pVertexAttributeDescriptions = attributeDescs;

        pipelineCi.pVertexInputState = &vertexInputCi;

        // Shader Stage

        VkSpecializationInfo specializationInfo = default;
        SpecializationConstant[] specDescs = description.ShaderSet.Specializations;

        if (specDescs != null) {
            uint specDataSize = 0;
            foreach (SpecializationConstant spec in specDescs) {
                specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
            }

            byte* fullSpecData = stackalloc byte[(int)specDataSize];
            int specializationCount = specDescs.Length;
            VkSpecializationMapEntry* mapEntries = stackalloc VkSpecializationMapEntry[specializationCount];
            uint specOffset = 0;

            for (int i = 0; i < specializationCount; i++) {
                ulong data = specDescs[i].Data;
                byte* srcData = (byte*)&data;
                uint dataSize = VkFormats.GetSpecializationConstantSize(specDescs[i].Type);
                Unsafe.CopyBlock(fullSpecData + specOffset, srcData, dataSize);
                mapEntries[i].constantID = specDescs[i].ID;
                mapEntries[i].offset = specOffset;
                mapEntries[i].size = dataSize;
                specOffset += dataSize;
            }

            specializationInfo.dataSize = specDataSize;
            specializationInfo.pData = fullSpecData;
            specializationInfo.mapEntryCount = (uint)specializationCount;
            specializationInfo.pMapEntries = mapEntries;
        }

        Shader[] shaders = description.ShaderSet.Shaders;
        VkPipelineShaderStageCreateInfo* stages = null;
        if (shaders.Length > 0) {
            byte* stageStorage = stackalloc byte[shaders.Length * Unsafe.SizeOf<VkPipelineShaderStageCreateInfo>()];
            stages = (VkPipelineShaderStageCreateInfo*)stageStorage;
        }
        int stageCount = 0;
        VkShaderStageFlags pushConstantStages = 0;

        foreach (Shader shader in shaders) {
            VkShader vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
            VkPipelineShaderStageCreateInfo stageCi = new VkPipelineShaderStageCreateInfo();
            stageCi.module = vkShader.ShaderModule;
            stageCi.stage = VkFormats.VdToVkShaderStages(shader.Stage);
            pushConstantStages |= stageCi.stage;
            // stageCI.pName = CommonStrings.main; // Meh
            stageCi.pName = new FixedUtf8String(shader.EntryPoint); // TODO: DONT ALLOCATE HERE
            stageCi.pSpecializationInfo = specDescs != null ? &specializationInfo : null;
            stages[stageCount++] = stageCi;
        }
        this._pushConstantStages = pushConstantStages;

        pipelineCi.stageCount = (uint)stageCount;
        pipelineCi.pStages = stages;

        // ViewportState
        VkPipelineViewportStateCreateInfo viewportStateCi = new VkPipelineViewportStateCreateInfo();
        viewportStateCi.viewportCount = 1;
        viewportStateCi.scissorCount = 1;

        pipelineCi.pViewportState = &viewportStateCi;

        // Pipeline Layout
        ResourceLayout[] resourceLayouts = description.ResourceLayouts;
        VkPipelineLayoutCreateInfo pipelineLayoutCi = new VkPipelineLayoutCreateInfo();
        pipelineLayoutCi.setLayoutCount = (uint)resourceLayouts.Length;
        VkDescriptorSetLayout* dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
        for (int i = 0; i < resourceLayouts.Length; i++) {
            dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
        }

        pipelineLayoutCi.pSetLayouts = dsls;
        VkPushConstantRange pushConstantRange = new() {
            stageFlags = this._pushConstantStages,
            offset = 0,
            size = this._gd.MaxPushConstantsSize
        };
        pipelineLayoutCi.pushConstantRangeCount = 1;
        pipelineLayoutCi.pPushConstantRanges = &pushConstantRange;

        this._gd.DeviceApi.vkCreatePipelineLayout(ref pipelineLayoutCi, null, out this._pipelineLayout);
        pipelineCi.layout = this._pipelineLayout;

        // Create fake RenderPass for compatibility.

        VkRenderPassCreateInfo renderPassCi = new VkRenderPassCreateInfo();
        OutputDescription outputDesc = description.Outputs;
        int colorAttachmentCount = outputDesc.ColorAttachments.Length;
        int totalAttachmentCount = colorAttachmentCount + (outputDesc.DepthAttachment != null ? 1 : 0);
        VkAttachmentDescription* attachments = null;
        if (totalAttachmentCount > 0) {
            byte* attachmentStorage = stackalloc byte[totalAttachmentCount * Unsafe.SizeOf<VkAttachmentDescription>()];
            attachments = (VkAttachmentDescription*)attachmentStorage;
        }

        VkAttachmentReference* colorAttachmentRefs = null;
        if (colorAttachmentCount > 0) {
            byte* colorAttachmentRefStorage = stackalloc byte[colorAttachmentCount * Unsafe.SizeOf<VkAttachmentReference>()];
            colorAttachmentRefs = (VkAttachmentReference*)colorAttachmentRefStorage;
        }

        // TODO: A huge portion of this next part is duplicated in VkFramebuffer.cs.

        for (int i = 0; i < outputDesc.ColorAttachments.Length; i++) {
            VkAttachmentDescription colorAttachmentDesc = new() {
                format = VkFormats.VdToVkPixelFormat(outputDesc.ColorAttachments[i].Format),
                samples = vkSampleCount,
                loadOp = VkAttachmentLoadOp.DontCare,
                storeOp = VkAttachmentStoreOp.Store,
                stencilLoadOp = VkAttachmentLoadOp.DontCare,
                stencilStoreOp = VkAttachmentStoreOp.DontCare,
                initialLayout = VkImageLayout.Undefined,
                finalLayout = VkImageLayout.ShaderReadOnlyOptimal
            };
            attachments[i] = colorAttachmentDesc;

            colorAttachmentRefs[i] = new VkAttachmentReference {
                attachment = (uint)i,
                layout = VkImageLayout.ColorAttachmentOptimal
            };
        }

        VkAttachmentDescription depthAttachmentDesc = new();
        VkAttachmentReference depthAttachmentRef = new();

        if (outputDesc.DepthAttachment is OutputAttachmentDescription depthAttachment) {
            PixelFormat depthFormat = depthAttachment.Format;
            bool hasStencil = FormatHelpers.IsStencilFormat(depthFormat);
            depthAttachmentDesc.format = VkFormats.VdToVkPixelFormat(depthAttachment.Format, true);
            depthAttachmentDesc.samples = vkSampleCount;
            depthAttachmentDesc.loadOp = VkAttachmentLoadOp.DontCare;
            depthAttachmentDesc.storeOp = VkAttachmentStoreOp.Store;
            depthAttachmentDesc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
            depthAttachmentDesc.stencilStoreOp = hasStencil ? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare;
            depthAttachmentDesc.initialLayout = VkImageLayout.Undefined;
            depthAttachmentDesc.finalLayout = VkImageLayout.DepthStencilAttachmentOptimal;

            depthAttachmentRef.attachment = (uint)outputDesc.ColorAttachments.Length;
            depthAttachmentRef.layout = VkImageLayout.DepthStencilAttachmentOptimal;
        }

        VkSubpassDescription subpass = new() {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = (uint)outputDesc.ColorAttachments.Length,
            pColorAttachments = colorAttachmentRefs
        };

        if (outputDesc.DepthAttachment != null) {
            subpass.pDepthStencilAttachment = &depthAttachmentRef;
            attachments[colorAttachmentCount] = depthAttachmentDesc;
        }

        VkSubpassDependency subpassDependency = new() {
            srcSubpass = SubpassExternal,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite
        };

        renderPassCi.attachmentCount = (uint)totalAttachmentCount;
        renderPassCi.pAttachments = attachments;
        renderPassCi.subpassCount = 1;
        renderPassCi.pSubpasses = &subpass;
        renderPassCi.dependencyCount = 1;
        renderPassCi.pDependencies = &subpassDependency;

        VkResult creationResult = this._gd.DeviceApi.vkCreateRenderPass(ref renderPassCi, null, out this._renderPass);
        CheckResult(creationResult);

        pipelineCi.renderPass = this._renderPass;

        VkResult result;
        fixed (global::Vortice.Vulkan.VkPipeline* pipelinePtr = &this._devicePipeline) {
            result = this._gd.DeviceApi.vkCreateGraphicsPipelines(this._gd.PipelineCache, 1, &pipelineCi, null, pipelinePtr);
        }
        CheckResult(result);

        this.ResourceSetCount = (uint)description.ResourceLayouts.Length;
        this.DynamicOffsetsCount = 0;
        foreach (ResourceLayout layout in description.ResourceLayouts) {
            this.DynamicOffsetsCount += layout.DynamicBufferCount;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VkPipeline" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkPipeline(VkGraphicsDevice gd, ref ComputePipelineDescription description) : base(ref description) {
        this._gd = gd;
        this.IsComputePipeline = true;
        this.RefCount = new ResourceRefCount(this.DisposeCore);

        VkComputePipelineCreateInfo pipelineCi = new VkComputePipelineCreateInfo();

        // Pipeline Layout
        ResourceLayout[] resourceLayouts = description.ResourceLayouts;
        VkPipelineLayoutCreateInfo pipelineLayoutCi = new VkPipelineLayoutCreateInfo();
        pipelineLayoutCi.setLayoutCount = (uint)resourceLayouts.Length;
        VkDescriptorSetLayout* dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
        for (int i = 0; i < resourceLayouts.Length; i++) {
            dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
        }

        pipelineLayoutCi.pSetLayouts = dsls;
        VkPushConstantRange pushConstantRange = new() {
            stageFlags = VkShaderStageFlags.Compute,
            offset = 0,
            size = this._gd.MaxPushConstantsSize
        };
        pipelineLayoutCi.pushConstantRangeCount = 1;
        pipelineLayoutCi.pPushConstantRanges = &pushConstantRange;

        this._gd.DeviceApi.vkCreatePipelineLayout(ref pipelineLayoutCi, null, out this._pipelineLayout);
        pipelineCi.layout = this._pipelineLayout;

        // Shader Stage

        VkSpecializationInfo specializationInfo;
        SpecializationConstant[] specDescs = description.Specializations;

        if (specDescs != null) {
            uint specDataSize = 0;
            foreach (SpecializationConstant spec in specDescs) {
                specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
            }

            byte* fullSpecData = stackalloc byte[(int)specDataSize];
            int specializationCount = specDescs.Length;
            VkSpecializationMapEntry* mapEntries = stackalloc VkSpecializationMapEntry[specializationCount];
            uint specOffset = 0;

            for (int i = 0; i < specializationCount; i++) {
                ulong data = specDescs[i].Data;
                byte* srcData = (byte*)&data;
                uint dataSize = VkFormats.GetSpecializationConstantSize(specDescs[i].Type);
                Unsafe.CopyBlock(fullSpecData + specOffset, srcData, dataSize);
                mapEntries[i].constantID = specDescs[i].ID;
                mapEntries[i].offset = specOffset;
                mapEntries[i].size = dataSize;
                specOffset += dataSize;
            }

            specializationInfo.dataSize = specDataSize;
            specializationInfo.pData = fullSpecData;
            specializationInfo.mapEntryCount = (uint)specializationCount;
            specializationInfo.pMapEntries = mapEntries;
        }

        Shader shader = description.ComputeShader;
        VkShader vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
        VkPipelineShaderStageCreateInfo stageCi = new VkPipelineShaderStageCreateInfo();
        stageCi.module = vkShader.ShaderModule;
        stageCi.stage = VkFormats.VdToVkShaderStages(shader.Stage);
        this._pushConstantStages = stageCi.stage;
        stageCi.pName = CommonStrings.Main; // Meh
        stageCi.pSpecializationInfo = &specializationInfo;
        pipelineCi.stage = stageCi;

        VkResult result;
        fixed (global::Vortice.Vulkan.VkPipeline* pipelinePtr = &this._devicePipeline) {
            result = this._gd.DeviceApi.vkCreateComputePipelines(this._gd.PipelineCache, 1, &pipelineCi, null, pipelinePtr);
        }
        CheckResult(result);

        this.ResourceSetCount = (uint)description.ResourceLayouts.Length;
        this.DynamicOffsetsCount = 0;
        foreach (ResourceLayout layout in description.ResourceLayouts) {
            this.DynamicOffsetsCount += layout.DynamicBufferCount;
        }
    }

    /// <summary>
    /// Stores the device pipeline state used by this instance.
    /// </summary>
    public global::Vortice.Vulkan.VkPipeline DevicePipeline => this._devicePipeline;

    /// <summary>
    /// Stores the pipeline layout state used by this instance.
    /// </summary>
    public VkPipelineLayout PipelineLayout => this._pipelineLayout;

    /// <summary>
    /// Gets or sets ResourceSetCount.
    /// </summary>
    public uint ResourceSetCount { get; }

    /// <summary>
    /// Gets or sets DynamicOffsetsCount.
    /// </summary>
    public uint DynamicOffsetsCount { get; }

    /// <summary>
    /// Gets the shader stages that can consume push constants for this pipeline.
    /// </summary>
    public VkShaderStageFlags PushConstantStages => this._pushConstantStages;

    /// <summary>
    /// Gets or sets ScissorTestEnabled.
    /// </summary>
    public bool ScissorTestEnabled { get; }

    /// <summary>
    /// Gets or sets IsComputePipeline.
    /// </summary>
    public override bool IsComputePipeline { get; }

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this._gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            this._gd.DeviceApi.vkDestroyPipelineLayout(this._pipelineLayout, null);
            this._gd.DeviceApi.vkDestroyPipeline(this._devicePipeline, null);
            if (!this.IsComputePipeline) {
                this._gd.DeviceApi.vkDestroyRenderPass(this._renderPass, null);
            }
        }
    }
}
