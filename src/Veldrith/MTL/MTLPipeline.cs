using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlPipeline : Pipeline {
    private bool _disposed;
    private List<MTLFunction> _specializedFunctions;

    public MtlPipeline(ref GraphicsPipelineDescription description, MtlGraphicsDevice gd)
        : base(ref description) {
        this.PrimitiveType = MtlFormats.VdToMtlPrimitiveTopology(description.PrimitiveTopology);
        this.ResourceLayouts = new MtlResourceLayout[description.ResourceLayouts.Length];
        this.NonVertexBufferCount = 0;

        for (int i = 0; i < this.ResourceLayouts.Length; i++) {
            this.ResourceLayouts[i] =
                Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.ResourceLayouts[i]);
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
                MTLFunctionConstantValues constantValues =
                    this.CreateConstantValues(description.ShaderSet.Specializations);
                specializedFunction =
                    mtlShader.Library.newFunctionWithNameConstantValues(mtlShader.EntryPoint, constantValues);
                this.AddSpecializedFunction(specializedFunction);
                ObjectiveCRuntime.release(constantValues.NativePtr);

                Debug.Assert(specializedFunction.NativePtr != IntPtr.Zero, "Failed to create specialized MTLFunction");
            }
            else {
                specializedFunction = mtlShader.Function;
            }

            if (shader.Stage == ShaderStages.Vertex) {
                mtlDesc.vertexFunction = specializedFunction;
            }
            else if (shader.Stage == ShaderStages.Fragment) {
                mtlDesc.fragmentFunction = specializedFunction;
            }
        }

        // Vertex layouts
        VertexLayoutDescription[] vdVertexLayouts = description.ShaderSet.VertexLayouts;
        MTLVertexDescriptor vertexDescriptor = mtlDesc.vertexDescriptor;

        for (uint i = 0; i < vdVertexLayouts.Length; i++) {
            uint layoutIndex = this.ResourceBindingModel == ResourceBindingModel.Improved
                ? this.NonVertexBufferCount + i
                : i;
            MTLVertexBufferLayoutDescriptor mtlLayout = vertexDescriptor.layouts[layoutIndex];
            mtlLayout.stride = vdVertexLayouts[i].Stride;
            uint stepRate = vdVertexLayouts[i].InstanceStepRate;
            mtlLayout.stepFunction =
                stepRate == 0 ? MTLVertexStepFunction.PerVertex : MTLVertexStepFunction.PerInstance;
            mtlLayout.stepRate = Math.Max(1, stepRate);
        }

        uint element = 0;

        for (uint i = 0; i < vdVertexLayouts.Length; i++) {
            uint offset = 0;
            VertexLayoutDescription vdDesc = vdVertexLayouts[i];

            for (uint j = 0; j < vdDesc.Elements.Length; j++) {
                VertexElementDescription elementDesc = vdDesc.Elements[j];
                MTLVertexAttributeDescriptor mtlAttribute = vertexDescriptor.attributes[element];
                mtlAttribute.bufferIndex = this.ResourceBindingModel == ResourceBindingModel.Improved
                    ? this.NonVertexBufferCount + i
                    : i;
                mtlAttribute.format = MtlFormats.VdToMtlVertexFormat(elementDesc.Format);
                mtlAttribute.offset = elementDesc.Offset != 0 ? elementDesc.Offset : (UIntPtr)offset;
                offset += FormatSizeHelpers.GetSizeInBytes(elementDesc.Format);
                element += 1;
            }
        }

        this.VertexBufferCount = (uint)vdVertexLayouts.Length;

        // Outputs
        OutputDescription outputs = description.Outputs;
        BlendStateDescription blendStateDesc = description.BlendState;
        this.BlendColor = blendStateDesc.BlendFactor;

        if (outputs.SampleCount != TextureSampleCount.Count1) {
            mtlDesc.sampleCount = FormatHelpers.GetSampleCountUInt32(outputs.SampleCount);
        }

        if (outputs.DepthAttachment != null) {
            PixelFormat depthFormat = outputs.DepthAttachment.Value.Format;
            MTLPixelFormat mtlDepthFormat = MtlFormats.VdToMtlPixelFormat(depthFormat, true);
            mtlDesc.depthAttachmentPixelFormat = mtlDepthFormat;

            if (FormatHelpers.IsStencilFormat(depthFormat)) {
                this.HasStencil = true;
                mtlDesc.stencilAttachmentPixelFormat = mtlDepthFormat;
            }
        }

        for (uint i = 0; i < outputs.ColorAttachments.Length; i++) {
            BlendAttachmentDescription attachmentBlendDesc = blendStateDesc.AttachmentStates[i];
            MTLRenderPipelineColorAttachmentDescriptor colorDesc = mtlDesc.colorAttachments[i];
            colorDesc.pixelFormat = MtlFormats.VdToMtlPixelFormat(outputs.ColorAttachments[i].Format, false);
            colorDesc.blendingEnabled = attachmentBlendDesc.BlendEnabled;
            colorDesc.writeMask = MtlFormats.VdToMtlColorWriteMask(attachmentBlendDesc.ColorWriteMask.GetOrDefault());
            colorDesc.alphaBlendOperation = MtlFormats.VdToMtlBlendOp(attachmentBlendDesc.AlphaFunction);
            colorDesc.sourceAlphaBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.SourceAlphaFactor);
            colorDesc.destinationAlphaBlendFactor =
                MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.DestinationAlphaFactor);

            colorDesc.rgbBlendOperation = MtlFormats.VdToMtlBlendOp(attachmentBlendDesc.ColorFunction);
            colorDesc.sourceRGBBlendFactor = MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.SourceColorFactor);
            colorDesc.destinationRGBBlendFactor =
                MtlFormats.VdToMtlBlendFactor(attachmentBlendDesc.DestinationColorFactor);
        }

        mtlDesc.alphaToCoverageEnabled = blendStateDesc.AlphaToCoverageEnabled;

        this.RenderPipelineState = gd.Device.newRenderPipelineStateWithDescriptor(mtlDesc);
        ObjectiveCRuntime.release(mtlDesc.NativePtr);

        if (description.Outputs.DepthAttachment != null) {
            MTLDepthStencilDescriptor depthDescriptor = MTLUtil.AllocInit<MTLDepthStencilDescriptor>(
                nameof(MTLDepthStencilDescriptor));
            // Metal has no explicit depth-test enable flag on the depth-stencil descriptor.
            // When depth testing is disabled we must force "always" to avoid unintentionally
            // rejecting all fragments if DepthComparison is left at its default (Never).
            depthDescriptor.depthCompareFunction = MtlFormats.VdToMtlCompareFunction(
                description.DepthStencilState.DepthTestEnabled
                    ? description.DepthStencilState.DepthComparison
                    : ComparisonKind.Always);
            depthDescriptor.depthWriteEnabled = description.DepthStencilState.DepthWriteEnabled;

            bool stencilEnabled = description.DepthStencilState.StencilTestEnabled;

            if (stencilEnabled) {
                this.StencilReference = description.DepthStencilState.StencilReference;

                StencilBehaviorDescription vdFrontDesc = description.DepthStencilState.StencilFront;
                MTLStencilDescriptor front = MTLUtil.AllocInit<MTLStencilDescriptor>(nameof(MTLStencilDescriptor));
                front.readMask = description.DepthStencilState.StencilReadMask;
                front.writeMask = description.DepthStencilState.StencilWriteMask;
                front.depthFailureOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.DepthFail);
                front.stencilFailureOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.Fail);
                front.depthStencilPassOperation = MtlFormats.VdToMtlStencilOperation(vdFrontDesc.Pass);
                front.stencilCompareFunction = MtlFormats.VdToMtlCompareFunction(vdFrontDesc.Comparison);
                depthDescriptor.frontFaceStencil = front;

                StencilBehaviorDescription vdBackDesc = description.DepthStencilState.StencilBack;
                MTLStencilDescriptor back = MTLUtil.AllocInit<MTLStencilDescriptor>(nameof(MTLStencilDescriptor));
                back.readMask = description.DepthStencilState.StencilReadMask;
                back.writeMask = description.DepthStencilState.StencilWriteMask;
                back.depthFailureOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.DepthFail);
                back.stencilFailureOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.Fail);
                back.depthStencilPassOperation = MtlFormats.VdToMtlStencilOperation(vdBackDesc.Pass);
                back.stencilCompareFunction = MtlFormats.VdToMtlCompareFunction(vdBackDesc.Comparison);
                depthDescriptor.backFaceStencil = back;

                ObjectiveCRuntime.release(front.NativePtr);
                ObjectiveCRuntime.release(back.NativePtr);
            }

            this.DepthStencilState = gd.Device.newDepthStencilStateWithDescriptor(depthDescriptor);
            ObjectiveCRuntime.release(depthDescriptor.NativePtr);
        }

        this.DepthClipMode = description.DepthStencilState.DepthTestEnabled
            ? MTLDepthClipMode.Clip
            : MTLDepthClipMode.Clamp;
    }

    public MtlPipeline(ref ComputePipelineDescription description, MtlGraphicsDevice gd)
        : base(ref description) {
        this.IsComputePipeline = true;
        this.ResourceLayouts = new MtlResourceLayout[description.ResourceLayouts.Length];

        for (int i = 0; i < this.ResourceLayouts.Length; i++) {
            this.ResourceLayouts[i] =
                Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.ResourceLayouts[i]);
        }

        this.ThreadsPerThreadgroup = new MTLSize(
            description.ThreadGroupSizeX,
            description.ThreadGroupSizeY,
            description.ThreadGroupSizeZ);

        MTLComputePipelineDescriptor mtlDesc = MTLUtil.AllocInit<MTLComputePipelineDescriptor>(
            nameof(MTLComputePipelineDescriptor));
        MtlShader mtlShader = Util.AssertSubtype<Shader, MtlShader>(description.ComputeShader);
        MTLFunction specializedFunction;

        if (mtlShader.HasFunctionConstants) {
            // Need to create specialized MTLFunction.
            MTLFunctionConstantValues constantValues = this.CreateConstantValues(description.Specializations);
            specializedFunction =
                mtlShader.Library.newFunctionWithNameConstantValues(mtlShader.EntryPoint, constantValues);
            this.AddSpecializedFunction(specializedFunction);
            ObjectiveCRuntime.release(constantValues.NativePtr);

            Debug.Assert(specializedFunction.NativePtr != IntPtr.Zero, "Failed to create specialized MTLFunction");
        }
        else {
            specializedFunction = mtlShader.Function;
        }

        mtlDesc.computeFunction = specializedFunction;
        MTLPipelineBufferDescriptorArray buffers = mtlDesc.buffers;
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

        this.ComputePipelineState = gd.Device.newComputePipelineStateWithDescriptor(mtlDesc);
        ObjectiveCRuntime.release(mtlDesc.NativePtr);
    }

    public MTLRenderPipelineState RenderPipelineState { get; }
    public MTLComputePipelineState ComputePipelineState { get; }
    public MTLPrimitiveType PrimitiveType { get; }
    public new MtlResourceLayout[] ResourceLayouts { get; }
    public ResourceBindingModel ResourceBindingModel { get; }
    public uint VertexBufferCount { get; }
    public uint NonVertexBufferCount { get; }
    public MTLCullMode CullMode { get; }
    public MTLWinding FrontFace { get; }
    public MTLTriangleFillMode FillMode { get; }
    public MTLDepthStencilState DepthStencilState { get; }
    public MTLDepthClipMode DepthClipMode { get; }
    public override bool IsComputePipeline { get; }
    public bool ScissorTestEnabled { get; }
    public MTLSize ThreadsPerThreadgroup { get; } = new(1, 1, 1);
    public bool HasStencil { get; }
    public uint StencilReference { get; }
    public RgbaFloat BlendColor { get; }
    public override bool IsDisposed => this._disposed;
    public override string Name { get; set; }

    #region Disposal

    public override void Dispose() {
        if (!this._disposed) {
            if (this.RenderPipelineState.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.release(this.RenderPipelineState.NativePtr);
            }

            if (this.DepthStencilState.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.release(this.DepthStencilState.NativePtr);
            }

            if (this.ComputePipelineState.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.release(this.ComputePipelineState.NativePtr);
            }

            if (this._specializedFunctions != null) {
                foreach (MTLFunction function in this._specializedFunctions) {
                    ObjectiveCRuntime.release(function.NativePtr);
                }

                this._specializedFunctions.Clear();
            }

            this._disposed = true;
        }
    }

    #endregion

    private unsafe MTLFunctionConstantValues CreateConstantValues(SpecializationConstant[] specializations) {
        MTLFunctionConstantValues ret = MTLFunctionConstantValues.New();

        if (specializations != null) {
            foreach (SpecializationConstant sc in specializations) {
                MTLDataType mtlType = MtlFormats.VdVoMtlShaderConstantType(sc.Type);
                ret.setConstantValuetypeatIndex(&sc.Data, mtlType, sc.ID);
            }
        }

        return ret;
    }

    private void AddSpecializedFunction(MTLFunction function) {
        this._specializedFunctions ??= new List<MTLFunction>();
        this._specializedFunctions.Add(function);
    }
}