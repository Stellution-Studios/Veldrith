using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrith.D3D12
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
        private RootBindingInfo[][] _rootBindings = Array.Empty<RootBindingInfo[]>();
        private bool[][] _rootBindingValid = Array.Empty<bool[]>();
        private readonly ResourceLayout[] _pipelineResourceLayouts;
        private bool _disposed;
        private string _name;
        private bool _usingSetRegisterSpaces;

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
            this._pipelineResourceLayouts = description.ResourceLayouts;

            CreateRootSignature(description.ResourceLayouts, useSetRegisterSpaces: true);
            try
            {
                CreateGraphicsPipelineState(ref description);
            }
            catch (VeldridException)
            {
                RecreateRootSignatureWithoutSetSpaces();
                CreateGraphicsPipelineState(ref description);
            }
        }

        public D3D12Pipeline(D3D12GraphicsDevice gd, ref ComputePipelineDescription description)
            : base(ref description)
        {
            this.gd = gd;
            IsComputePipeline = true;
            this._pipelineResourceLayouts = description.ResourceLayouts;
            CreateRootSignature(description.ResourceLayouts, useSetRegisterSpaces: true);
            try
            {
                CreateComputePipelineState(ref description);
            }
            catch (Exception)
            {
                RecreateRootSignatureWithoutSetSpaces();
                CreateComputePipelineState(ref description);
            }
        }

        public override bool IsComputePipeline { get; }
        public ID3D12PipelineState PipelineState { get; private set; }
        public ID3D12RootSignature RootSignature { get; private set; }
        public Vortice.Direct3D.PrimitiveTopology PrimitiveTopology { get; }
        public PrimitiveTopologyType PrimitiveTopologyType { get; }
        public uint[] VertexStrides { get; } = Array.Empty<uint>();
        public override bool IsDisposed => this._disposed;

        public override string Name
        {
            get => this._name;
            set
            {
                this._name = value;
            }
        }

        public override void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            PipelineState?.Dispose();
            this._disposed = true;
        }

        internal bool TryGetGraphicsRootBinding(uint set, uint element, out RootBindingInfo bindingInfo)
            => TryGetRootBinding(set, element, out bindingInfo);

        internal bool TryGetComputeRootBinding(uint set, uint element, out RootBindingInfo bindingInfo)
            => TryGetRootBinding(set, element, out bindingInfo);

        private void CreateRootSignature(ResourceLayout[] resourceLayouts, bool useSetRegisterSpaces)
        {
            this._usingSetRegisterSpaces = useSetRegisterSpaces;
            var rootParameters = new List<RootParameter>();
            InitializeRootBindingTables(resourceLayouts);
            uint globalCbvRegister = 0;
            uint globalSrvRegister = 0;
            uint globalUavRegister = 0;
            uint globalSamplerRegister = 0;

            if (resourceLayouts != null)
            {
                for (uint setIndex = 0; setIndex < resourceLayouts.Length; setIndex++)
                {
                    var resourceLayout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
                    ResourceLayoutElementDescription[] elements = resourceLayout.Elements;
                    for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++)
                    {
                        ResourceLayoutElementDescription element = elements[elementIndex];
                        uint shaderRegister;
                        // SPIR-V -> HLSL remapping in Veldrith.SPIRV assigns binding indices
                        // globally per resource-kind (CBV/SRV/UAV/Sampler), not per set.
                        // Keep register numbering global here for both space modes.
                        shaderRegister = AllocateShaderRegister(
                            element.Kind,
                            ref globalCbvRegister,
                            ref globalSrvRegister,
                            ref globalUavRegister,
                            ref globalSamplerRegister);

                        uint registerSpace = useSetRegisterSpaces ? setIndex : 0u;
                        bool descriptorTable = UsesDescriptorTable(element.Kind);
                        RootParameter rootParameter;
                        if (descriptorTable)
                        {
                            DescriptorRangeType rangeType = GetDescriptorRangeType(element.Kind);
                            var descriptorRange = new DescriptorRange(rangeType, 1, shaderRegister, registerSpace, 0);
                            var descriptorTableInfo = new RootDescriptorTable(new[] { descriptorRange });
                            rootParameter = new RootParameter(descriptorTableInfo, ToShaderVisibility(element.Stages));
                        }
                        else
                        {
                            RootParameterType parameterType = GetRootParameterType(element.Kind);
                            var rootDescriptor = new RootDescriptor(shaderRegister, registerSpace);
                            rootParameter = new RootParameter(parameterType, rootDescriptor, ToShaderVisibility(element.Stages));
                        }

                        uint rootParameterIndex = (uint)rootParameters.Count;
                        rootParameters.Add(rootParameter);
                        this._rootBindings[setIndex][elementIndex] = new RootBindingInfo(rootParameterIndex, element.Kind, descriptorTable);
                        this._rootBindingValid[setIndex][elementIndex] = true;
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
            string cacheKey = BuildRootSignatureCacheKey(resourceLayouts, useSetRegisterSpaces, IsComputePipeline);
            RootSignature = gd.GetOrCreateRootSignature(cacheKey, in rootSignatureDescription);
        }

        private void RecreateRootSignatureWithoutSetSpaces()
        {
            if (!this._usingSetRegisterSpaces)
            {
                return;
            }

            CreateRootSignature(this._pipelineResourceLayouts, useSetRegisterSpaces: false);
        }

        private static uint AllocateShaderRegister(
            ResourceKind resourceKind,
            ref uint nextCbvRegister,
            ref uint nextSrvRegister,
            ref uint nextUavRegister,
            ref uint nextSamplerRegister)
        {
            switch (resourceKind)
            {
                case ResourceKind.UniformBuffer:
                    return nextCbvRegister++;
                case ResourceKind.StructuredBufferReadOnly:
                case ResourceKind.TextureReadOnly:
                    return nextSrvRegister++;
                case ResourceKind.StructuredBufferReadWrite:
                case ResourceKind.TextureReadWrite:
                    return nextUavRegister++;
                case ResourceKind.Sampler:
                    return nextSamplerRegister++;
                default:
                    throw Illegal.Value<ResourceKind>();
            }
        }

        private bool TryGetRootBinding(uint set, uint element, out RootBindingInfo bindingInfo)
        {
            if (set < (uint)this._rootBindings.Length
                && element < (uint)this._rootBindings[set].Length
                && this._rootBindingValid[set][element])
            {
                bindingInfo = this._rootBindings[set][element];
                return true;
            }

            bindingInfo = default;
            return false;
        }

        private void InitializeRootBindingTables(ResourceLayout[] resourceLayouts)
        {
            if (resourceLayouts == null || resourceLayouts.Length == 0)
            {
                this._rootBindings = Array.Empty<RootBindingInfo[]>();
                this._rootBindingValid = Array.Empty<bool[]>();
                return;
            }

            this._rootBindings = new RootBindingInfo[resourceLayouts.Length][];
            this._rootBindingValid = new bool[resourceLayouts.Length][];
            for (int setIndex = 0; setIndex < resourceLayouts.Length; setIndex++)
            {
                var resourceLayout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
                int elementCount = resourceLayout.Elements.Length;
                this._rootBindings[setIndex] = new RootBindingInfo[elementCount];
                this._rootBindingValid[setIndex] = new bool[elementCount];
            }
        }

        private static string BuildRootSignatureCacheKey(ResourceLayout[] resourceLayouts, bool useSetRegisterSpaces, bool isComputePipeline)
        {
            var sb = new StringBuilder(256);
            sb.Append(isComputePipeline ? 'C' : 'G');
            sb.Append(useSetRegisterSpaces ? 'S' : 'N');
            sb.Append('|');
            if (resourceLayouts == null || resourceLayouts.Length == 0)
            {
                sb.Append("0");
                return sb.ToString();
            }

            sb.Append(resourceLayouts.Length);
            for (int setIndex = 0; setIndex < resourceLayouts.Length; setIndex++)
            {
                sb.Append(';');
                var layout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
                ResourceLayoutElementDescription[] elements = layout.Elements;
                sb.Append(elements.Length);
                for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++)
                {
                    ResourceLayoutElementDescription element = elements[elementIndex];
                    sb.Append(',');
                    sb.Append((int)element.Kind);
                    sb.Append(':');
                    sb.Append((int)element.Stages);
                    sb.Append(':');
                    sb.Append((int)element.Options);
                }
            }

            return sb.ToString();
        }

        private void CreateComputePipelineState(ref ComputePipelineDescription description)
        {
            var d3d12Shader = Util.AssertSubtype<Shader, D3D12Shader>(description.ComputeShader);
            var psoDescription = new ComputePipelineStateDescription
            {
                RootSignature = RootSignature,
                ComputeShader = d3d12Shader.ShaderBytes
            };

            PipelineState = gd.Device.CreateComputePipelineState(psoDescription);
        }

        private static RootParameterType GetRootParameterType(ResourceKind resourceKind)
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

        private static bool UsesDescriptorTable(ResourceKind resourceKind)
            => resourceKind == ResourceKind.TextureReadOnly
               || resourceKind == ResourceKind.TextureReadWrite
               || resourceKind == ResourceKind.Sampler;

        private static DescriptorRangeType GetDescriptorRangeType(ResourceKind resourceKind)
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

        private static ShaderVisibility ToShaderVisibility(ShaderStages stages)
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

        private void CreateGraphicsPipelineState(ref GraphicsPipelineDescription description)
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

            InputElementDescription[] inputElements = BuildInputElements(description.ShaderSet.VertexLayouts);

            var psoDescription = new GraphicsPipelineStateDescription
            {
                RootSignature = RootSignature,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                GeometryShader = geometryShader,
                HullShader = hullShader,
                DomainShader = domainShader,
                BlendState = BuildBlendState(ref description.BlendState),
                RasterizerState = BuildRasterizerState(ref description.RasterizerState),
                DepthStencilState = BuildDepthStencilState(ref description.DepthStencilState),
                SampleMask = uint.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType,
                InputLayout = new InputLayoutDescription(inputElements),
                SampleDescription = new SampleDescription(FormatHelpers.GetSampleCountUInt32(description.Outputs.SampleCount), 0)
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

            try
            {
                PipelineState = gd.Device.CreateGraphicsPipelineState(psoDescription);
            }
            catch (Exception ex)
            {
                throw new VeldridException(
                    $"D3D12 graphics PSO creation failed. " +
                    $"VS={(vertexShader.IsEmpty ? "missing" : vertexShader.Length.ToString())}, " +
                    $"PS={(pixelShader.IsEmpty ? "missing" : pixelShader.Length.ToString())}, " +
                    $"GS={(geometryShader.IsEmpty ? "none" : geometryShader.Length.ToString())}, " +
                    $"HS={(hullShader.IsEmpty ? "none" : hullShader.Length.ToString())}, " +
                    $"DS={(domainShader.IsEmpty ? "none" : domainShader.Length.ToString())}, " +
                    $"InputElements={inputElements.Length}, " +
                    $"ColorTargets={colorCount}, " +
                    $"DepthFormat={psoDescription.DepthStencilFormat}, " +
                    $"SampleCount={FormatHelpers.GetSampleCountUInt32(description.Outputs.SampleCount)}, " +
                    $"PrimitiveTopology={description.PrimitiveTopology}, " +
                    $"UseSetRegisterSpaces={this._usingSetRegisterSpaces}.",
                    ex);
            }
        }

        private static BlendDescription BuildBlendState(ref BlendStateDescription description)
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

        private static RasterizerDescription BuildRasterizerState(ref RasterizerStateDescription description)
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

        private static DepthStencilDescription BuildDepthStencilState(ref DepthStencilStateDescription description)
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

        private static InputElementDescription[] BuildInputElements(VertexLayoutDescription[] vertexLayouts)
        {
            if (vertexLayouts == null || vertexLayouts.Length == 0)
            {
                return Array.Empty<InputElementDescription>();
            }

            var elements = new List<InputElementDescription>();
            uint semanticIndex = 0;
            for (uint slot = 0; slot < vertexLayouts.Length; slot++)
            {
                VertexLayoutDescription layout = vertexLayouts[slot];
                uint currentOffset = 0;
                for (int i = 0; i < layout.Elements.Length; i++)
                {
                    VertexElementDescription element = layout.Elements[i];
                    uint offset = element.Offset != 0 ? element.Offset : currentOffset;
                    var slotClass = layout.InstanceStepRate == 0 ? InputClassification.PerVertexData : InputClassification.PerInstanceData;

                    elements.Add(new InputElementDescription(
                        "TEXCOORD",
                        semanticIndex,
                        D3D12Formats.ToDxgiFormat(element.Format),
                        offset,
                        slot,
                        slotClass,
                        layout.InstanceStepRate));

                    semanticIndex++;
                    currentOffset += FormatSizeHelpers.GetSizeInBytes(element.Format);
                }
            }

            return elements.ToArray();
        }
    }
}
