using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlPipeline.
/// </summary>
internal class MtlPipeline : Pipeline {

    /// <summary>
    /// Stores the maximum inline-byte payload size supported for push constants.
    /// </summary>
    private const uint MaxPushConstantSizeInBytesValue = 4096;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the specialized functions state used by this instance.
    /// </summary>
    private List<MTLFunction> _specializedFunctions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlPipeline" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlPipeline(ref GraphicsPipelineDescription description, MtlGraphicsDevice gd) : base(ref description) {
        this.PrimitiveType = MtlFormats.VdToMtlPrimitiveTopology(description.PrimitiveTopology);
        this.ResourceLayouts = new MtlResourceLayout[description.ResourceLayouts.Length];
        this.NonVertexBufferCount = 0;

        for (int i = 0; i < this.ResourceLayouts.Length; i++) {
            this.ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.ResourceLayouts[i]);
            this.NonVertexBufferCount += this.ResourceLayouts[i].BufferCount;
        }

        this.ResourceBindingModel = description.ResourceBindingModel ?? gd.ResourceBindingModel;

        this.CullMode = MtlFormats.VdToMtlCullMode(description.RasterizerState.CullMode);
        this.FrontFace = MtlFormats.VdVoMtlFrontFace(description.RasterizerState.FrontFace);
        this.FillMode = MtlFormats.VdToMtlFillMode(description.RasterizerState.FillMode);
        this.ScissorTestEnabled = description.RasterizerState.ScissorTestEnabled;

        MTLRenderPipelineDescriptor mtlDesc = MTLRenderPipelineDescriptor.New();

        foreach (Shader shader in description.ShaderSet.Shaders) {
            MtlShader mtlShader = Util.AssertSubtype<Shader, MtlShader>(shader);
            MTLFunction specializedFunction;

            if (mtlShader.HasFunctionConstants) {
                // Need to create specialized MTLFunction.
                MTLFunctionConstantValues constantValues = this.CreateConstantValues(description.ShaderSet.Specializations);
                specializedFunction = mtlShader.Library.NewFunctionWithNameConstantValues(mtlShader.EntryPoint, constantValues);
                this.AddSpecializedFunction(specializedFunction);
                ObjectiveCRuntime.Release(constantValues.NativePtr);

                Debug.Assert(specializedFunction.NativePtr != IntPtr.Zero, "Failed to create specialized MTLFunction");
            }
            else {
                specializedFunction = mtlShader.Function;
            }

            if (shader.Stage == ShaderStages.Vertex) {
                mtlDesc.VertexFunction = specializedFunction;
            }
            else if (shader.Stage == ShaderStages.Fragment) {
                mtlDesc.FragmentFunction = specializedFunction;
            }
        }

        // Vertex layouts
        VertexLayoutDescription[] vdVertexLayouts = description.ShaderSet.VertexLayouts;
        MTLVertexDescriptor vertexDescriptor = mtlDesc.VertexDescriptor;

        for (uint i = 0; i < vdVertexLayouts.Length; i++) {
            uint layoutIndex = this.ResourceBindingModel == ResourceBindingModel.Improved ? this.NonVertexBufferCount + i : i;
            MTLVertexBufferLayoutDescriptor mtlLayout = vertexDescriptor.Layouts[layoutIndex];
            mtlLayout.Stride = vdVertexLayouts[i].Stride;
            uint stepRate = vdVertexLayouts[i].InstanceStepRate;
            mtlLayout.StepFunction = stepRate == 0 ? MTLVertexStepFunction.PerVertex : MTLVertexStepFunction.PerInstance;
            mtlLayout.StepRate = Math.Max(1, stepRate);
        }

        uint element = 0;

        for (uint i = 0; i < vdVertexLayouts.Length; i++) {
            uint offset = 0;
            VertexLayoutDescription vdDesc = vdVertexLayouts[i];

            for (uint j = 0; j < vdDesc.Elements.Length; j++) {
                VertexElementDescription elementDesc = vdDesc.Elements[j];
                MTLVertexAttributeDescriptor mtlAttribute = vertexDescriptor.Attributes[element];
                mtlAttribute.BufferIndex = this.ResourceBindingModel == ResourceBindingModel.Improved
                    ? this.NonVertexBufferCount + i
                    : i;
                mtlAttribute.Format = MtlFormats.VdToMtlVertexFormat(elementDesc.Format);
                mtlAttribute.Offset = elementDesc.Offset != 0 ? elementDesc.Offset : (UIntPtr)offset;
                offset += FormatSizeHelpers.GetSizeInBytes(elementDesc.Format);
                element += 1;
            }
        }

        this.VertexBufferCount = (uint)vdVertexLayouts.Length;
        this.VertexPushConstantSlot = this.NonVertexBufferCount + this.VertexBufferCount;
        this.FragmentPushConstantSlot = this.NonVertexBufferCount;

        // Outputs
        OutputDescription outputs = description.Outputs;
        BlendStateDescription blendStateDesc = description.BlendState;
        this.BlendColor = blendStateDesc.BlendFactor;

        if (outputs.SampleCount != TextureSampleCount.Count1) {
            mtlDesc.SampleCount = FormatHelpers.GetSampleCountUInt32(outputs.SampleCount);
        }

        if (outputs.DepthAttachment != null) {
            PixelFormat depthFormat = outputs.DepthAttachment.Value.Format;
            MTLPixelFormat mtlDepthFormat = MtlFormats.VdToMtlPixelFormat(depthFormat, true);
            mtlDesc.DepthAttachmentPixelFormat = mtlDepthFormat;

            if (FormatHelpers.IsStencilFormat(depthFormat)) {
                this.HasStencil = true;
                mtlDesc.StencilAttachmentPixelFormat = mtlDepthFormat;
            }
        }

        for (uint i = 0; i < outputs.ColorAttachments.Length; i++) {
            BlendAttachmentDescription attachmentBlendDesc = blendStateDesc.AttachmentStates[i];
            MTLRenderPipelineColorAttachmentDescriptor colorDesc = mtlDesc.ColorAttachments[i];
            colorDesc.PixelFormat = MtlFormats.VdToMtlPixelFormat(outputs.ColorAttachments[i].Format, false);
            colorDesc.BlendingEnabled = attachmentBlendDesc.BlendEnabled;
            colorDesc.WriteMask = MtlFormats.VdToMtlColorWriteMask(attachmentBlendDesc.ColorWriteMask.GetOrDefault());
            colorDesc.AlphaBlendOperation = MtlFormats.VdToMtlBlendOp(attachmentBlendDesc.AlphaFunction);
            colorDesc.SourceAlphaBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.SourceAlphaFactor);
            colorDesc.DestinationAlphaBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.DestinationAlphaFactor);

            colorDesc.RgbBlendOperation = MtlFormats.VdToMtlBlendOp(attachmentBlendDesc.ColorFunction);
            colorDesc.SourceRgbBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.SourceColorFactor);
            colorDesc.DestinationRgbBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.DestinationColorFactor);
        }

        mtlDesc.AlphaToCoverageEnabled = blendStateDesc.AlphaToCoverageEnabled;

        this.RenderPipelineState = gd.Device.NewRenderPipelineStateWithDescriptor(mtlDesc);
        ObjectiveCRuntime.Release(mtlDesc.NativePtr);

        if (description.Outputs.DepthAttachment != null) {
            MTLDepthStencilDescriptor depthDescriptor = MTLUtil.AllocInit<MTLDepthStencilDescriptor>(nameof(MTLDepthStencilDescriptor));
            // Metal has no explicit depth-test enable flag on the depth-stencil descriptor.
            // When depth testing is disabled we must force "always" to avoid unintentionally
            // rejecting all fragments if DepthComparison is left at its default (Never).
            depthDescriptor.DepthCompareFunction = MtlFormats.VdToMtlCompareFunction(description.DepthStencilState.DepthTestEnabled
                    ? description.DepthStencilState.DepthComparison
                    : ComparisonKind.Always);
            depthDescriptor.DepthWriteEnabled = description.DepthStencilState.DepthWriteEnabled;

            bool stencilEnabled = description.DepthStencilState.StencilTestEnabled;

            if (stencilEnabled) {
                this.StencilReference = description.DepthStencilState.StencilReference;

                StencilBehaviorDescription vdFrontDesc = description.DepthStencilState.StencilFront;
                MTLStencilDescriptor front = MTLUtil.AllocInit<MTLStencilDescriptor>(nameof(MTLStencilDescriptor));
                front.ReadMask = description.DepthStencilState.StencilReadMask;
                front.WriteMask = description.DepthStencilState.StencilWriteMask;
                front.DepthFailureOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.DepthFail);
                front.StencilFailureOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.Fail);
                front.DepthStencilPassOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.Pass);
                front.StencilCompareFunction = MtlFormats.VdToMtlCompareFunction(vdFrontDesc.Comparison);
                depthDescriptor.FrontFaceStencil = front;

                StencilBehaviorDescription vdBackDesc = description.DepthStencilState.StencilBack;
                MTLStencilDescriptor back = MTLUtil.AllocInit<MTLStencilDescriptor>(nameof(MTLStencilDescriptor));
                back.ReadMask = description.DepthStencilState.StencilReadMask;
                back.WriteMask = description.DepthStencilState.StencilWriteMask;
                back.DepthFailureOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.DepthFail);
                back.StencilFailureOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.Fail);
                back.DepthStencilPassOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.Pass);
                back.StencilCompareFunction = MtlFormats.VdToMtlCompareFunction(vdBackDesc.Comparison);
                depthDescriptor.BackFaceStencil = back;

                ObjectiveCRuntime.Release(front.NativePtr);
                ObjectiveCRuntime.Release(back.NativePtr);
            }

            this.DepthStencilState = gd.Device.NewDepthStencilStateWithDescriptor(depthDescriptor);
            ObjectiveCRuntime.Release(depthDescriptor.NativePtr);
        }

        this.DepthClipMode = description.DepthStencilState.DepthTestEnabled
            ? MTLDepthClipMode.Clip
            : MTLDepthClipMode.Clamp;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlPipeline" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlPipeline(ref ComputePipelineDescription description, MtlGraphicsDevice gd) : base(ref description) {
        this.IsComputePipeline = true;
        this.ResourceLayouts = new MtlResourceLayout[description.ResourceLayouts.Length];

        for (int i = 0; i < this.ResourceLayouts.Length; i++) {
            this.ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.ResourceLayouts[i]);
        }

        this.ThreadsPerThreadgroup = new MTLSize(description.ThreadGroupSizeX, description.ThreadGroupSizeY, description.ThreadGroupSizeZ);

        MTLComputePipelineDescriptor mtlDesc = MTLUtil.AllocInit<MTLComputePipelineDescriptor>(nameof(MTLComputePipelineDescriptor));
        MtlShader mtlShader = Util.AssertSubtype<Shader, MtlShader>(description.ComputeShader);
        MTLFunction specializedFunction;

        if (mtlShader.HasFunctionConstants) {
            // Need to create specialized MTLFunction.
            MTLFunctionConstantValues constantValues = this.CreateConstantValues(description.Specializations);
            specializedFunction = mtlShader.Library.NewFunctionWithNameConstantValues(mtlShader.EntryPoint, constantValues);
            this.AddSpecializedFunction(specializedFunction);
            ObjectiveCRuntime.Release(constantValues.NativePtr);

            Debug.Assert(specializedFunction.NativePtr != IntPtr.Zero, "Failed to create specialized MTLFunction");
        }
        else {
            specializedFunction = mtlShader.Function;
        }

        mtlDesc.ComputeFunction = specializedFunction;
        MTLPipelineBufferDescriptorArray buffers = mtlDesc.Buffers;
        uint bufferIndex = 0;

        foreach (MtlResourceLayout layout in this.ResourceLayouts) {
            foreach (ResourceLayoutElementDescription rle in layout.Description.Elements) {
                ResourceKind kind = rle.Kind;

                if (kind == ResourceKind.UniformBuffer
                    || kind == ResourceKind.StructuredBufferReadOnly) {
                    MTLPipelineBufferDescriptor bufferDesc = buffers[bufferIndex];
                    bufferDesc.mutability = MTLMutability.Immutable;
                    bufferIndex += 1;
                }
                else if (kind == ResourceKind.StructuredBufferReadWrite) {
                    MTLPipelineBufferDescriptor bufferDesc = buffers[bufferIndex];
                    bufferDesc.mutability = MTLMutability.Mutable;
                    bufferIndex += 1;
                }
            }
        }

        this.ComputePushConstantSlot = bufferIndex;

        this.ComputePipelineState = gd.Device.NewComputePipelineStateWithDescriptor(mtlDesc);
        ObjectiveCRuntime.Release(mtlDesc.NativePtr);
    }

    /// <summary>
    /// Gets or sets RenderPipelineState.
    /// </summary>
    public MTLRenderPipelineState RenderPipelineState { get; }

    /// <summary>
    /// Gets or sets ComputePipelineState.
    /// </summary>
    public MTLComputePipelineState ComputePipelineState { get; }

    /// <summary>
    /// Gets or sets PrimitiveType.
    /// </summary>
    public MTLPrimitiveType PrimitiveType { get; }

    /// <summary>
    /// Gets or sets ResourceLayouts.
    /// </summary>
    public new MtlResourceLayout[] ResourceLayouts { get; }

    /// <summary>
    /// Gets or sets ResourceBindingModel.
    /// </summary>
    public ResourceBindingModel ResourceBindingModel { get; }

    /// <summary>
    /// Gets or sets VertexBufferCount.
    /// </summary>
    public uint VertexBufferCount { get; }

    /// <summary>
    /// Gets or sets NonVertexBufferCount.
    /// </summary>
    public uint NonVertexBufferCount { get; }

    /// <summary>
    /// Gets the vertex-stage buffer slot reserved for push constants.
    /// </summary>
    public uint VertexPushConstantSlot { get; }

    /// <summary>
    /// Gets the fragment-stage buffer slot reserved for push constants.
    /// </summary>
    public uint FragmentPushConstantSlot { get; }

    /// <summary>
    /// Gets the compute-stage buffer slot reserved for push constants.
    /// </summary>
    public uint ComputePushConstantSlot { get; }

    /// <summary>
    /// Gets the maximum push-constant payload size, in bytes, supported by this backend.
    /// </summary>
    public uint MaxPushConstantSizeInBytes => MaxPushConstantSizeInBytesValue;

    /// <summary>
    /// Gets or sets CullMode.
    /// </summary>
    public MTLCullMode CullMode { get; }

    /// <summary>
    /// Gets or sets FrontFace.
    /// </summary>
    public MTLWinding FrontFace { get; }

    /// <summary>
    /// Gets or sets FillMode.
    /// </summary>
    public MTLTriangleFillMode FillMode { get; }

    /// <summary>
    /// Gets or sets DepthStencilState.
    /// </summary>
    public MTLDepthStencilState DepthStencilState { get; }

    /// <summary>
    /// Gets or sets DepthClipMode.
    /// </summary>
    public MTLDepthClipMode DepthClipMode { get; }

    /// <summary>
    /// Gets or sets IsComputePipeline.
    /// </summary>
    public override bool IsComputePipeline { get; }

    /// <summary>
    /// Gets or sets ScissorTestEnabled.
    /// </summary>
    public bool ScissorTestEnabled { get; }

    /// <summary>
    /// Stores the threads per threadgroup state used by this instance.
    /// </summary>
    public MTLSize ThreadsPerThreadgroup { get; } = new(1, 1, 1);

    /// <summary>
    /// Gets or sets HasStencil.
    /// </summary>
    public bool HasStencil { get; }

    /// <summary>
    /// Gets or sets StencilReference.
    /// </summary>
    public uint StencilReference { get; }

    /// <summary>
    /// Gets or sets BlendColor.
    /// </summary>
    public RgbaFloat BlendColor { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            if (this.RenderPipelineState.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.Release(this.RenderPipelineState.NativePtr);
            }

            if (this.DepthStencilState.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.Release(this.DepthStencilState.NativePtr);
            }

            if (this.ComputePipelineState.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.Release(this.ComputePipelineState.NativePtr);
            }

            if (this._specializedFunctions != null) {
                foreach (MTLFunction function in this._specializedFunctions) {
                    ObjectiveCRuntime.Release(function.NativePtr);
                }

                this._specializedFunctions.Clear();
            }

            this._disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Creates the constant values instance used by this backend.
    /// </summary>
    /// <param name="specializations">The specializations value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private unsafe MTLFunctionConstantValues CreateConstantValues(SpecializationConstant[] specializations) {
        MTLFunctionConstantValues ret = MTLFunctionConstantValues.New();

        if (specializations != null) {
            foreach (SpecializationConstant sc in specializations) {
                MTLDataType mtlType = MtlFormats.VdVoMtlShaderConstantType(sc.Type);
                ret.SetConstantValueTypeAtIndex(&sc.Data, mtlType, sc.ID);
            }
        }

        return ret;
    }

    /// <summary>
    /// Executes the add specialized function logic for this backend.
    /// </summary>
    /// <param name="function">The function value used by this operation.</param>
    private void AddSpecializedFunction(MTLFunction function) {
        this._specializedFunctions ??= new List<MTLFunction>();
        this._specializedFunctions.Add(function);
    }
}
