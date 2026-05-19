using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkPipeline : Pipeline
    {
        public Vulkan.VkPipeline DevicePipeline => _devicePipeline;

        public VkPipelineLayout PipelineLayout => _pipelineLayout;

        public uint ResourceSetCount { get; }
        public uint DynamicOffsetsCount { get; }
        public bool ScissorTestEnabled { get; }

        public override bool IsComputePipeline { get; }

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => _destroyed;

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly Vulkan.VkPipeline _devicePipeline;
        private readonly VkPipelineLayout _pipelineLayout;
        private readonly VkRenderPass _renderPass;
        private bool _destroyed;
        private string _name;

        public VkPipeline(VkGraphicsDevice gd, ref GraphicsPipelineDescription description)
            : base(ref description)
        {
            this.gd = gd;
            IsComputePipeline = false;
            RefCount = new ResourceRefCount(disposeCore);

            var pipelineCi = VkGraphicsPipelineCreateInfo.New();

            // Blend State
            var blendStateCi = VkPipelineColorBlendStateCreateInfo.New();
            int attachmentsCount = description.BlendState.AttachmentStates.Length;
            var attachmentsPtr
                = stackalloc VkPipelineColorBlendAttachmentState[attachmentsCount];

            for (int i = 0; i < attachmentsCount; i++)
            {
                var vdDesc = description.BlendState.AttachmentStates[i];
                var attachmentState = new VkPipelineColorBlendAttachmentState
                {
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
            var blendFactor = description.BlendState.BlendFactor;
            blendStateCi.blendConstants_0 = blendFactor.R;
            blendStateCi.blendConstants_1 = blendFactor.G;
            blendStateCi.blendConstants_2 = blendFactor.B;
            blendStateCi.blendConstants_3 = blendFactor.A;

            pipelineCi.pColorBlendState = &blendStateCi;

            // Rasterizer State
            var rsDesc = description.RasterizerState;
            var rsCi = VkPipelineRasterizationStateCreateInfo.New();
            rsCi.cullMode = VkFormats.VdToVkCullMode(rsDesc.CullMode);
            rsCi.polygonMode = VkFormats.VdToVkPolygonMode(rsDesc.FillMode);
            rsCi.depthClampEnable = !rsDesc.DepthClipEnabled;
            rsCi.frontFace = rsDesc.FrontFace == FrontFace.Clockwise ? VkFrontFace.Clockwise : VkFrontFace.CounterClockwise;
            rsCi.lineWidth = 1f;

            pipelineCi.pRasterizationState = &rsCi;

            ScissorTestEnabled = rsDesc.ScissorTestEnabled;

            // Dynamic State
            var dynamicStateCi = VkPipelineDynamicStateCreateInfo.New();
            var dynamicStates = stackalloc VkDynamicState[2];
            dynamicStates[0] = VkDynamicState.Viewport;
            dynamicStates[1] = VkDynamicState.Scissor;
            dynamicStateCi.dynamicStateCount = 2;
            dynamicStateCi.pDynamicStates = dynamicStates;

            pipelineCi.pDynamicState = &dynamicStateCi;

            // Depth Stencil State
            var vdDssDesc = description.DepthStencilState;
            var dssCi = VkPipelineDepthStencilStateCreateInfo.New();
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
            var multisampleCi = VkPipelineMultisampleStateCreateInfo.New();
            var vkSampleCount = VkFormats.VdToVkSampleCount(description.Outputs.SampleCount);
            multisampleCi.rasterizationSamples = vkSampleCount;
            multisampleCi.alphaToCoverageEnable = description.BlendState.AlphaToCoverageEnabled;

            pipelineCi.pMultisampleState = &multisampleCi;

            // Input Assembly
            var inputAssemblyCi = VkPipelineInputAssemblyStateCreateInfo.New();
            inputAssemblyCi.topology = VkFormats.VdToVkPrimitiveTopology(description.PrimitiveTopology);

            pipelineCi.pInputAssemblyState = &inputAssemblyCi;

            // Vertex Input State
            var vertexInputCi = VkPipelineVertexInputStateCreateInfo.New();

            var inputDescriptions = description.ShaderSet.VertexLayouts;
            uint bindingCount = (uint)inputDescriptions.Length;
            uint attributeCount = 0;
            for (int i = 0; i < inputDescriptions.Length; i++) attributeCount += (uint)inputDescriptions[i].Elements.Length;
            var bindingDescs = stackalloc VkVertexInputBindingDescription[(int)bindingCount];
            var attributeDescs = stackalloc VkVertexInputAttributeDescription[(int)attributeCount];

            int targetIndex = 0;
            int targetLocation = 0;

            for (int binding = 0; binding < inputDescriptions.Length; binding++)
            {
                var inputDesc = inputDescriptions[binding];
                bindingDescs[binding] = new VkVertexInputBindingDescription
                {
                    binding = (uint)binding,
                    inputRate = inputDesc.InstanceStepRate != 0 ? VkVertexInputRate.Instance : VkVertexInputRate.Vertex,
                    stride = inputDesc.Stride
                };

                uint currentOffset = 0;

                for (int location = 0; location < inputDesc.Elements.Length; location++)
                {
                    var inputElement = inputDesc.Elements[location];

                    attributeDescs[targetIndex] = new VkVertexInputAttributeDescription
                    {
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
            var specDescs = description.ShaderSet.Specializations;

            if (specDescs != null)
            {
                uint specDataSize = 0;
                foreach (var spec in specDescs) specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
                byte* fullSpecData = stackalloc byte[(int)specDataSize];
                int specializationCount = specDescs.Length;
                var mapEntries = stackalloc VkSpecializationMapEntry[specializationCount];
                uint specOffset = 0;

                for (int i = 0; i < specializationCount; i++)
                {
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

            var shaders = description.ShaderSet.Shaders;
            var stages = new StackList<VkPipelineShaderStageCreateInfo>();

            foreach (var shader in shaders)
            {
                var vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
                var stageCi = VkPipelineShaderStageCreateInfo.New();
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
            var viewportStateCi = VkPipelineViewportStateCreateInfo.New();
            viewportStateCi.viewportCount = 1;
            viewportStateCi.scissorCount = 1;

            pipelineCi.pViewportState = &viewportStateCi;

            // Pipeline Layout
            var resourceLayouts = description.ResourceLayouts;
            var pipelineLayoutCi = VkPipelineLayoutCreateInfo.New();
            pipelineLayoutCi.setLayoutCount = (uint)resourceLayouts.Length;
            var dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
            for (int i = 0; i < resourceLayouts.Length; i++) dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
            pipelineLayoutCi.pSetLayouts = dsls;

            vkCreatePipelineLayout(this.gd.Device, ref pipelineLayoutCi, null, out _pipelineLayout);
            pipelineCi.layout = _pipelineLayout;

            // Create fake RenderPass for compatibility.

            var renderPassCi = VkRenderPassCreateInfo.New();
            var outputDesc = description.Outputs;
            var attachments = new StackList<VkAttachmentDescription, Size512Bytes>();

            // TODO: A huge portion of this next part is duplicated in VkFramebuffer.cs.

            var colorAttachmentDescs = new StackList<VkAttachmentDescription>();
            var colorAttachmentRefs = new StackList<VkAttachmentReference>();

            for (uint i = 0; i < outputDesc.ColorAttachments.Length; i++)
            {
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

            var depthAttachmentDesc = new VkAttachmentDescription();
            var depthAttachmentRef = new VkAttachmentReference();

            if (outputDesc.DepthAttachment is OutputAttachmentDescription depthAttachment)
            {
                var depthFormat = depthAttachment.Format;
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

            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics,
                colorAttachmentCount = (uint)outputDesc.ColorAttachments.Length,
                pColorAttachments = (VkAttachmentReference*)colorAttachmentRefs.Data
            };
            for (int i = 0; i < colorAttachmentDescs.Count; i++) attachments.Add(colorAttachmentDescs[i]);

            if (outputDesc.DepthAttachment != null)
            {
                subpass.pDepthStencilAttachment = &depthAttachmentRef;
                attachments.Add(depthAttachmentDesc);
            }

            var subpassDependency = new VkSubpassDependency
            {
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

            var creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out _renderPass);
            CheckResult(creationResult);

            pipelineCi.renderPass = _renderPass;

            var result = vkCreateGraphicsPipelines(this.gd.Device, VkPipelineCache.Null, 1, ref pipelineCi, null, out _devicePipeline);
            CheckResult(result);

            ResourceSetCount = (uint)description.ResourceLayouts.Length;
            DynamicOffsetsCount = 0;
            foreach (ResourceLayout layout in description.ResourceLayouts)
                DynamicOffsetsCount += layout.DynamicBufferCount;
        }

        public VkPipeline(VkGraphicsDevice gd, ref ComputePipelineDescription description)
            : base(ref description)
        {
            this.gd = gd;
            IsComputePipeline = true;
            RefCount = new ResourceRefCount(disposeCore);

            var pipelineCi = VkComputePipelineCreateInfo.New();

            // Pipeline Layout
            var resourceLayouts = description.ResourceLayouts;
            var pipelineLayoutCi = VkPipelineLayoutCreateInfo.New();
            pipelineLayoutCi.setLayoutCount = (uint)resourceLayouts.Length;
            var dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
            for (int i = 0; i < resourceLayouts.Length; i++) dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
            pipelineLayoutCi.pSetLayouts = dsls;

            vkCreatePipelineLayout(this.gd.Device, ref pipelineLayoutCi, null, out _pipelineLayout);
            pipelineCi.layout = _pipelineLayout;

            // Shader Stage

            VkSpecializationInfo specializationInfo;
            var specDescs = description.Specializations;

            if (specDescs != null)
            {
                uint specDataSize = 0;
                foreach (var spec in specDescs) specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
                byte* fullSpecData = stackalloc byte[(int)specDataSize];
                int specializationCount = specDescs.Length;
                var mapEntries = stackalloc VkSpecializationMapEntry[specializationCount];
                uint specOffset = 0;

                for (int i = 0; i < specializationCount; i++)
                {
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

            var shader = description.ComputeShader;
            var vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
            var stageCi = VkPipelineShaderStageCreateInfo.New();
            stageCi.module = vkShader.ShaderModule;
            stageCi.stage = VkFormats.VdToVkShaderStages(shader.Stage);
            stageCi.pName = CommonStrings.Main; // Meh
            stageCi.pSpecializationInfo = &specializationInfo;
            pipelineCi.stage = stageCi;

            var result = vkCreateComputePipelines(
                this.gd.Device,
                VkPipelineCache.Null,
                1,
                ref pipelineCi,
                null,
                out _devicePipeline);
            CheckResult(result);

            ResourceSetCount = (uint)description.ResourceLayouts.Length;
            DynamicOffsetsCount = 0;
            foreach (ResourceLayout layout in description.ResourceLayouts)
                DynamicOffsetsCount += layout.DynamicBufferCount;
        }

        #region Disposal

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        #endregion

        private void disposeCore()
        {
            if (!_destroyed)
            {
                _destroyed = true;
                vkDestroyPipelineLayout(gd.Device, _pipelineLayout, null);
                vkDestroyPipeline(gd.Device, _devicePipeline, null);
                if (!IsComputePipeline) vkDestroyRenderPass(gd.Device, _renderPass, null);
            }
        }
    }
}
