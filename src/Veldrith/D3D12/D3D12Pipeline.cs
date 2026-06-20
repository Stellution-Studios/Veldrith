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
    private const uint _pushConstantSizeInBytes = 64;

    /// <summary>
    /// Stores the number of 32-bit constants reserved for push constants.
    /// </summary>
    private const uint _pushConstantDwordCount = _pushConstantSizeInBytes / 4;

    /// <summary>
    /// Stores the pipeline resource layouts state used by this instance.
    /// </summary>
    private readonly ResourceLayout[] _pipelineResourceLayouts;

    /// <summary>
    /// Stores the graphics shader stages used by this pipeline.
    /// </summary>
    private readonly ShaderStages _graphicsShaderStages;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

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
    /// Stores the cache key used for the current root signature.
    /// </summary>
    private string _rootSignatureCacheKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Pipeline" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Pipeline(D3D12GraphicsDevice gd, ref GraphicsPipelineDescription description) : base(ref description) {
        this._gd = gd;
        this.IsComputePipeline = false;
        this.PrimitiveTopology = D3D12Formats.ToD3DPrimitiveTopology(description.PrimitiveTopology);
        this.PrimitiveTopologyType = D3D12Formats.ToPrimitiveTopologyType(description.PrimitiveTopology);
        this.StencilReference = description.DepthStencilState.StencilReference;
        this.UsesStencilReference = description.DepthStencilState.StencilTestEnabled;
        this.VertexStrides = new uint[description.ShaderSet.VertexLayouts.Length];
        for (uint i = 0; i < this.VertexStrides.Length; i++) {
            this.VertexStrides[i] = description.ShaderSet.VertexLayouts[i].Stride;
        }

        this._pipelineResourceLayouts = description.ResourceLayouts;
        this._graphicsShaderStages = GetShaderStages(description.ShaderSet.Shaders);
        this.CreateRootSignature(description.ResourceLayouts, false);
        this.CreateGraphicsPipelineState(ref description);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Pipeline" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Pipeline(D3D12GraphicsDevice gd, ref ComputePipelineDescription description) : base(ref description) {
        this._gd = gd;
        this.IsComputePipeline = true;
        this._pipelineResourceLayouts = description.ResourceLayouts;
        this._graphicsShaderStages = ShaderStages.None;
        this.CreateRootSignature(description.ResourceLayouts, false);
        this.CreateComputePipelineState(ref description);
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
    /// Gets the stencil reference value used by this graphics pipeline.
    /// </summary>
    public uint StencilReference { get; }

    /// <summary>
    /// Gets whether the graphics pipeline consumes the dynamic stencil reference state.
    /// </summary>
    public bool UsesStencilReference { get; }

    /// <summary>
    /// Gets the root-parameter index that receives push constants.
    /// </summary>
    public uint PushConstantRootParameterIndex => this._pushConstantRootParameterIndex;

    /// <summary>
    /// Gets the maximum push-constant payload size supported by this pipeline.
    /// </summary>
    public uint MaxPushConstantSizeInBytes => _pushConstantSizeInBytes;

    /// <summary>
    /// Gets the number of resource set slots used by this pipeline.
    /// </summary>
    public uint ResourceSetCount => (uint)(this._pipelineResourceLayouts?.Length ?? 0);

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
                List<PendingDescriptorTableBinding> srvUavBindings = null;
                List<PendingDescriptorTableBinding> samplerBindings = null;
                for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
                    ResourceLayoutElementDescription element = elements[elementIndex];
                    uint shaderRegister;
                    // SPIR-V -> HLSL remapping in Veldrith.SPIRV assigns binding indices
                    // globally per resource-kind (CBV/SRV/UAV/Sampler), not per set.
                    // Keep register numbering global here for both space modes.
                    shaderRegister = AllocateShaderRegister(element.Kind, ref globalCbvRegister, ref globalSrvRegister, ref globalUavRegister, ref globalSamplerRegister);

                    uint registerSpace = useSetRegisterSpaces ? setIndex : 0u;
                    bool descriptorTable = UsesDescriptorTable(element.Kind);
                    if (descriptorTable) {
                        DescriptorRangeType rangeType = GetDescriptorRangeType(element.Kind);
                        if (element.Kind == ResourceKind.Sampler) {
                            samplerBindings ??= new List<PendingDescriptorTableBinding>();
                            samplerBindings.Add(new PendingDescriptorTableBinding(elementIndex, element.Kind, rangeType, shaderRegister, registerSpace, (uint)samplerBindings.Count));
                        }
                        else {
                            srvUavBindings ??= new List<PendingDescriptorTableBinding>();
                            srvUavBindings.Add(new PendingDescriptorTableBinding(elementIndex, element.Kind, rangeType, shaderRegister, registerSpace, (uint)srvUavBindings.Count));
                        }
                    }
                    else {
                        RootParameterType parameterType = GetRootParameterType(element.Kind);
                        RootDescriptor rootDescriptor = new(shaderRegister, registerSpace);
                        RootParameter rootParameter = new(parameterType, rootDescriptor, ToShaderVisibility(element.Stages));
                        uint rootParameterIndex = (uint)rootParameters.Count;
                        rootParameters.Add(rootParameter);
                        this._rootBindings[setIndex][elementIndex] = new RootBindingInfo(rootParameterIndex, element.Kind, false, DescriptorTableKind.None, 0);
                        this._rootBindingValid[setIndex][elementIndex] = true;
                    }
                }

                this.AddDescriptorTableRootParameter(rootParameters, setIndex, srvUavBindings, DescriptorTableKind.SrvUav);
                this.AddDescriptorTableRootParameter(rootParameters, setIndex, samplerBindings, DescriptorTableKind.Sampler);
            }
        }

        RootConstants rootConstants = new(0, 0, _pushConstantDwordCount);
        this._pushConstantRootParameterIndex = (uint)rootParameters.Count;
        rootParameters.Add(new RootParameter(rootConstants, ShaderVisibility.All));

        RootSignatureFlags rootSignatureFlags = this.BuildRootSignatureFlags(resourceLayouts);

        RootSignatureDescription rootSignatureDescription = new(rootSignatureFlags, rootParameters.ToArray(), Array.Empty<StaticSamplerDescription>());
        string cacheKey = BuildRootSignatureCacheKey(resourceLayouts, useSetRegisterSpaces, this.IsComputePipeline, rootSignatureFlags);
        this._rootSignatureCacheKey = cacheKey;
        this.RootSignature = this._gd.GetOrCreateRootSignature(cacheKey, in rootSignatureDescription);
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
    /// Builds D3D12 root-signature flags for this pipeline.
    /// </summary>
    /// <param name="resourceLayouts">The resource layouts used by the root signature.</param>
    /// <returns>The root-signature flags.</returns>
    private RootSignatureFlags BuildRootSignatureFlags(ResourceLayout[] resourceLayouts) {
        if (this.IsComputePipeline) {
            return RootSignatureFlags.None;
        }

        RootSignatureFlags flags = RootSignatureFlags.AllowInputAssemblerInputLayout;
        ShaderStages visibleStages = this._graphicsShaderStages | GetResourceLayoutStages(resourceLayouts);
        if ((visibleStages & ShaderStages.Vertex) == 0) {
            flags |= RootSignatureFlags.DenyVertexShaderRootAccess;
        }

        if ((visibleStages & ShaderStages.Fragment) == 0) {
            flags |= RootSignatureFlags.DenyPixelShaderRootAccess;
        }

        if ((visibleStages & ShaderStages.Geometry) == 0) {
            flags |= RootSignatureFlags.DenyGeometryShaderRootAccess;
        }

        if ((visibleStages & ShaderStages.TessellationControl) == 0) {
            flags |= RootSignatureFlags.DenyHullShaderRootAccess;
        }

        if ((visibleStages & ShaderStages.TessellationEvaluation) == 0) {
            flags |= RootSignatureFlags.DenyDomainShaderRootAccess;
        }

        return flags;
    }

    /// <summary>
    /// Collects shader stages from a shader array.
    /// </summary>
    /// <param name="shaders">The shaders to inspect.</param>
    /// <returns>The combined shader stage mask.</returns>
    private static ShaderStages GetShaderStages(Shader[] shaders) {
        ShaderStages stages = ShaderStages.None;
        if (shaders == null) {
            return stages;
        }

        for (int i = 0; i < shaders.Length; i++) {
            stages |= shaders[i].Stage;
        }

        return stages;
    }

    /// <summary>
    /// Collects shader stages declared by resource layouts.
    /// </summary>
    /// <param name="resourceLayouts">The resource layouts to inspect.</param>
    /// <returns>The combined resource-layout stage mask.</returns>
    private static ShaderStages GetResourceLayoutStages(ResourceLayout[] resourceLayouts) {
        ShaderStages stages = ShaderStages.None;
        if (resourceLayouts == null) {
            return stages;
        }

        for (int layoutIndex = 0; layoutIndex < resourceLayouts.Length; layoutIndex++) {
            D3D12ResourceLayout layout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(resourceLayouts[layoutIndex]);
            ResourceLayoutElementDescription[] elements = layout.Elements;
            for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
                stages |= elements[elementIndex].Stages;
            }
        }

        return stages;
    }

    /// <summary>
    /// Adds a grouped descriptor table root parameter for a resource set.
    /// </summary>
    /// <param name="rootParameters">The root parameter list being built.</param>
    /// <param name="setIndex">The resource set index.</param>
    /// <param name="bindings">The descriptor bindings assigned to the table.</param>
    /// <param name="tableKind">The grouped table kind.</param>
    private void AddDescriptorTableRootParameter(List<RootParameter> rootParameters, uint setIndex, List<PendingDescriptorTableBinding> bindings, DescriptorTableKind tableKind) {
        if (bindings == null || bindings.Count == 0) {
            return;
        }

        DescriptorRange[] ranges = new DescriptorRange[bindings.Count];
        for (int i = 0; i < bindings.Count; i++) {
            PendingDescriptorTableBinding binding = bindings[i];
            ranges[i] = new DescriptorRange(binding.RangeType, 1, binding.ShaderRegister, binding.RegisterSpace, binding.TableOffset);
        }

        uint rootParameterIndex = (uint)rootParameters.Count;
        rootParameters.Add(new RootParameter(new RootDescriptorTable(ranges), ShaderVisibility.All));
        for (int i = 0; i < bindings.Count; i++) {
            PendingDescriptorTableBinding binding = bindings[i];
            this._rootBindings[setIndex][binding.ElementIndex] = new RootBindingInfo(rootParameterIndex, binding.Kind, true, tableKind, binding.TableOffset);
            this._rootBindingValid[setIndex][binding.ElementIndex] = true;
        }
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
    /// <param name="rootSignatureFlags">The D3D12 root-signature flags.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string BuildRootSignatureCacheKey(ResourceLayout[] resourceLayouts, bool useSetRegisterSpaces, bool isComputePipeline, RootSignatureFlags rootSignatureFlags) {
        StringBuilder sb = new(256);
        sb.Append(isComputePipeline ? 'C' : 'G');
        sb.Append(useSetRegisterSpaces ? 'S' : 'N');
        sb.Append('F');
        sb.Append((int)rootSignatureFlags);
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
    /// Builds a stable cache key for a compute pipeline state.
    /// </summary>
    /// <param name="rootSignatureKey">The root signature cache key.</param>
    /// <param name="shaderBytes">The compute shader bytecode.</param>
    /// <returns>The cache key used by the D3D12 pipeline-state cache.</returns>
    private static string BuildComputePipelineStateCacheKey(string rootSignatureKey, byte[] shaderBytes) {
        StringBuilder sb = new(128);
        sb.Append("cpso|");
        sb.Append(rootSignatureKey);
        AppendShaderHash(sb, shaderBytes);
        return sb.ToString();
    }

    /// <summary>
    /// Builds a stable cache key for a graphics pipeline state.
    /// </summary>
    /// <param name="description">The graphics pipeline description.</param>
    /// <param name="rootSignatureKey">The root signature cache key.</param>
    /// <param name="vertexShader">The vertex shader bytecode.</param>
    /// <param name="pixelShader">The pixel shader bytecode.</param>
    /// <param name="geometryShader">The geometry shader bytecode.</param>
    /// <param name="hullShader">The hull shader bytecode.</param>
    /// <param name="domainShader">The domain shader bytecode.</param>
    /// <param name="inputElements">The generated D3D12 input elements.</param>
    /// <param name="colorTargetCount">The number of color targets.</param>
    /// <param name="depthStencilFormat">The depth/stencil DXGI format.</param>
    /// <returns>The cache key used by the D3D12 pipeline-state cache.</returns>
    private static string BuildGraphicsPipelineStateCacheKey(ref GraphicsPipelineDescription description, string rootSignatureKey, ReadOnlyMemory<byte> vertexShader, ReadOnlyMemory<byte> pixelShader, ReadOnlyMemory<byte> geometryShader, ReadOnlyMemory<byte> hullShader, ReadOnlyMemory<byte> domainShader, InputElementDescription[] inputElements, int colorTargetCount, Format depthStencilFormat) {
        StringBuilder sb = new(512);
        sb.Append("gpso|");
        sb.Append(rootSignatureKey);
        sb.Append("|pt=");
        sb.Append((int)description.PrimitiveTopology);
        sb.Append("|ptt=");
        sb.Append((int)D3D12Formats.ToPrimitiveTopologyType(description.PrimitiveTopology));
        sb.Append("|rbm=");
        sb.Append(description.ResourceBindingModel.HasValue ? (int)description.ResourceBindingModel.Value : -1);
        sb.Append("|blend=");
        AppendBlendState(sb, ref description.BlendState);
        sb.Append("|depth=");
        AppendDepthStencilState(sb, ref description.DepthStencilState);
        sb.Append("|rast=");
        AppendRasterizerState(sb, ref description.RasterizerState);
        sb.Append("|vl=");
        AppendVertexLayouts(sb, description.ShaderSet.VertexLayouts);
        sb.Append("|iel=");
        sb.Append(inputElements?.Length ?? 0);
        sb.Append("|out=");
        AppendOutputDescription(sb, ref description.Outputs, colorTargetCount, depthStencilFormat);
        sb.Append("|vs");
        AppendShaderHash(sb, vertexShader);
        sb.Append("|ps");
        AppendShaderHash(sb, pixelShader);
        sb.Append("|gs");
        AppendShaderHash(sb, geometryShader);
        sb.Append("|hs");
        AppendShaderHash(sb, hullShader);
        sb.Append("|ds");
        AppendShaderHash(sb, domainShader);
        return sb.ToString();
    }

    /// <summary>
    /// Appends blend state values to a pipeline-state cache key.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="description">The blend state description.</param>
    private static void AppendBlendState(StringBuilder sb, ref BlendStateDescription description) {
        sb.Append(description.AlphaToCoverageEnabled ? '1' : '0');
        sb.Append(':');
        sb.Append(description.BlendFactor.R);
        sb.Append(',');
        sb.Append(description.BlendFactor.G);
        sb.Append(',');
        sb.Append(description.BlendFactor.B);
        sb.Append(',');
        sb.Append(description.BlendFactor.A);
        BlendAttachmentDescription[] attachments = description.AttachmentStates;
        sb.Append(':');
        sb.Append(attachments?.Length ?? 0);
        if (attachments == null) {
            return;
        }

        for (int i = 0; i < attachments.Length; i++) {
            BlendAttachmentDescription attachment = attachments[i];
            sb.Append(';');
            sb.Append(attachment.BlendEnabled ? '1' : '0');
            sb.Append(',');
            sb.Append(attachment.ColorWriteMask.HasValue ? (int)attachment.ColorWriteMask.Value : -1);
            sb.Append(',');
            sb.Append((int)attachment.SourceColorFactor);
            sb.Append(',');
            sb.Append((int)attachment.DestinationColorFactor);
            sb.Append(',');
            sb.Append((int)attachment.ColorFunction);
            sb.Append(',');
            sb.Append((int)attachment.SourceAlphaFactor);
            sb.Append(',');
            sb.Append((int)attachment.DestinationAlphaFactor);
            sb.Append(',');
            sb.Append((int)attachment.AlphaFunction);
        }
    }

    /// <summary>
    /// Appends depth/stencil state values to a pipeline-state cache key.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="description">The depth/stencil state description.</param>
    private static void AppendDepthStencilState(StringBuilder sb, ref DepthStencilStateDescription description) {
        sb.Append(description.DepthTestEnabled ? '1' : '0');
        sb.Append(',');
        sb.Append(description.DepthWriteEnabled ? '1' : '0');
        sb.Append(',');
        sb.Append((int)description.DepthComparison);
        sb.Append(',');
        sb.Append(description.StencilTestEnabled ? '1' : '0');
        sb.Append(',');
        sb.Append(description.StencilReadMask);
        sb.Append(',');
        sb.Append(description.StencilWriteMask);
        sb.Append(',');
        sb.Append(description.StencilReference);
        sb.Append(',');
        sb.Append(description.StencilFront.GetHashCode());
        sb.Append(',');
        sb.Append(description.StencilBack.GetHashCode());
    }

    /// <summary>
    /// Appends rasterizer state values to a pipeline-state cache key.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="description">The rasterizer state description.</param>
    private static void AppendRasterizerState(StringBuilder sb, ref RasterizerStateDescription description) {
        sb.Append((int)description.CullMode);
        sb.Append(',');
        sb.Append((int)description.FillMode);
        sb.Append(',');
        sb.Append((int)description.FrontFace);
        sb.Append(',');
        sb.Append(description.DepthClipEnabled ? '1' : '0');
        sb.Append(',');
        sb.Append(description.ScissorTestEnabled ? '1' : '0');
    }

    /// <summary>
    /// Appends vertex layout values to a pipeline-state cache key.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="layouts">The vertex layouts to append.</param>
    private static void AppendVertexLayouts(StringBuilder sb, VertexLayoutDescription[] layouts) {
        sb.Append(layouts?.Length ?? 0);
        if (layouts == null) {
            return;
        }

        for (int layoutIndex = 0; layoutIndex < layouts.Length; layoutIndex++) {
            VertexLayoutDescription layout = layouts[layoutIndex];
            sb.Append(';');
            sb.Append(layout.Stride);
            sb.Append(',');
            sb.Append(layout.InstanceStepRate);
            VertexElementDescription[] elements = layout.Elements;
            sb.Append(',');
            sb.Append(elements?.Length ?? 0);
            if (elements == null) {
                continue;
            }

            for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
                VertexElementDescription element = elements[elementIndex];
                sb.Append('[');
                sb.Append(element.Name);
                sb.Append(',');
                sb.Append((int)element.Semantic);
                sb.Append(',');
                sb.Append((int)element.Format);
                sb.Append(',');
                sb.Append(element.Offset);
                sb.Append(']');
            }
        }
    }

    /// <summary>
    /// Appends output attachment values to a pipeline-state cache key.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="description">The output description.</param>
    /// <param name="colorTargetCount">The number of color targets.</param>
    /// <param name="depthStencilFormat">The depth/stencil DXGI format.</param>
    private static void AppendOutputDescription(StringBuilder sb, ref OutputDescription description, int colorTargetCount, Format depthStencilFormat) {
        sb.Append((int)description.SampleCount);
        sb.Append(',');
        sb.Append(colorTargetCount);
        sb.Append(',');
        sb.Append((int)depthStencilFormat);
        OutputAttachmentDescription[] colors = description.ColorAttachments;
        for (int i = 0; i < colorTargetCount; i++) {
            sb.Append(',');
            sb.Append((int)colors[i].Format);
        }
    }

    /// <summary>
    /// Appends a shader bytecode hash to a pipeline-state cache key.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="shaderBytes">The shader bytecode.</param>
    private static void AppendShaderHash(StringBuilder sb, byte[] shaderBytes) {
        AppendShaderHash(sb, new ReadOnlyMemory<byte>(shaderBytes ?? Array.Empty<byte>()));
    }

    /// <summary>
    /// Appends a shader bytecode hash to a pipeline-state cache key.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="shaderBytes">The shader bytecode.</param>
    private static void AppendShaderHash(StringBuilder sb, ReadOnlyMemory<byte> shaderBytes) {
        ReadOnlySpan<byte> span = shaderBytes.Span;
        ulong hash = 14695981039346656037UL;
        for (int i = 0; i < span.Length; i++) {
            hash ^= span[i];
            hash *= 1099511628211UL;
        }

        sb.Append(':');
        sb.Append(span.Length);
        sb.Append(':');
        sb.Append(hash.ToString("X16"));
    }

    /// <summary>
    /// Creates the compute pipeline state instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    private void CreateComputePipelineState(ref ComputePipelineDescription description) {
        D3D12Shader d3D12Shader = Util.AssertSubtype<Shader, D3D12Shader>(description.ComputeShader);
        ComputePipelineStateDescription psoDescription = new() {
            RootSignature = this.RootSignature,
            ComputeShader = d3D12Shader.ShaderBytes
        };

        string cacheKey = BuildComputePipelineStateCacheKey(this._rootSignatureCacheKey, d3D12Shader.ShaderBytes);
        this.PipelineState = this._gd.GetOrCreatePipelineState(cacheKey, () => this._gd.Device.CreateComputePipelineState(psoDescription));
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
            D3D12Shader d3D12Shader = Util.AssertSubtype<Shader, D3D12Shader>(shader);
            ReadOnlyMemory<byte> bytecode = d3D12Shader.ShaderBytes;
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
            RasterizerState = BuildRasterizerState(ref description.RasterizerState, description.Outputs.SampleCount),
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
            string cacheKey = BuildGraphicsPipelineStateCacheKey(ref description, this._rootSignatureCacheKey, vertexShader, pixelShader, geometryShader, hullShader, domainShader, inputElements, colorCount, psoDescription.DepthStencilFormat);
            this.PipelineState = this._gd.GetOrCreatePipelineState(cacheKey, () => this._gd.Device.CreateGraphicsPipelineState(psoDescription));
        }
        catch (Exception ex) {
            string removedReason = this._gd.GetDeviceRemovedReasonDescription();
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
            BlendAttachmentDescription attachment = description.AttachmentStates![i];
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
    /// <param name="sampleCount">The sample count used by the output attachments.</param>
    /// <returns>The value produced by this operation.</returns>
    private static RasterizerDescription BuildRasterizerState(ref RasterizerStateDescription description, TextureSampleCount sampleCount) {
        return new RasterizerDescription {
            FillMode = D3D12Formats.ToFillMode(description.FillMode),
            CullMode = D3D12Formats.ToCullMode(description.CullMode),
            FrontCounterClockwise = description.FrontFace == FrontFace.CounterClockwise,
            DepthClipEnable = description.DepthClipEnabled,
            MultisampleEnable = sampleCount != TextureSampleCount.Count1,
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
    /// Identifies which grouped descriptor table contains a root binding.
    /// </summary>
    internal enum DescriptorTableKind {

        /// <summary>
        /// The binding is not stored in a descriptor table.
        /// </summary>
        None,

        /// <summary>
        /// The binding is stored in the SRV/UAV descriptor heap.
        /// </summary>
        SrvUav,

        /// <summary>
        /// The binding is stored in the sampler descriptor heap.
        /// </summary>
        Sampler
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
        /// <param name="descriptorTableKind">The descriptor table kind.</param>
        /// <param name="descriptorTableOffset">The descriptor offset inside the grouped table.</param>
        public RootBindingInfo(uint rootParameterIndex, ResourceKind kind, bool descriptorTable, DescriptorTableKind descriptorTableKind, uint descriptorTableOffset) {
            this.RootParameterIndex = rootParameterIndex;
            this.Kind = kind;
            this.DescriptorTable = descriptorTable;
            this.DescriptorTableKind = descriptorTableKind;
            this.DescriptorTableOffset = descriptorTableOffset;
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

        /// <summary>
        /// Gets the grouped descriptor table kind.
        /// </summary>
        public DescriptorTableKind DescriptorTableKind { get; }

        /// <summary>
        /// Gets the descriptor offset inside the grouped descriptor table.
        /// </summary>
        public uint DescriptorTableOffset { get; }
    }

    /// <summary>
    /// Stores a descriptor binding while a grouped root descriptor table is being built.
    /// </summary>
    private readonly struct PendingDescriptorTableBinding {

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingDescriptorTableBinding" /> struct.
        /// </summary>
        /// <param name="elementIndex">The resource layout element index.</param>
        /// <param name="kind">The resource kind.</param>
        /// <param name="rangeType">The D3D12 descriptor range type.</param>
        /// <param name="shaderRegister">The shader register assigned to the resource.</param>
        /// <param name="registerSpace">The register space assigned to the resource.</param>
        /// <param name="tableOffset">The descriptor offset inside the table.</param>
        public PendingDescriptorTableBinding(uint elementIndex, ResourceKind kind, DescriptorRangeType rangeType, uint shaderRegister, uint registerSpace, uint tableOffset) {
            this.ElementIndex = elementIndex;
            this.Kind = kind;
            this.RangeType = rangeType;
            this.ShaderRegister = shaderRegister;
            this.RegisterSpace = registerSpace;
            this.TableOffset = tableOffset;
        }

        /// <summary>
        /// Gets the resource layout element index.
        /// </summary>
        public uint ElementIndex { get; }

        /// <summary>
        /// Gets the resource kind.
        /// </summary>
        public ResourceKind Kind { get; }

        /// <summary>
        /// Gets the D3D12 descriptor range type.
        /// </summary>
        public DescriptorRangeType RangeType { get; }

        /// <summary>
        /// Gets the shader register assigned to the resource.
        /// </summary>
        public uint ShaderRegister { get; }

        /// <summary>
        /// Gets the register space assigned to the resource.
        /// </summary>
        public uint RegisterSpace { get; }

        /// <summary>
        /// Gets the descriptor offset inside the table.
        /// </summary>
        public uint TableOffset { get; }
    }
}
