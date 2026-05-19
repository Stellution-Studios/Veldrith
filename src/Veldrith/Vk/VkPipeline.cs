using System.Runtime.CompilerServices;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkPipeline.
/// </summary>
internal unsafe class VkPipeline : Pipeline {

    /// <summary>
    /// Stores the device pipeline state used by this instance.
    /// </summary>
    private readonly Vulkan.VkPipeline _devicePipeline;

    /// <summary>
    /// Stores the pipeline layout state used by this instance.
    /// </summary>
    private readonly VkPipelineLayout _pipelineLayout;

    /// <summary>
    /// Stores the render pass state used by this instance.
    /// </summary>
    private readonly VkRenderPass _renderPass;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice gd;

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
        this.gd = gd;
        this.IsComputePipeline = false;
        this.RefCount = new ResourceRefCount(this.DisposeCore);

        VkGraphicsPipelineCreateInfo pipelineCi = VkGraphicsPipelineCreateInfo.New();

        // Blend State
        VkPipelineColorBlendStateCreateInfo blendStateCi = VkPipelineColorBlendStateCreateInfo.New();
        int attachmentsCount = description.BlendState.AttachmentStates.Length;
        VkPipelineColorBlendAttachmentState* attachmentsPtr
            = stackalloc VkPipelineColorBlendAttachmentState[attachmentsCount];

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
        blendStateCi.blendConstants_0 = blendFactor.R;
        blendStateCi.blendConstants_1 = blendFactor.G;
        blendStateCi.blendConstants_2 = blendFactor.B;
        blendStateCi.blendConstants_3 = blendFactor.A;

        pipelineCi.pColorBlendState = &blendStateCi;

        // Rasterizer State
        RasterizerStateDescription rsDesc = description.RasterizerState;
        VkPipelineRasterizationStateCreateInfo rsCi = VkPipelineRasterizationStateCreateInfo.New();
        rsCi.cullMode = VkFormats.VdToVkCullMode(rsDesc.CullMode);
        rsCi.polygonMode = VkFormats.VdToVkPolygonMode(rsDesc.FillMode);
        rsCi.depthClampEnable = !rsDesc.DepthClipEnabled;
        rsCi.frontFace = rsDesc.FrontFace == FrontFace.Clockwise ? VkFrontFace.Clockwise : VkFrontFace.CounterClockwise;
        rsCi.lineWidth = 1f;

        pipelineCi.pRasterizationState = &rsCi;

        this.ScissorTestEnabled = rsDesc.ScissorTestEnabled;

        // Dynamic State
        VkPipelineDynamicStateCreateInfo dynamicStateCi = VkPipelineDynamicStateCreateInfo.New();
        VkDynamicState* dynamicStates = stackalloc VkDynamicState[2];
        dynamicStates[0] = VkDynamicState.Viewport;
        dynamicStates[1] = VkDynamicState.Scissor;
        dynamicStateCi.dynamicStateCount = 2;
        dynamicStateCi.pDynamicStates = dynamicStates;

        pipelineCi.pDynamicState = &dynamicStateCi;

        // Depth Stencil State
        DepthStencilStateDescription vdDssDesc = description.DepthStencilState;
        VkPipelineDepthStencilStateCreateInfo dssCi = VkPipelineDepthStencilStateCreateInfo.New();
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
        VkPipelineMultisampleStateCreateInfo multisampleCi = VkPipelineMultisampleStateCreateInfo.New();
        VkSampleCountFlags vkSampleCount = VkFormats.VdToVkSampleCount(description.Outputs.SampleCount);
        multisampleCi.rasterizationSamples = vkSampleCount;
        multisampleCi.alphaToCoverageEnable = description.BlendState.AlphaToCoverageEnabled;

        pipelineCi.pMultisampleState = &multisampleCi;

        // Input Assembly
        VkPipelineInputAssemblyStateCreateInfo inputAssemblyCi = VkPipelineInputAssemblyStateCreateInfo.New();
        inputAssemblyCi.topology = VkFormats.VdToVkPrimitiveTopology(description.PrimitiveTopology);

        pipelineCi.pInputAssemblyState = &inputAssemblyCi;

        // Vertex Input State
        VkPipelineVertexInputStateCreateInfo vertexInputCi = VkPipelineVertexInputStateCreateInfo.New();

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

        VkSpecializationInfo specializationInfo;
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
        StackList<VkPipelineShaderStageCreateInfo> stages = new();

        foreach (Shader shader in shaders) {
            VkShader vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
            VkPipelineShaderStageCreateInfo stageCi = VkPipelineShaderStageCreateInfo.New();
            stageCi.module = vkShader.ShaderModule;
            stageCi.stage = VkFormats.VdToVkShaderStages(shader.Stage);
            // stageCI.pName = CommonStrings.main; // Meh
            stageCi.pName = new FixedUtf8String(shader.EntryPoint); // TODO: DONT ALLOCATE HERE
            stageCi.pSpecializationInfo = &specializationInfo;
            stages.Add(stageCi);
        }

        pipelineCi.stageCount = stages.Count;
        pipelineCi.pStages = (VkPipelineShaderStageCreateInfo*)stages.Data;

        // ViewportState
        VkPipelineViewportStateCreateInfo viewportStateCi = VkPipelineViewportStateCreateInfo.New();
        viewportStateCi.viewportCount = 1;
        viewportStateCi.scissorCount = 1;

        pipelineCi.pViewportState = &viewportStateCi;

        // Pipeline Layout
        ResourceLayout[] resourceLayouts = description.ResourceLayouts;
        VkPipelineLayoutCreateInfo pipelineLayoutCi = VkPipelineLayoutCreateInfo.New();
        pipelineLayoutCi.setLayoutCount = (uint)resourceLayouts.Length;
        VkDescriptorSetLayout* dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
        for (int i = 0; i < resourceLayouts.Length; i++) {
            dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
        }

        pipelineLayoutCi.pSetLayouts = dsls;

        vkCreatePipelineLayout(this.gd.Device, ref pipelineLayoutCi, null, out this._pipelineLayout);
        pipelineCi.layout = this._pipelineLayout;

        // Create fake RenderPass for compatibility.

        VkRenderPassCreateInfo renderPassCi = VkRenderPassCreateInfo.New();
        OutputDescription outputDesc = description.Outputs;
        StackList<VkAttachmentDescription, Size512Bytes> attachments = new();

        // TODO: A huge portion of this next part is duplicated in VkFramebuffer.cs.

        StackList<VkAttachmentDescription> colorAttachmentDescs = new();
        StackList<VkAttachmentReference> colorAttachmentRefs = new();

        for (uint i = 0; i < outputDesc.ColorAttachments.Length; i++) {
            colorAttachmentDescs[i].format = VkFormats.VdToVkPixelFormat(outputDesc.ColorAttachments[i].Format);
            colorAttachmentDescs[i].samples = vkSampleCount;
            colorAttachmentDescs[i].loadOp = VkAttachmentLoadOp.DontCare;
            colorAttachmentDescs[i].storeOp = VkAttachmentStoreOp.Store;
            colorAttachmentDescs[i].stencilLoadOp = VkAttachmentLoadOp.DontCare;
            colorAttachmentDescs[i].stencilStoreOp = VkAttachmentStoreOp.DontCare;
            colorAttachmentDescs[i].initialLayout = VkImageLayout.Undefined;
            colorAttachmentDescs[i].finalLayout = VkImageLayout.ShaderReadOnlyOptimal;
            attachments.Add(colorAttachmentDescs[i]);

            colorAttachmentRefs[i].attachment = i;
            colorAttachmentRefs[i].layout = VkImageLayout.ColorAttachmentOptimal;
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
            pColorAttachments = (VkAttachmentReference*)colorAttachmentRefs.Data
        };
        for (int i = 0; i < colorAttachmentDescs.Count; i++) {
            attachments.Add(colorAttachmentDescs[i]);
        }

        if (outputDesc.DepthAttachment != null) {
            subpass.pDepthStencilAttachment = &depthAttachmentRef;
            attachments.Add(depthAttachmentDesc);
        }

        VkSubpassDependency subpassDependency = new() {
            srcSubpass = SubpassExternal,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite
        };

        renderPassCi.attachmentCount = attachments.Count;
        renderPassCi.pAttachments = (VkAttachmentDescription*)attachments.Data;
        renderPassCi.subpassCount = 1;
        renderPassCi.pSubpasses = &subpass;
        renderPassCi.dependencyCount = 1;
        renderPassCi.pDependencies = &subpassDependency;

        VkResult creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out this._renderPass);
        CheckResult(creationResult);

        pipelineCi.renderPass = this._renderPass;

        VkResult result = vkCreateGraphicsPipelines(this.gd.Device, VkPipelineCache.Null, 1, ref pipelineCi, null, out this._devicePipeline);
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
        this.gd = gd;
        this.IsComputePipeline = true;
        this.RefCount = new ResourceRefCount(this.DisposeCore);

        VkComputePipelineCreateInfo pipelineCi = VkComputePipelineCreateInfo.New();

        // Pipeline Layout
        ResourceLayout[] resourceLayouts = description.ResourceLayouts;
        VkPipelineLayoutCreateInfo pipelineLayoutCi = VkPipelineLayoutCreateInfo.New();
        pipelineLayoutCi.setLayoutCount = (uint)resourceLayouts.Length;
        VkDescriptorSetLayout* dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
        for (int i = 0; i < resourceLayouts.Length; i++) {
            dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
        }

        pipelineLayoutCi.pSetLayouts = dsls;

        vkCreatePipelineLayout(this.gd.Device, ref pipelineLayoutCi, null, out this._pipelineLayout);
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
        VkPipelineShaderStageCreateInfo stageCi = VkPipelineShaderStageCreateInfo.New();
        stageCi.module = vkShader.ShaderModule;
        stageCi.stage = VkFormats.VdToVkShaderStages(shader.Stage);
        stageCi.pName = CommonStrings.Main; // Meh
        stageCi.pSpecializationInfo = &specializationInfo;
        pipelineCi.stage = stageCi;

        VkResult result = vkCreateComputePipelines(this.gd.Device, VkPipelineCache.Null, 1, ref pipelineCi, null, out this._devicePipeline);
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
    public Vulkan.VkPipeline DevicePipeline => this._devicePipeline;

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
            this.gd.SetResourceName(this, value);
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
            vkDestroyPipelineLayout(this.gd.Device, this._pipelineLayout, null);
            vkDestroyPipeline(this.gd.Device, this._devicePipeline, null);
            if (!this.IsComputePipeline) {
                vkDestroyRenderPass(this.gd.Device, this._renderPass, null);
            }
        }
    }
}