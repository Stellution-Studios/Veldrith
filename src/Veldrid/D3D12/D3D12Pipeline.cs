using System;
using System.Collections.Generic;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrid.D3D12
{
    internal sealed class D3D12Pipeline : Pipeline
    {
        internal readonly struct RootBindingInfo
        {
            public RootBindingInfo(uint rootParameterIndex, ResourceKind kind, bool descriptorTable)
            {
                RootParameterIndex = rootParameterIndex;
                Kind = kind;
                DescriptorTable = descriptorTable;
            }

            public uint RootParameterIndex { get; }
            public ResourceKind Kind { get; }
            public bool DescriptorTable { get; }
        }

        private readonly D3D12GraphicsDevice gd;
        private readonly Dictionary<(uint Set, uint Element), RootBindingInfo> rootBindings = new();
        private bool disposed;
        private string name;

        public D3D12Pipeline(D3D12GraphicsDevice gd, ref GraphicsPipelineDescription description)
            : base(ref description)
        {
            this.gd = gd;
            IsComputePipeline = false;
            PrimitiveTopology = D3D12Formats.ToD3DPrimitiveTopology(description.PrimitiveTopology);
            PrimitiveTopologyType = D3D12Formats.ToPrimitiveTopologyType(description.PrimitiveTopology);
            VertexStrides = new uint[description.ShaderSet.VertexLayouts.Length];
            for (uint i = 0; i < VertexStrides.Length; i++)
            {
                VertexStrides[i] = description.ShaderSet.VertexLayouts[i].Stride;
            }

            createRootSignature(description.ResourceLayouts);
            createGraphicsPipelineState(ref description);
        }

        public D3D12Pipeline(D3D12GraphicsDevice gd, ref ComputePipelineDescription description)
            : base(ref description)
        {
            this.gd = gd;
            IsComputePipeline = true;
            createRootSignature(description.ResourceLayouts);
            createComputePipelineState(ref description);
        }

        public override bool IsComputePipeline { get; }
        public ID3D12PipelineState PipelineState { get; private set; }
        public ID3D12RootSignature RootSignature { get; private set; }
        public Vortice.Direct3D.PrimitiveTopology PrimitiveTopology { get; }
        public PrimitiveTopologyType PrimitiveTopologyType { get; }
        public uint[] VertexStrides { get; } = Array.Empty<uint>();
        public override bool IsDisposed => disposed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
            }
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }

            PipelineState?.Dispose();
            RootSignature?.Dispose();
            disposed = true;
        }

        internal bool TryGetGraphicsRootBinding(uint set, uint element, out RootBindingInfo bindingInfo)
            => rootBindings.TryGetValue((set, element), out bindingInfo);

        internal bool TryGetComputeRootBinding(uint set, uint element, out RootBindingInfo bindingInfo)
            => rootBindings.TryGetValue((set, element), out bindingInfo);

        private void createRootSignature(ResourceLayout[] resourceLayouts)
        {
            var rootParameters = new List<RootParameter>();
            rootBindings.Clear();

            if (resourceLayouts != null)
            {
                for (uint setIndex = 0; setIndex < resourceLayouts.Length; setIndex++)
                {
                    var resourceLayout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
                    ResourceLayoutElementDescription[] elements = resourceLayout.Elements;
                    for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++)
                    {
                        ResourceLayoutElementDescription element = elements[elementIndex];
                        bool descriptorTable = usesDescriptorTable(element.Kind);
                        RootParameter rootParameter;
                        if (descriptorTable)
                        {
                            DescriptorRangeType rangeType = getDescriptorRangeType(element.Kind);
                            var descriptorRange = new DescriptorRange(rangeType, 1, elementIndex, setIndex, 0);
                            var descriptorTableInfo = new RootDescriptorTable(new[] { descriptorRange });
                            rootParameter = new RootParameter(descriptorTableInfo, toShaderVisibility(element.Stages));
                        }
                        else
                        {
                            RootParameterType parameterType = getRootParameterType(element.Kind);
                            var rootDescriptor = new RootDescriptor(elementIndex, setIndex);
                            rootParameter = new RootParameter(parameterType, rootDescriptor, toShaderVisibility(element.Stages));
                        }

                        uint rootParameterIndex = (uint)rootParameters.Count;
                        rootParameters.Add(rootParameter);
                        rootBindings[(setIndex, elementIndex)] = new RootBindingInfo(rootParameterIndex, element.Kind, descriptorTable);
                    }
                }
            }

            RootSignatureFlags rootSignatureFlags = IsComputePipeline
                ? RootSignatureFlags.None
                : RootSignatureFlags.AllowInputAssemblerInputLayout;

            var rootSignatureDescription = new RootSignatureDescription(
                rootSignatureFlags,
                rootParameters.ToArray(),
                Array.Empty<StaticSamplerDescription>());
            RootSignature = gd.Device.CreateRootSignature(in rootSignatureDescription, RootSignatureVersion.Version1);
        }

        private void createComputePipelineState(ref ComputePipelineDescription description)
        {
            var d3d12Shader = Util.AssertSubtype<Shader, D3D12Shader>(description.ComputeShader);
            var psoDescription = new ComputePipelineStateDescription
            {
                RootSignature = RootSignature,
                ComputeShader = d3d12Shader.ShaderBytes
            };

            PipelineState = gd.Device.CreateComputePipelineState(psoDescription);
        }

        private static RootParameterType getRootParameterType(ResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceKind.UniformBuffer:
                    return RootParameterType.ConstantBufferView;
                case ResourceKind.StructuredBufferReadOnly:
                    return RootParameterType.ShaderResourceView;
                case ResourceKind.StructuredBufferReadWrite:
                    return RootParameterType.UnorderedAccessView;
                case ResourceKind.TextureReadOnly:
                case ResourceKind.TextureReadWrite:
                case ResourceKind.Sampler:
                    throw new VeldridException("Texture and Sampler resources must use descriptor tables.");
                default:
                    throw Illegal.Value<ResourceKind>();
            }
        }

        private static bool usesDescriptorTable(ResourceKind resourceKind)
            => resourceKind == ResourceKind.TextureReadOnly
               || resourceKind == ResourceKind.TextureReadWrite
               || resourceKind == ResourceKind.Sampler;

        private static DescriptorRangeType getDescriptorRangeType(ResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceKind.TextureReadOnly:
                    return DescriptorRangeType.ShaderResourceView;
                case ResourceKind.TextureReadWrite:
                    return DescriptorRangeType.UnorderedAccessView;
                case ResourceKind.Sampler:
                    return DescriptorRangeType.Sampler;
                default:
                    throw new VeldridException("Only texture and sampler resources use descriptor ranges.");
            }
        }

        private static ShaderVisibility toShaderVisibility(ShaderStages stages)
        {
            if (stages == ShaderStages.Vertex)
            {
                return ShaderVisibility.Vertex;
            }

            if (stages == ShaderStages.Fragment)
            {
                return ShaderVisibility.Pixel;
            }

            if (stages == ShaderStages.Geometry)
            {
                return ShaderVisibility.Geometry;
            }

            if (stages == ShaderStages.TessellationControl)
            {
                return ShaderVisibility.Hull;
            }

            if (stages == ShaderStages.TessellationEvaluation)
            {
                return ShaderVisibility.Domain;
            }

            return ShaderVisibility.All;
        }

        private void createGraphicsPipelineState(ref GraphicsPipelineDescription description)
        {
            ReadOnlyMemory<byte> vertexShader = default;
            ReadOnlyMemory<byte> pixelShader = default;
            ReadOnlyMemory<byte> geometryShader = default;
            ReadOnlyMemory<byte> hullShader = default;
            ReadOnlyMemory<byte> domainShader = default;

            foreach (Shader shader in description.ShaderSet.Shaders)
            {
                var d3d12Shader = Util.AssertSubtype<Shader, D3D12Shader>(shader);
                ReadOnlyMemory<byte> bytecode = d3d12Shader.ShaderBytes;
                switch (shader.Stage)
                {
                    case ShaderStages.Vertex:
                        vertexShader = bytecode;
                        break;
                    case ShaderStages.Fragment:
                        pixelShader = bytecode;
                        break;
                    case ShaderStages.Geometry:
                        geometryShader = bytecode;
                        break;
                    case ShaderStages.TessellationControl:
                        hullShader = bytecode;
                        break;
                    case ShaderStages.TessellationEvaluation:
                        domainShader = bytecode;
                        break;
                }
            }

            InputElementDescription[] inputElements = buildInputElements(description.ShaderSet.VertexLayouts);

            var psoDescription = new GraphicsPipelineStateDescription
            {
                RootSignature = RootSignature,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                GeometryShader = geometryShader,
                HullShader = hullShader,
                DomainShader = domainShader,
                BlendState = buildBlendState(ref description.BlendState),
                RasterizerState = buildRasterizerState(ref description.RasterizerState),
                DepthStencilState = buildDepthStencilState(ref description.DepthStencilState),
                SampleMask = uint.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType,
                InputLayout = new InputLayoutDescription(inputElements),
                SampleDescription = new SampleDescription((uint)description.Outputs.SampleCount, 0)
            };

            int colorCount = Math.Min(description.Outputs.ColorAttachments.Length, 8);
            Format[] renderTargetFormats = new Format[colorCount];
            for (int i = 0; i < colorCount; i++)
            {
                renderTargetFormats[i] = D3D12Formats.ToDxgiFormat(description.Outputs.ColorAttachments[i].Format);
            }

            psoDescription.RenderTargetFormats = renderTargetFormats;

            if (description.Outputs.DepthAttachment is OutputAttachmentDescription depthAttachment)
            {
                psoDescription.DepthStencilFormat = D3D12Formats.ToDepthFormat(depthAttachment.Format);
            }

            PipelineState = gd.Device.CreateGraphicsPipelineState(psoDescription);
        }

        private static BlendDescription buildBlendState(ref BlendStateDescription description)
        {
            var blendDescription = BlendDescription.Opaque;
            blendDescription.AlphaToCoverageEnable = description.AlphaToCoverageEnabled;
            blendDescription.IndependentBlendEnable = true;

            int count = Math.Min(description.AttachmentStates?.Length ?? 0, 8);
            for (int i = 0; i < count; i++)
            {
                var attachment = description.AttachmentStates[i];
                blendDescription.RenderTarget[i] = new RenderTargetBlendDescription
                {
                    BlendEnable = attachment.BlendEnabled,
                    LogicOpEnable = false,
                    SourceBlend = D3D12Formats.ToBlend(attachment.SourceColorFactor),
                    DestinationBlend = D3D12Formats.ToBlend(attachment.DestinationColorFactor),
                    BlendOperation = D3D12Formats.ToBlendOp(attachment.ColorFunction),
                    SourceBlendAlpha = D3D12Formats.ToBlend(attachment.SourceAlphaFactor),
                    DestinationBlendAlpha = D3D12Formats.ToBlend(attachment.DestinationAlphaFactor),
                    BlendOperationAlpha = D3D12Formats.ToBlendOp(attachment.AlphaFunction),
                    LogicOp = LogicOp.Noop,
                    RenderTargetWriteMask = D3D12Formats.ToColorWriteMask(attachment.ColorWriteMask.GetOrDefault())
                };
            }

            return blendDescription;
        }

        private static RasterizerDescription buildRasterizerState(ref RasterizerStateDescription description)
        {
            return new RasterizerDescription
            {
                FillMode = D3D12Formats.ToFillMode(description.FillMode),
                CullMode = D3D12Formats.ToCullMode(description.CullMode),
                FrontCounterClockwise = description.FrontFace == FrontFace.CounterClockwise,
                DepthClipEnable = description.DepthClipEnabled,
                MultisampleEnable = false,
                AntialiasedLineEnable = false,
                ForcedSampleCount = 0,
                ConservativeRaster = ConservativeRasterizationMode.Off
            };
        }

        private static DepthStencilDescription buildDepthStencilState(ref DepthStencilStateDescription description)
        {
            return new DepthStencilDescription
            {
                DepthEnable = description.DepthTestEnabled,
                DepthWriteMask = description.DepthWriteEnabled ? DepthWriteMask.All : DepthWriteMask.Zero,
                DepthFunc = D3D12Formats.ToComparison(description.DepthComparison),
                StencilEnable = description.StencilTestEnabled,
                StencilReadMask = description.StencilReadMask,
                StencilWriteMask = description.StencilWriteMask,
                FrontFace = new DepthStencilOperationDescription(
                    D3D12Formats.ToStencilOp(description.StencilFront.Fail),
                    D3D12Formats.ToStencilOp(description.StencilFront.DepthFail),
                    D3D12Formats.ToStencilOp(description.StencilFront.Pass),
                    D3D12Formats.ToComparison(description.StencilFront.Comparison)),
                BackFace = new DepthStencilOperationDescription(
                    D3D12Formats.ToStencilOp(description.StencilBack.Fail),
                    D3D12Formats.ToStencilOp(description.StencilBack.DepthFail),
                    D3D12Formats.ToStencilOp(description.StencilBack.Pass),
                    D3D12Formats.ToComparison(description.StencilBack.Comparison))
            };
        }

        private static InputElementDescription[] buildInputElements(VertexLayoutDescription[] vertexLayouts)
        {
            if (vertexLayouts == null || vertexLayouts.Length == 0)
            {
                return Array.Empty<InputElementDescription>();
            }

            var elements = new List<InputElementDescription>();
            for (uint slot = 0; slot < vertexLayouts.Length; slot++)
            {
                VertexLayoutDescription layout = vertexLayouts[slot];
                uint currentOffset = 0;
                for (int i = 0; i < layout.Elements.Length; i++)
                {
                    VertexElementDescription element = layout.Elements[i];
                    uint offset = element.Offset != 0 ? element.Offset : currentOffset;
                    string semantic = string.IsNullOrEmpty(element.Name) ? "TEXCOORD" : element.Name;
                    var slotClass = layout.InstanceStepRate == 0 ? InputClassification.PerVertexData : InputClassification.PerInstanceData;

                    elements.Add(new InputElementDescription(
                        semantic,
                        0,
                        D3D12Formats.ToDxgiFormat(element.Format),
                        offset,
                        slot,
                        slotClass,
                        layout.InstanceStepRate));

                    currentOffset += FormatSizeHelpers.GetSizeInBytes(element.Format);
                }
            }

            return elements.ToArray();
        }
    }
}
