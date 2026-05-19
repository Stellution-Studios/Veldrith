using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrith.D3D12;

internal sealed class D3D12Pipeline : Pipeline {
    private readonly ResourceLayout[] _pipelineResourceLayouts;

    private readonly D3D12GraphicsDevice gd;
    private bool _disposed;
    private RootBindingInfo[][] _rootBindings = Array.Empty<RootBindingInfo[]>();
    private bool[][] _rootBindingValid = Array.Empty<bool[]>();
    private bool _usingSetRegisterSpaces;

    public D3D12Pipeline(D3D12GraphicsDevice gd, ref GraphicsPipelineDescription description)
        : base(ref description) {
        this.gd = gd;
        this.IsComputePipeline = false;
        this.PrimitiveTopology = D3D12Formats.ToD3DPrimitiveTopology(description.PrimitiveTopology);
        this.PrimitiveTopologyType = D3D12Formats.ToPrimitiveTopologyType(description.PrimitiveTopology);
        this.VertexStrides = new uint[description.ShaderSet.VertexLayouts.Length];
        for (uint i = 0; i < this.VertexStrides.Length; i++) {
            this.VertexStrides[i] = description.ShaderSet.VertexLayouts[i].Stride;
        }

        this._pipelineResourceLayouts = description.ResourceLayouts;

        this.CreateRootSignature(description.ResourceLayouts, true);
        try {
            this.CreateGraphicsPipelineState(ref description);
        }
        catch (VeldridException) {
            this.RecreateRootSignatureWithoutSetSpaces();
            this.CreateGraphicsPipelineState(ref description);
        }
    }

    public D3D12Pipeline(D3D12GraphicsDevice gd, ref ComputePipelineDescription description)
        : base(ref description) {
        this.gd = gd;
        this.IsComputePipeline = true;
        this._pipelineResourceLayouts = description.ResourceLayouts;
        this.CreateRootSignature(description.ResourceLayouts, true);
        try {
            this.CreateComputePipelineState(ref description);
        }
        catch (Exception) {
            this.RecreateRootSignatureWithoutSetSpaces();
            this.CreateComputePipelineState(ref description);
        }
    }

    public override bool IsComputePipeline { get; }
    public ID3D12PipelineState PipelineState { get; private set; }
    public ID3D12RootSignature RootSignature { get; private set; }
    public Vortice.Direct3D.PrimitiveTopology PrimitiveTopology { get; }
    public PrimitiveTopologyType PrimitiveTopologyType { get; }
    public uint[] VertexStrides { get; } = Array.Empty<uint>();
    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this.PipelineState?.Dispose();
        this._disposed = true;
    }

    internal bool TryGetGraphicsRootBinding(uint set, uint element, out RootBindingInfo bindingInfo) {
        return this.TryGetRootBinding(set, element, out bindingInfo);
    }

    internal bool TryGetComputeRootBinding(uint set, uint element, out RootBindingInfo bindingInfo) {
        return this.TryGetRootBinding(set, element, out bindingInfo);
    }

    private void CreateRootSignature(ResourceLayout[] resourceLayouts, bool useSetRegisterSpaces) {
        this._usingSetRegisterSpaces = useSetRegisterSpaces;
        List<RootParameter> rootParameters = new();
        this.InitializeRootBindingTables(resourceLayouts);
        uint globalCbvRegister = 0;
        uint globalSrvRegister = 0;
        uint globalUavRegister = 0;
        uint globalSamplerRegister = 0;

        if (resourceLayouts != null) {
            for (uint setIndex = 0; setIndex < resourceLayouts.Length; setIndex++) {
                D3D12ResourceLayout resourceLayout =
                    Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
                ResourceLayoutElementDescription[] elements = resourceLayout.Elements;
                for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
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
                    if (descriptorTable) {
                        DescriptorRangeType rangeType = GetDescriptorRangeType(element.Kind);
                        DescriptorRange descriptorRange = new(rangeType, 1, shaderRegister, registerSpace, 0);
                        RootDescriptorTable descriptorTableInfo = new(descriptorRange);
                        rootParameter = new RootParameter(descriptorTableInfo, ToShaderVisibility(element.Stages));
                    }
                    else {
                        RootParameterType parameterType = GetRootParameterType(element.Kind);
                        RootDescriptor rootDescriptor = new(shaderRegister, registerSpace);
                        rootParameter = new RootParameter(parameterType, rootDescriptor,
                            ToShaderVisibility(element.Stages));
                    }

                    uint rootParameterIndex = (uint)rootParameters.Count;
                    rootParameters.Add(rootParameter);
                    this._rootBindings[setIndex][elementIndex] =
                        new RootBindingInfo(rootParameterIndex, element.Kind, descriptorTable);
                    this._rootBindingValid[setIndex][elementIndex] = true;
                }
            }
        }

        RootSignatureFlags rootSignatureFlags = this.IsComputePipeline
            ? RootSignatureFlags.None
            : RootSignatureFlags.AllowInputAssemblerInputLayout;

        RootSignatureDescription rootSignatureDescription = new(
            rootSignatureFlags,
            rootParameters.ToArray(),
            Array.Empty<StaticSamplerDescription>());
        string cacheKey = BuildRootSignatureCacheKey(resourceLayouts, useSetRegisterSpaces, this.IsComputePipeline);
        this.RootSignature = this.gd.GetOrCreateRootSignature(cacheKey, in rootSignatureDescription);
    }

    private void RecreateRootSignatureWithoutSetSpaces() {
        if (!this._usingSetRegisterSpaces) {
            return;
        }

        this.CreateRootSignature(this._pipelineResourceLayouts, false);
    }

    private static uint AllocateShaderRegister(
        ResourceKind resourceKind,
        ref uint nextCbvRegister,
        ref uint nextSrvRegister,
        ref uint nextUavRegister,
        ref uint nextSamplerRegister) {
        switch (resourceKind) {
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

    private bool TryGetRootBinding(uint set, uint element, out RootBindingInfo bindingInfo) {
        if (set < (uint)this._rootBindings.Length
            && element < (uint)this._rootBindings[set].Length
            && this._rootBindingValid[set][element]) {
            bindingInfo = this._rootBindings[set][element];
            return true;
        }

        bindingInfo = default;
        return false;
    }

    private void InitializeRootBindingTables(ResourceLayout[] resourceLayouts) {
        if (resourceLayouts == null || resourceLayouts.Length == 0) {
            this._rootBindings = Array.Empty<RootBindingInfo[]>();
            this._rootBindingValid = Array.Empty<bool[]>();
            return;
        }

        this._rootBindings = new RootBindingInfo[resourceLayouts.Length][];
        this._rootBindingValid = new bool[resourceLayouts.Length][];
        for (int setIndex = 0; setIndex < resourceLayouts.Length; setIndex++) {
            D3D12ResourceLayout resourceLayout =
                Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
            int elementCount = resourceLayout.Elements.Length;
            this._rootBindings[setIndex] = new RootBindingInfo[elementCount];
            this._rootBindingValid[setIndex] = new bool[elementCount];
        }
    }

    private static string BuildRootSignatureCacheKey(ResourceLayout[] resourceLayouts, bool useSetRegisterSpaces,
        bool isComputePipeline) {
        StringBuilder sb = new(256);
        sb.Append(isComputePipeline ? 'C' : 'G');
        sb.Append(useSetRegisterSpaces ? 'S' : 'N');
        sb.Append('|');
        if (resourceLayouts == null || resourceLayouts.Length == 0) {
            sb.Append("0");
            return sb.ToString();
        }

        sb.Append(resourceLayouts.Length);
        for (int setIndex = 0; setIndex < resourceLayouts.Length; setIndex++) {
            sb.Append(';');
            D3D12ResourceLayout layout =
                Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
            ResourceLayoutElementDescription[] elements = layout.Elements;
            sb.Append(elements.Length);
            for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
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

    private void CreateComputePipelineState(ref ComputePipelineDescription description) {
        D3D12Shader d3d12Shader = Util.AssertSubtype<Shader, D3D12Shader>(description.ComputeShader);
        ComputePipelineStateDescription psoDescription = new() {
            RootSignature = this.RootSignature,
            ComputeShader = d3d12Shader.ShaderBytes
        };

        this.PipelineState = this.gd.Device.CreateComputePipelineState(psoDescription);
    }

    private static RootParameterType GetRootParameterType(ResourceKind resourceKind) {
        switch (resourceKind) {
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

    private static bool UsesDescriptorTable(ResourceKind resourceKind) {
        return resourceKind == ResourceKind.TextureReadOnly
               || resourceKind == ResourceKind.TextureReadWrite
               || resourceKind == ResourceKind.Sampler;
    }

    private static DescriptorRangeType GetDescriptorRangeType(ResourceKind resourceKind) {
        switch (resourceKind) {
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

    private static ShaderVisibility ToShaderVisibility(ShaderStages stages) {
        if (stages == ShaderStages.Vertex) {
            return ShaderVisibility.Vertex;
        }

        if (stages == ShaderStages.Fragment) {
            return ShaderVisibility.Pixel;
        }

        if (stages == ShaderStages.Geometry) {
            return ShaderVisibility.Geometry;
        }

        if (stages == ShaderStages.TessellationControl) {
            return ShaderVisibility.Hull;
        }

        if (stages == ShaderStages.TessellationEvaluation) {
            return ShaderVisibility.Domain;
        }

        return ShaderVisibility.All;
    }

    private void CreateGraphicsPipelineState(ref GraphicsPipelineDescription description) {
        ReadOnlyMemory<byte> vertexShader = default;
        ReadOnlyMemory<byte> pixelShader = default;
        ReadOnlyMemory<byte> geometryShader = default;
        ReadOnlyMemory<byte> hullShader = default;
        ReadOnlyMemory<byte> domainShader = default;

        foreach (Shader shader in description.ShaderSet.Shaders) {
            D3D12Shader d3d12Shader = Util.AssertSubtype<Shader, D3D12Shader>(shader);
            ReadOnlyMemory<byte> bytecode = d3d12Shader.ShaderBytes;
            switch (shader.Stage) {
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

        GraphicsPipelineStateDescription psoDescription = new() {
            RootSignature = this.RootSignature,
            VertexShader = vertexShader,
            PixelShader = pixelShader,
            GeometryShader = geometryShader,
            HullShader = hullShader,
            DomainShader = domainShader,
            BlendState = BuildBlendState(ref description.BlendState),
            RasterizerState = BuildRasterizerState(ref description.RasterizerState),
            DepthStencilState = BuildDepthStencilState(ref description.DepthStencilState),
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = this.PrimitiveTopologyType,
            InputLayout = new InputLayoutDescription(inputElements),
            SampleDescription =
                new SampleDescription(FormatHelpers.GetSampleCountUInt32(description.Outputs.SampleCount), 0)
        };

        int colorCount = Math.Min(description.Outputs.ColorAttachments.Length, 8);
        Format[] renderTargetFormats = new Format[colorCount];
        for (int i = 0; i < colorCount; i++) {
            renderTargetFormats[i] = D3D12Formats.ToDxgiFormat(description.Outputs.ColorAttachments[i].Format);
        }

        psoDescription.RenderTargetFormats = renderTargetFormats;

        if (description.Outputs.DepthAttachment is OutputAttachmentDescription depthAttachment) {
            psoDescription.DepthStencilFormat = D3D12Formats.ToDepthFormat(depthAttachment.Format);
        }

        try {
            this.PipelineState = this.gd.Device.CreateGraphicsPipelineState(psoDescription);
        }
        catch (Exception ex) {
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

    private static BlendDescription BuildBlendState(ref BlendStateDescription description) {
        BlendDescription blendDescription = BlendDescription.Opaque;
        blendDescription.AlphaToCoverageEnable = description.AlphaToCoverageEnabled;
        blendDescription.IndependentBlendEnable = true;

        int count = Math.Min(description.AttachmentStates?.Length ?? 0, 8);
        for (int i = 0; i < count; i++) {
            BlendAttachmentDescription attachment = description.AttachmentStates[i];
            blendDescription.RenderTarget[i] = new RenderTargetBlendDescription {
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

    private static RasterizerDescription BuildRasterizerState(ref RasterizerStateDescription description) {
        return new RasterizerDescription {
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

    private static DepthStencilDescription BuildDepthStencilState(ref DepthStencilStateDescription description) {
        return new DepthStencilDescription {
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

    private static InputElementDescription[] BuildInputElements(VertexLayoutDescription[] vertexLayouts) {
        if (vertexLayouts == null || vertexLayouts.Length == 0) {
            return Array.Empty<InputElementDescription>();
        }

        List<InputElementDescription> elements = new();
        uint semanticIndex = 0;
        for (uint slot = 0; slot < vertexLayouts.Length; slot++) {
            VertexLayoutDescription layout = vertexLayouts[slot];
            uint currentOffset = 0;
            for (int i = 0; i < layout.Elements.Length; i++) {
                VertexElementDescription element = layout.Elements[i];
                uint offset = element.Offset != 0 ? element.Offset : currentOffset;
                InputClassification slotClass = layout.InstanceStepRate == 0
                    ? InputClassification.PerVertexData
                    : InputClassification.PerInstanceData;

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

    internal readonly struct RootBindingInfo {
        public RootBindingInfo(uint rootParameterIndex, ResourceKind kind, bool descriptorTable) {
            this.RootParameterIndex = rootParameterIndex;
            this.Kind = kind;
            this.DescriptorTable = descriptorTable;
        }

        public uint RootParameterIndex { get; }
        public ResourceKind Kind { get; }
        public bool DescriptorTable { get; }
    }
}