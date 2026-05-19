using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Pipeline.
/// </summary>
internal sealed class D3D12Pipeline : Pipeline {

    /// <summary>
    /// Stores the size, in bytes, reserved for push constants in the root signature.
    /// </summary>
    private const uint PushConstantSizeInBytes = 64;

    /// <summary>
    /// Stores the number of 32-bit constants reserved for push constants.
    /// </summary>
    private const uint PushConstantDwordCount = PushConstantSizeInBytes / 4;

    /// <summary>
    /// Stores the pipeline resource layouts state used by this instance.
    /// </summary>
    private readonly ResourceLayout[] _pipelineResourceLayouts;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Executes the value logic for this backend.
    /// </summary>
    private RootBindingInfo[][] _rootBindings = Array.Empty<RootBindingInfo[]>();

    /// <summary>
    /// Executes the value logic for this backend.
    /// </summary>
    private bool[][] _rootBindingValid = Array.Empty<bool[]>();

    /// <summary>
    /// Stores the using set register spaces state used by this instance.
    /// </summary>
    private bool _usingSetRegisterSpaces;

    /// <summary>
    /// Stores the root-parameter index used for push constants.
    /// </summary>
    private uint _pushConstantRootParameterIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Pipeline" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Pipeline(D3D12GraphicsDevice gd, ref GraphicsPipelineDescription description) : base(ref description) {
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

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Pipeline" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Pipeline(D3D12GraphicsDevice gd, ref ComputePipelineDescription description) : base(ref description) {
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

    /// <summary>
    /// Gets or sets IsComputePipeline.
    /// </summary>
    public override bool IsComputePipeline { get; }

    /// <summary>
    /// Gets or sets PipelineState.
    /// </summary>
    public ID3D12PipelineState PipelineState { get; private set; }

    /// <summary>
    /// Gets or sets RootSignature.
    /// </summary>
    public ID3D12RootSignature RootSignature { get; private set; }

    /// <summary>
    /// Gets or sets PrimitiveTopology.
    /// </summary>
    public Vortice.Direct3D.PrimitiveTopology PrimitiveTopology { get; }

    /// <summary>
    /// Gets or sets PrimitiveTopologyType.
    /// </summary>
    public PrimitiveTopologyType PrimitiveTopologyType { get; }

    /// <summary>
    /// Gets or sets VertexStrides.
    /// </summary>
    public uint[] VertexStrides { get; } = Array.Empty<uint>();

    /// <summary>
    /// Gets the root-parameter index that receives push constants.
    /// </summary>
    public uint PushConstantRootParameterIndex => this._pushConstantRootParameterIndex;

    /// <summary>
    /// Gets the maximum push-constant payload size supported by this pipeline.
    /// </summary>
    public uint MaxPushConstantSizeInBytes => PushConstantSizeInBytes;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this.PipelineState?.Dispose();
        this._disposed = true;
    }

    /// <summary>
    /// Attempts to get graphics root binding and reports whether it succeeded.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="element">The element value used by this operation.</param>
    /// <param name="bindingInfo">The binding info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool TryGetGraphicsRootBinding(uint set, uint element, out RootBindingInfo bindingInfo) {
        return this.TryGetRootBinding(set, element, out bindingInfo);
    }

    /// <summary>
    /// Attempts to get compute root binding and reports whether it succeeded.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="element">The element value used by this operation.</param>
    /// <param name="bindingInfo">The binding info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool TryGetComputeRootBinding(uint set, uint element, out RootBindingInfo bindingInfo) {
        return this.TryGetRootBinding(set, element, out bindingInfo);
    }

    /// <summary>
    /// Creates the root signature instance used by this backend.
    /// </summary>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    /// <param name="useSetRegisterSpaces">The use set register spaces value used by this operation.</param>
    private void CreateRootSignature(ResourceLayout[] resourceLayouts, bool useSetRegisterSpaces) {
        this._usingSetRegisterSpaces = useSetRegisterSpaces;
        List<RootParameter> rootParameters = new();
        this.InitializeRootBindingTables(resourceLayouts);
        uint globalCbvRegister = 1;
        uint globalSrvRegister = 0;
        uint globalUavRegister = 0;
        uint globalSamplerRegister = 0;

        if (resourceLayouts != null) {
            for (uint setIndex = 0; setIndex < resourceLayouts.Length; setIndex++) {
                D3D12ResourceLayout resourceLayout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
                ResourceLayoutElementDescription[] elements = resourceLayout.Elements;
                for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
                    ResourceLayoutElementDescription element = elements[elementIndex];
                    uint shaderRegister;
                    // SPIR-V -> HLSL remapping in Veldrith.SPIRV assigns binding indices
                    // globally per resource-kind (CBV/SRV/UAV/Sampler), not per set.
                    // Keep register numbering global here for both space modes.
                    shaderRegister = AllocateShaderRegister(element.Kind, ref globalCbvRegister, ref globalSrvRegister, ref globalUavRegister, ref globalSamplerRegister);

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
                        rootParameter = new RootParameter(parameterType, rootDescriptor, ToShaderVisibility(element.Stages));
                    }

                    uint rootParameterIndex = (uint)rootParameters.Count;
                    rootParameters.Add(rootParameter);
                    this._rootBindings[setIndex][elementIndex] = new RootBindingInfo(rootParameterIndex, element.Kind, descriptorTable);
                    this._rootBindingValid[setIndex][elementIndex] = true;
                }
            }
        }

        RootConstants rootConstants = new(0, 0, PushConstantDwordCount);
        this._pushConstantRootParameterIndex = (uint)rootParameters.Count;
        rootParameters.Add(new RootParameter(rootConstants, ShaderVisibility.All));

        RootSignatureFlags rootSignatureFlags = this.IsComputePipeline
            ? RootSignatureFlags.None
            : RootSignatureFlags.AllowInputAssemblerInputLayout;

        RootSignatureDescription rootSignatureDescription = new(rootSignatureFlags, rootParameters.ToArray(), Array.Empty<StaticSamplerDescription>());
        string cacheKey = BuildRootSignatureCacheKey(resourceLayouts, useSetRegisterSpaces, this.IsComputePipeline);
        this.RootSignature = this.gd.GetOrCreateRootSignature(cacheKey, in rootSignatureDescription);
    }

    /// <summary>
    /// Executes the recreate root signature without set spaces logic for this backend.
    /// </summary>
    private void RecreateRootSignatureWithoutSetSpaces() {
        if (!this._usingSetRegisterSpaces) {
            return;
        }

        this.CreateRootSignature(this._pipelineResourceLayouts, false);
    }

    /// <summary>
    /// Executes the allocate shader register logic for this backend.
    /// </summary>
    /// <param name="resourceKind">The resource kind value used by this operation.</param>
    /// <param name="nextCbvRegister">The next cbv register value used by this operation.</param>
    /// <param name="nextSrvRegister">The next srv register value used by this operation.</param>
    /// <param name="nextUavRegister">The next uav register value used by this operation.</param>
    /// <param name="nextSamplerRegister">The next sampler register value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static uint AllocateShaderRegister(ResourceKind resourceKind, ref uint nextCbvRegister, ref uint nextSrvRegister, ref uint nextUavRegister, ref uint nextSamplerRegister) {
        switch (resourceKind) {
            case ResourceKind.UniformBuffer: return nextCbvRegister++;
            case ResourceKind.StructuredBufferReadOnly: case ResourceKind.TextureReadOnly: return nextSrvRegister++;
            case ResourceKind.StructuredBufferReadWrite: case ResourceKind.TextureReadWrite: return nextUavRegister++;
            case ResourceKind.Sampler: return nextSamplerRegister++;
            default: throw Illegal.Value<ResourceKind>();
        }
    }

    /// <summary>
    /// Attempts to get root binding and reports whether it succeeded.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="element">The element value used by this operation.</param>
    /// <param name="bindingInfo">The binding info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Executes the initialize root binding tables logic for this backend.
    /// </summary>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    private void InitializeRootBindingTables(ResourceLayout[] resourceLayouts) {
        if (resourceLayouts == null || resourceLayouts.Length == 0) {
            this._rootBindings = Array.Empty<RootBindingInfo[]>();
            this._rootBindingValid = Array.Empty<bool[]>();
            return;
        }

        this._rootBindings = new RootBindingInfo[resourceLayouts.Length][];
        this._rootBindingValid = new bool[resourceLayouts.Length][];
        for (int setIndex = 0; setIndex < resourceLayouts.Length; setIndex++) {
            D3D12ResourceLayout resourceLayout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
            int elementCount = resourceLayout.Elements.Length;
            this._rootBindings[setIndex] = new RootBindingInfo[elementCount];
            this._rootBindingValid[setIndex] = new bool[elementCount];
        }
    }

    /// <summary>
    /// Executes the build root signature cache key logic for this backend.
    /// </summary>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    /// <param name="useSetRegisterSpaces">The use set register spaces value used by this operation.</param>
    /// <param name="isComputePipeline">The is compute pipeline value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string BuildRootSignatureCacheKey(ResourceLayout[] resourceLayouts, bool useSetRegisterSpaces, bool isComputePipeline) {
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
            D3D12ResourceLayout layout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[setIndex]);
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

    /// <summary>
    /// Creates the compute pipeline state instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    private void CreateComputePipelineState(ref ComputePipelineDescription description) {
        D3D12Shader d3d12Shader = Util.AssertSubtype<Shader, D3D12Shader>(description.ComputeShader);
        ComputePipelineStateDescription psoDescription = new() {
            RootSignature = this.RootSignature,
            ComputeShader = d3d12Shader.ShaderBytes
        };

        this.PipelineState = this.gd.Device.CreateComputePipelineState(psoDescription);
    }

    /// <summary>
    /// Gets the root parameter type value.
    /// </summary>
    /// <param name="resourceKind">The resource kind value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static RootParameterType GetRootParameterType(ResourceKind resourceKind) {
        switch (resourceKind) {
            case ResourceKind.UniformBuffer: return RootParameterType.ConstantBufferView;
            case ResourceKind.StructuredBufferReadOnly: return RootParameterType.ShaderResourceView;
            case ResourceKind.StructuredBufferReadWrite: return RootParameterType.UnorderedAccessView;
            case ResourceKind.TextureReadOnly: case ResourceKind.TextureReadWrite: case ResourceKind.Sampler: throw new VeldridException("Texture and Sampler resources must use descriptor tables.");
            default: throw Illegal.Value<ResourceKind>();
        }
    }

    /// <summary>
    /// Executes the uses descriptor table logic for this backend.
    /// </summary>
    /// <param name="resourceKind">The resource kind value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool UsesDescriptorTable(ResourceKind resourceKind) {
        return resourceKind == ResourceKind.TextureReadOnly
               || resourceKind == ResourceKind.TextureReadWrite
               || resourceKind == ResourceKind.Sampler;
    }

    /// <summary>
    /// Gets the descriptor range type value.
    /// </summary>
    /// <param name="resourceKind">The resource kind value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static DescriptorRangeType GetDescriptorRangeType(ResourceKind resourceKind) {
        switch (resourceKind) {
            case ResourceKind.TextureReadOnly: return DescriptorRangeType.ShaderResourceView;
            case ResourceKind.TextureReadWrite: return DescriptorRangeType.UnorderedAccessView;
            case ResourceKind.Sampler: return DescriptorRangeType.Sampler;
            default: throw new VeldridException("Only texture and sampler resources use descriptor ranges.");
        }
    }

    /// <summary>
    /// Executes the to shader visibility logic for this backend.
    /// </summary>
    /// <param name="stages">The stages value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Creates the graphics pipeline state instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
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
            SampleDescription = new SampleDescription(FormatHelpers.GetSampleCountUInt32(description.Outputs.SampleCount), 0)
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
            string removedReason = this.gd.GetDeviceRemovedReasonDescription();
            throw new VeldridException($"D3D12 graphics PSO creation failed. " + $"VS={(vertexShader.IsEmpty ? "missing" : vertexShader.Length.ToString())}, " + $"PS={(pixelShader.IsEmpty ? "missing" : pixelShader.Length.ToString())}, " + $"GS={(geometryShader.IsEmpty ? "none" : geometryShader.Length.ToString())}, " + $"HS={(hullShader.IsEmpty ? "none" : hullShader.Length.ToString())}, " + $"DS={(domainShader.IsEmpty ? "none" : domainShader.Length.ToString())}, " + $"InputElements={inputElements.Length}, " + $"ColorTargets={colorCount}, " + $"DepthFormat={psoDescription.DepthStencilFormat}, " + $"SampleCount={FormatHelpers.GetSampleCountUInt32(description.Outputs.SampleCount)}, " + $"PrimitiveTopology={description.PrimitiveTopology}, " + $"UseSetRegisterSpaces={this._usingSetRegisterSpaces}, " + $"DeviceRemovedReason={removedReason}.", ex);
        }
    }

    /// <summary>
    /// Executes the build blend state logic for this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Executes the build rasterizer state logic for this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Executes the build depth stencil state logic for this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static DepthStencilDescription BuildDepthStencilState(ref DepthStencilStateDescription description) {
        return new DepthStencilDescription {
            DepthEnable = description.DepthTestEnabled,
            DepthWriteMask = description.DepthWriteEnabled ? DepthWriteMask.All : DepthWriteMask.Zero,
            DepthFunc = D3D12Formats.ToComparison(description.DepthComparison),
            StencilEnable = description.StencilTestEnabled,
            StencilReadMask = description.StencilReadMask,
            StencilWriteMask = description.StencilWriteMask,
            FrontFace = new DepthStencilOperationDescription(D3D12Formats.ToStencilOp(description.StencilFront.Fail), D3D12Formats.ToStencilOp(description.StencilFront.DepthFail), D3D12Formats.ToStencilOp(description.StencilFront.Pass), D3D12Formats.ToComparison(description.StencilFront.Comparison)),
            BackFace = new DepthStencilOperationDescription(D3D12Formats.ToStencilOp(description.StencilBack.Fail), D3D12Formats.ToStencilOp(description.StencilBack.DepthFail), D3D12Formats.ToStencilOp(description.StencilBack.Pass), D3D12Formats.ToComparison(description.StencilBack.Comparison))
        };
    }

    /// <summary>
    /// Executes the build input elements logic for this backend.
    /// </summary>
    /// <param name="vertexLayouts">The resource layout used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

                elements.Add(new InputElementDescription("TEXCOORD", semanticIndex, D3D12Formats.ToDxgiFormat(element.Format), offset, slot, slotClass, layout.InstanceStepRate));

                semanticIndex++;
                currentOffset += FormatSizeHelpers.GetSizeInBytes(element.Format);
            }
        }

        return elements.ToArray();
    }

    /// <summary>
    /// Represents the RootBindingInfo data structure used by the graphics runtime.
    /// </summary>
    internal readonly struct RootBindingInfo {

        /// <summary>
        /// Initializes a new instance of the <see cref="RootBindingInfo" /> type.
        /// </summary>
        /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
        /// <param name="kind">The kind value used by this operation.</param>
        /// <param name="descriptorTable">The descriptor table value used by this operation.</param>
        public RootBindingInfo(uint rootParameterIndex, ResourceKind kind, bool descriptorTable) {
            this.RootParameterIndex = rootParameterIndex;
            this.Kind = kind;
            this.DescriptorTable = descriptorTable;
        }

        /// <summary>
        /// Gets or sets RootParameterIndex.
        /// </summary>
        public uint RootParameterIndex { get; }

        /// <summary>
        /// Gets or sets Kind.
        /// </summary>
        public ResourceKind Kind { get; }

        /// <summary>
        /// Gets or sets DescriptorTable.
        /// </summary>
        public bool DescriptorTable { get; }
    }
}
