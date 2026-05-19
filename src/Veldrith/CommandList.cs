using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Veldrith;

/// <summary>
/// Represents the CommandList type used by the graphics runtime.
/// </summary>
public abstract class CommandList : IDeviceResource, IDisposable {

    /// <summary>
    /// Stores the structured buffer alignment state used by this instance.
    /// </summary>
    private readonly uint _structuredBufferAlignment;

    /// <summary>
    /// Stores the uniform buffer alignment state used by this instance.
    /// </summary>
    private readonly uint _uniformBufferAlignment;

    /// <summary>
    /// Stores the features state used by this instance.
    /// </summary>
    private readonly GraphicsDeviceFeatures features;

    /// <summary>
    /// Stores the compute pipeline state used by this instance.
    /// </summary>
    private protected Pipeline ComputePipeline;

    /// <summary>
    /// Stores the framebuffer state used by this instance.
    /// </summary>
    private protected Framebuffer Framebuffer;

    /// <summary>
    /// Stores the graphics pipeline state used by this instance.
    /// </summary>
    private protected Pipeline GraphicsPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandList" /> type.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="features">The features value used by this operation.</param>
    /// <param name="uniformAlignment">The uniform alignment value used by this operation.</param>
    /// <param name="structuredAlignment">The structured alignment value used by this operation.</param>
    internal CommandList(ref CommandListDescription description, GraphicsDeviceFeatures features, uint uniformAlignment, uint structuredAlignment) {
        this.features = features;
        this._uniformBufferAlignment = uniformAlignment;
        this._structuredBufferAlignment = structuredAlignment;
    }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public abstract void Dispose();

    #endregion

    /// <summary>
    /// Begins the value operation.
    /// </summary>
    public abstract void Begin();

    /// <summary>
    /// Ends the value operation.
    /// </summary>
    public abstract void End();

    /// <summary>
    /// Sets the pipeline value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    public void SetPipeline(Pipeline pipeline) {
        if (pipeline.IsComputePipeline) {
            this.ComputePipeline = pipeline;
        }
        else {
            this.GraphicsPipeline = pipeline;
        }

        this.SetPipelineCore(pipeline);
    }

    /// <summary>
    /// Uploads raw push-constant data to the currently active pipeline.
    /// </summary>
    /// <param name="offset">The byte offset inside the pipeline's push-constant range.</param>
    /// <param name="data">A pointer to the source data to copy.</param>
    /// <param name="sizeInBytes">The number of bytes to upload.</param>
    public void PushConstants(uint offset, IntPtr data, uint sizeInBytes) {
        if (!this.features.PushConstants) {
            throw new VeldridException("Push constants are not supported by this GraphicsDevice.");
        }

        if (sizeInBytes == 0) {
            return;
        }

#if VALIDATE_USAGE
        if (this.GraphicsPipeline == null && this.ComputePipeline == null) {
            throw new VeldridException("A Pipeline must be active before push constants can be set.");
        }
#endif

        this.PushConstantsCore(offset, data, sizeInBytes);
    }

    /// <summary>
    /// Uploads a blittable value as push constants to the currently active pipeline.
    /// </summary>
    /// <typeparam name="T">The unmanaged value type to upload.</typeparam>
    /// <param name="offset">The byte offset inside the pipeline's push-constant range.</param>
    /// <param name="value">The value to copy into push constants.</param>
    public unsafe void PushConstants<T>(uint offset, in T value) where T : unmanaged {
        fixed (T* valuePtr = &value) {
            this.PushConstants(offset, (IntPtr)valuePtr, Util.USizeOf<T>());
        }
    }

    /// <summary>
    /// Sets the vertex buffer value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    public void SetVertexBuffer(uint index, DeviceBuffer buffer) {
        this.SetVertexBuffer(index, buffer, 0);
    }

    /// <summary>
    /// Sets the vertex buffer value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    public void SetVertexBuffer(uint index, DeviceBuffer buffer, uint offset) {
#if VALIDATE_USAGE
        if ((buffer.Usage & BufferUsage.VertexBuffer) == 0) {
            throw new VeldridException("Buffer cannot be bound as a vertex buffer because it was not created with BufferUsage.VertexBuffer.");
        }
#endif
        this.SetVertexBufferCore(index, buffer, offset);
    }

    /// <summary>
    /// Sets the index buffer value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    public void SetIndexBuffer(DeviceBuffer buffer, IndexFormat format) {
        this.SetIndexBuffer(buffer, format, 0);
    }

    /// <summary>
    /// Sets the index buffer value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    public void SetIndexBuffer(DeviceBuffer buffer, IndexFormat format, uint offset) {
#if VALIDATE_USAGE
        if ((buffer.Usage & BufferUsage.IndexBuffer) == 0) {
            throw new VeldridException("Buffer cannot be bound as an index buffer because it was not created with BufferUsage.IndexBuffer.");
        }

        this.indexBuffer = buffer;
        this.indexFormat = format;
#endif
        this.SetIndexBufferCore(buffer, format, offset);
    }

    /// <summary>
    /// Sets the graphics resource set value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    public unsafe void SetGraphicsResourceSet(uint slot, ResourceSet rs) {
        this.SetGraphicsResourceSet(slot, rs, 0, ref Unsafe.AsRef<uint>(null));
    }

    /// <summary>
    /// Sets the graphics resource set value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    public void SetGraphicsResourceSet(uint slot, ResourceSet rs, uint[] dynamicOffsets) {
        this.SetGraphicsResourceSet(slot, rs, (uint)dynamicOffsets.Length, ref dynamicOffsets[0]);
    }

    /// <summary>
    /// Sets the graphics resource set value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    public void SetGraphicsResourceSet(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
#if VALIDATE_USAGE
        if (this.GraphicsPipeline == null) {
            throw new VeldridException($"A graphics Pipeline must be active before {nameof(SetGraphicsResourceSet)} can be called.");
        }

        int layoutsCount = this.GraphicsPipeline.ResourceLayouts.Length;

        if (layoutsCount <= slot) {
            throw new VeldridException($"Failed to bind ResourceSet to slot {slot}. The active graphics Pipeline only contains {layoutsCount} ResourceLayouts.");
        }

        ResourceLayout layout = this.GraphicsPipeline.ResourceLayouts[slot];
        int pipelineLength = layout.Description.Elements.Length;
        ResourceLayoutDescription layoutDesc = rs.Layout.Description;
        int setLength = layoutDesc.Elements.Length;

        if (pipelineLength != setLength) {
            throw new VeldridException($"Failed to bind ResourceSet to slot {slot}. The number of resources in the ResourceSet ({setLength}) does not match the number expected by the active Pipeline ({pipelineLength}).");
        }

        for (int i = 0; i < pipelineLength; i++) {
            ResourceKind pipelineKind = layout.Description.Elements[i].Kind;
            ResourceKind setKind = layoutDesc.Elements[i].Kind;

            if (pipelineKind != setKind) {
                throw new VeldridException($"Failed to bind ResourceSet to slot {slot}. Resource element {i} was of the incorrect type. The bound Pipeline expects {pipelineKind}, but the ResourceSet contained {setKind}.");
            }
        }

        if (rs.Layout.DynamicBufferCount != dynamicOffsetsCount) {
            throw new VeldridException("A dynamic offset must be provided for each resource that specifies " + $"{nameof(ResourceLayoutElementOptions)}.{nameof(ResourceLayoutElementOptions.DynamicBinding)}. " + $"{rs.Layout.DynamicBufferCount} offsets were expected, but only {dynamicOffsetsCount} were provided.");
        }

        uint dynamicOffsetIndex = 0;

        for (uint i = 0; i < layoutDesc.Elements.Length; i++) {
            if ((layoutDesc.Elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0) {
                uint requiredAlignment = layoutDesc.Elements[i].Kind == ResourceKind.UniformBuffer
                    ? this._uniformBufferAlignment
                    : this._structuredBufferAlignment;
                uint desiredOffset = Unsafe.Add(ref dynamicOffsets, (int)dynamicOffsetIndex);
                dynamicOffsetIndex += 1;
                DeviceBufferRange range = Util.GetBufferRange(rs.Resources[i], desiredOffset);

                if (range.Offset % requiredAlignment != 0) {
                    throw new VeldridException($"The effective offset of the buffer in slot {i} does not meet the alignment " + $"requirements of this device. The offset must be a multiple of {requiredAlignment}, but it is " + $"{range.Offset}");
                }
            }
        }

#endif
        this.SetGraphicsResourceSetCore(slot, rs, dynamicOffsetsCount, ref dynamicOffsets);
    }

    /// <summary>
    /// Sets the compute resource set value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    public unsafe void SetComputeResourceSet(uint slot, ResourceSet rs) {
        this.SetComputeResourceSet(slot, rs, 0, ref Unsafe.AsRef<uint>(null));
    }

    /// <summary>
    /// Sets the compute resource set value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    public void SetComputeResourceSet(uint slot, ResourceSet rs, uint[] dynamicOffsets) {
        this.SetComputeResourceSet(slot, rs, (uint)dynamicOffsets.Length, ref dynamicOffsets[0]);
    }

    /// <summary>
    /// Sets the compute resource set value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    public void SetComputeResourceSet(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
#if VALIDATE_USAGE
        if (this.ComputePipeline == null) {
            throw new VeldridException($"A compute Pipeline must be active before {nameof(SetComputeResourceSet)} can be called.");
        }

        int layoutsCount = this.ComputePipeline.ResourceLayouts.Length;

        if (layoutsCount <= slot) {
            throw new VeldridException($"Failed to bind ResourceSet to slot {slot}. The active compute Pipeline only contains {layoutsCount} ResourceLayouts.");
        }

        ResourceLayout layout = this.ComputePipeline.ResourceLayouts[slot];
        int pipelineLength = layout.Description.Elements.Length;
        int setLength = rs.Layout.Description.Elements.Length;

        if (pipelineLength != setLength) {
            throw new VeldridException($"Failed to bind ResourceSet to slot {slot}. The number of resources in the ResourceSet ({setLength}) does not match the number expected by the active Pipeline ({pipelineLength}).");
        }

        for (int i = 0; i < pipelineLength; i++) {
            ResourceKind pipelineKind = layout.Description.Elements[i].Kind;
            ResourceKind setKind = rs.Layout.Description.Elements[i].Kind;

            if (pipelineKind != setKind) {
                throw new VeldridException($"Failed to bind ResourceSet to slot {slot}. Resource element {i} was of the incorrect type. The bound Pipeline expects {pipelineKind}, but the ResourceSet contained {setKind}.");
            }
        }
#endif
        this.SetComputeResourceSetCore(slot, rs, dynamicOffsetsCount, ref dynamicOffsets);
    }

    /// <summary>
    /// Sets the framebuffer value.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
    public void SetFramebuffer(Framebuffer fb) {
        if (this.Framebuffer != fb) {
            this.Framebuffer = fb;
            this.SetFramebufferCore(fb);
            this.SetFullViewports();
            this.SetFullScissorRects();
        }
    }

    /// <summary>
    /// Executes the clear color target logic for this backend.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="clearColor">The clear color value used by this operation.</param>
    public void ClearColorTarget(uint index, RgbaFloat clearColor) {
#if VALIDATE_USAGE
        if (this.Framebuffer == null) {
            throw new VeldridException("Cannot use ClearColorTarget. There is no Framebuffer bound.");
        }

        if (this.Framebuffer.ColorTargets.Count <= index) {
            throw new VeldridException("ClearColorTarget index must be less than the current Framebuffer's color target count.");
        }
#endif
        this.ClearColorTargetCore(index, clearColor);
    }

    /// <summary>
    /// Executes the clear depth stencil logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    public void ClearDepthStencil(float depth) {
        this.ClearDepthStencil(depth, 0);
    }

    /// <summary>
    /// Executes the clear depth stencil logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    public void ClearDepthStencil(float depth, byte stencil) {
#if VALIDATE_USAGE
        if (this.Framebuffer == null) {
            throw new VeldridException("Cannot use ClearDepthStencil. There is no Framebuffer bound.");
        }

        if (this.Framebuffer.DepthTarget == null) {
            throw new VeldridException("The current Framebuffer has no depth target, so ClearDepthStencil cannot be used.");
        }
#endif

        this.ClearDepthStencilCore(depth, stencil);
    }

    /// <summary>
    /// Sets the full viewports value.
    /// </summary>
    public void SetFullViewports() {
        this.SetViewport(0, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));

        for (uint index = 1; index < this.Framebuffer.ColorTargets.Count; index++) {
            this.SetViewport(index, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));
        }
    }

    /// <summary>
    /// Sets the full viewport value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetFullViewport(uint index) {
        this.SetViewport(index, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));
    }

    /// <summary>
    /// Sets the viewport value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="viewport">The viewport value used by this operation.</param>
    public void SetViewport(uint index, Viewport viewport) {
        this.SetViewport(index, ref viewport);
    }

    /// <summary>
    /// Sets the viewport value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="viewport">The viewport value used by this operation.</param>
    public abstract void SetViewport(uint index, ref Viewport viewport);

    /// <summary>
    /// Sets the full scissor rects value.
    /// </summary>
    public void SetFullScissorRects() {
        this.SetScissorRect(0, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);

        for (uint index = 1; index < this.Framebuffer.ColorTargets.Count; index++) {
            this.SetScissorRect(index, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);
        }
    }

    /// <summary>
    /// Sets the full scissor rect value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetFullScissorRect(uint index) {
        this.SetScissorRect(index, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);
    }

    /// <summary>
    /// Sets the scissor rect value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public abstract void SetScissorRect(uint index, uint x, uint y, uint width, uint height);

    /// <summary>
    /// Executes the draw logic for this backend.
    /// </summary>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    public void Draw(uint vertexCount) {
        this.Draw(vertexCount, 1, 0, 0);
    }

    /// <summary>
    /// Executes the draw logic for this backend.
    /// </summary>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    public void Draw(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        this.preDrawValidation();
        this.DrawCore(vertexCount, instanceCount, vertexStart, instanceStart);
    }

    /// <summary>
    /// Executes the draw indexed logic for this backend.
    /// </summary>
    /// <param name="indexCount">The index count value used by this operation.</param>
    public void DrawIndexed(uint indexCount) {
        this.DrawIndexed(indexCount, 1, 0, 0, 0);
    }

    /// <summary>
    /// Executes the draw indexed logic for this backend.
    /// </summary>
    /// <param name="indexCount">The index count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="indexStart">The index start value used by this operation.</param>
    /// <param name="vertexOffset">The vertex offset value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    public void DrawIndexed(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart) {
        this.validateIndexBuffer(indexCount);
        this.preDrawValidation();

#if VALIDATE_USAGE
        if (!this.features.DrawBaseVertex && vertexOffset != 0) {
            throw new VeldridException("Drawing with a non-zero base vertex is not supported on this device.");
        }

        if (!this.features.DrawBaseInstance && instanceStart != 0) {
            throw new VeldridException("Drawing with a non-zero base instance is not supported on this device.");
        }
#endif

        this.DrawIndexedCore(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
    }

    /// <summary>
    /// Executes the draw indirect logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    public unsafe void DrawIndirect(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.validateDrawIndirectSupport();
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        validateIndirectStride(stride, sizeof(IndirectDrawArguments));
        this.preDrawValidation();

        this.DrawIndirectCore(indirectBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the draw indexed indirect logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    public unsafe void DrawIndexedIndirect(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.validateDrawIndirectSupport();
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        validateIndirectStride(stride, sizeof(IndirectDrawIndexedArguments));
        this.preDrawValidation();

        this.DrawIndexedIndirectCore(indirectBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the dispatch logic for this backend.
    /// </summary>
    /// <param name="groupCountX">The group count x value used by this operation.</param>
    /// <param name="groupCountY">The group count y value used by this operation.</param>
    /// <param name="groupCountZ">The group count z value used by this operation.</param>
    public abstract void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ);

    /// <summary>
    /// Executes the dispatch indirect logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    public void DispatchIndirect(DeviceBuffer indirectBuffer, uint offset) {
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        this.DispatchIndirectCore(indirectBuffer, offset);
    }

    /// <summary>
    /// Executes the resolve texture logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    public void ResolveTexture(Texture source, Texture destination) {
#if VALIDATE_USAGE
        if (source.SampleCount == TextureSampleCount.Count1) {
            throw new VeldridException($"The {nameof(source)} parameter of {nameof(this.ResolveTexture)} must be a multisample texture.");
        }

        if (destination.SampleCount != TextureSampleCount.Count1) {
            throw new VeldridException($"The {nameof(destination)} parameter of {nameof(this.ResolveTexture)} must be a non-multisample texture. Instead, it is a texture with {FormatHelpers.GetSampleCountUInt32(source.SampleCount)} samples.");
        }
#endif

        this.ResolveTextureCore(source, destination);
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, T source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)(&source), (uint)sizeof(T));
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, ref T source) where T : unmanaged {
        fixed (T* ptr = &source) {
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)ptr, Util.USizeOf<T>());
        }
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, ref T source, uint sizeInBytes) where T : unmanaged {
        fixed (T* ptr = &source) {
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)ptr, sizeInBytes);
        }
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    public void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, T[] source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)source);
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, ReadOnlySpan<T> source) where T : unmanaged {
        fixed (void* pin = &MemoryMarshal.GetReference(source)) {
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)pin, (uint)(sizeof(T) * source.Length));
        }
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    public void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, Span<T> source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)source);
    }

    /// <summary>
    /// Updates the buffer state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    public void UpdateBuffer(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        if (bufferOffsetInBytes + sizeInBytes > buffer.SizeInBytes) {
            throw new VeldridException($"The DeviceBuffer's capacity ({buffer.SizeInBytes}) is not large enough to store the amount of " + $"data specified ({sizeInBytes}) at the given offset ({bufferOffsetInBytes}).");
        }

        if (sizeInBytes == 0) {
            return;
        }

        this.UpdateBufferCore(buffer, bufferOffsetInBytes, source, sizeInBytes);
    }

    /// <summary>
    /// Copies buffer data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    public void CopyBuffer(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
#if VALIDATE_USAGE
#endif
        if (sizeInBytes == 0) {
            return;
        }

        this.CopyBufferCore(source, sourceOffset, destination, destinationOffset, sizeInBytes);
    }

    /// <summary>
    /// Copies texture data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    public void CopyTexture(Texture source, Texture destination) {
        uint effectiveSrcArrayLayers = (source.Usage & TextureUsage.Cubemap) != 0
            ? source.ArrayLayers * 6
            : source.ArrayLayers;
#if VALIDATE_USAGE
        uint effectiveDstArrayLayers = (destination.Usage & TextureUsage.Cubemap) != 0
            ? destination.ArrayLayers * 6
            : destination.ArrayLayers;
        if (effectiveSrcArrayLayers != effectiveDstArrayLayers || source.MipLevels != destination.MipLevels
                                                               || source.SampleCount != destination.SampleCount || source.Width != destination.Width
                                                               || source.Height != destination.Height || source.Depth != destination.Depth
                                                               || source.Format != destination.Format) {
            throw new VeldridException("Source and destination Textures are not compatible to be copied.");
        }
#endif

        for (uint level = 0; level < source.MipLevels; level++) {
            Util.GetMipDimensions(source, level, out uint mipWidth, out uint mipHeight, out uint mipDepth);
            this.CopyTexture(source, 0, 0, 0, level, 0, destination, 0, 0, 0, level, 0, mipWidth, mipHeight, mipDepth, effectiveSrcArrayLayers);
        }
    }

    /// <summary>
    /// Copies texture data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    public void CopyTexture(Texture source, Texture destination, uint mipLevel, uint arrayLayer) {
#if VALIDATE_USAGE
        uint effectiveSrcArrayLayers = (source.Usage & TextureUsage.Cubemap) != 0
            ? source.ArrayLayers * 6
            : source.ArrayLayers;
        uint effectiveDstArrayLayers = (destination.Usage & TextureUsage.Cubemap) != 0
            ? destination.ArrayLayers * 6
            : destination.ArrayLayers;
        if (source.SampleCount != destination.SampleCount || source.Width != destination.Width
                                                          || source.Height != destination.Height || source.Depth != destination.Depth
                                                          || source.Format != destination.Format) {
            throw new VeldridException("Source and destination Textures are not compatible to be copied.");
        }

        if (mipLevel >= source.MipLevels || mipLevel >= destination.MipLevels || arrayLayer >= effectiveSrcArrayLayers || arrayLayer >= effectiveDstArrayLayers) {
            throw new VeldridException($"{nameof(mipLevel)} and {nameof(arrayLayer)} must be less than the given Textures' mip level count and array layer count.");
        }
#endif

        Util.GetMipDimensions(source, mipLevel, out uint width, out uint height, out uint depth);
        this.CopyTexture(source, 0, 0, 0, mipLevel, arrayLayer, destination, 0, 0, 0, mipLevel, arrayLayer, width, height, depth, 1);
    }

    /// <summary>
    /// Copies texture data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="srcX">The src x value used by this operation.</param>
    /// <param name="srcY">The src y value used by this operation.</param>
    /// <param name="srcZ">The src z value used by this operation.</param>
    /// <param name="srcMipLevel">The src mip level value used by this operation.</param>
    /// <param name="srcBaseArrayLayer">The src base array layer value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="dstX">The dst x value used by this operation.</param>
    /// <param name="dstY">The dst y value used by this operation.</param>
    /// <param name="dstZ">The dst z value used by this operation.</param>
    /// <param name="dstMipLevel">The dst mip level value used by this operation.</param>
    /// <param name="dstBaseArrayLayer">The dst base array layer value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
    public void CopyTexture(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
#if VALIDATE_USAGE
        if (width == 0 || height == 0 || depth == 0) {
            throw new VeldridException("The given copy region is empty.");
        }

        if (layerCount == 0) {
            throw new VeldridException($"{nameof(layerCount)} must be greater than 0.");
        }

        Util.GetMipDimensions(source, srcMipLevel, out uint srcWidth, out uint srcHeight, out uint srcDepth);
        uint srcBlockSize = FormatHelpers.IsCompressedFormat(source.Format) ? 4u : 1u;
        uint roundedSrcWidth = (srcWidth + srcBlockSize - 1) / srcBlockSize * srcBlockSize;
        uint roundedSrcHeight = (srcHeight + srcBlockSize - 1) / srcBlockSize * srcBlockSize;
        if (srcX + width > roundedSrcWidth || srcY + height > roundedSrcHeight || srcZ + depth > srcDepth) {
            throw new VeldridException("The given copy region is not valid for the source Texture.");
        }

        Util.GetMipDimensions(destination, dstMipLevel, out uint dstWidth, out uint dstHeight, out uint dstDepth);
        uint dstBlockSize = FormatHelpers.IsCompressedFormat(destination.Format) ? 4u : 1u;
        uint roundedDstWidth = (dstWidth + dstBlockSize - 1) / dstBlockSize * dstBlockSize;
        uint roundedDstHeight = (dstHeight + dstBlockSize - 1) / dstBlockSize * dstBlockSize;
        if (dstX + width > roundedDstWidth || dstY + height > roundedDstHeight || dstZ + depth > dstDepth) {
            throw new VeldridException("The given copy region is not valid for the destination Texture.");
        }

        if (srcMipLevel >= source.MipLevels) {
            throw new VeldridException($"{nameof(srcMipLevel)} must be less than the number of mip levels in the source Texture.");
        }

        uint effectiveSrcArrayLayers = (source.Usage & TextureUsage.Cubemap) != 0
            ? source.ArrayLayers * 6
            : source.ArrayLayers;
        if (srcBaseArrayLayer + layerCount > effectiveSrcArrayLayers) {
            throw new VeldridException("An invalid mip range was given for the source Texture.");
        }

        if (dstMipLevel >= destination.MipLevels) {
            throw new VeldridException($"{nameof(dstMipLevel)} must be less than the number of mip levels in the destination Texture.");
        }

        uint effectiveDstArrayLayers = (destination.Usage & TextureUsage.Cubemap) != 0
            ? destination.ArrayLayers * 6
            : destination.ArrayLayers;
        if (dstBaseArrayLayer + layerCount > effectiveDstArrayLayers) {
            throw new VeldridException("An invalid mip range was given for the destination Texture.");
        }
#endif
        this.CopyTextureCore(source, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer, destination, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer, width, height, depth, layerCount);
    }

    /// <summary>
    /// Executes the generate mipmaps logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    public void GenerateMipmaps(Texture texture) {
        if ((texture.Usage & TextureUsage.GenerateMipmaps) == 0) {
            throw new VeldridException($"{nameof(this.GenerateMipmaps)} requires a target Texture with {nameof(TextureUsage)}.{nameof(TextureUsage.GenerateMipmaps)}");
        }

        if (texture.MipLevels > 1) {
            this.GenerateMipmapsCore(texture);
        }
    }

    /// <summary>
    /// Executes the push debug group logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    public void PushDebugGroup(string name) {
        this.PushDebugGroupCore(name);
    }

    /// <summary>
    /// Executes the pop debug group logic for this backend.
    /// </summary>
    public void PopDebugGroup() {
        this.PopDebugGroupCore();
    }

    /// <summary>
    /// Executes the insert debug marker logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    public void InsertDebugMarker(string name) {
        this.InsertDebugMarkerCore(name);
    }

    /// <summary>
    /// Executes the clear cached state logic for this backend.
    /// </summary>
    internal void ClearCachedState() {
        this.Framebuffer = null;
        this.GraphicsPipeline = null;
        this.ComputePipeline = null;
#if VALIDATE_USAGE
        this.indexBuffer = null;
#endif
    }

    // TODO: private protected

    /// <summary>
    /// Sets the graphics resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected abstract void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets);

    // TODO: private protected

    /// <summary>
    /// Sets the compute resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected abstract void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets);

    /// <summary>
    /// Sets the framebuffer core value.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
    protected abstract void SetFramebufferCore(Framebuffer fb);

    // TODO: private protected

    /// <summary>
    /// Executes the draw indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected abstract void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride);

    // TODO: private protected

    /// <summary>
    /// Executes the draw indexed indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected abstract void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride);

    // TODO: private protected

    /// <summary>
    /// Executes the dispatch indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    protected abstract void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset);

    /// <summary>
    /// Executes the resolve texture core logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    protected abstract void ResolveTextureCore(Texture source, Texture destination);

    /// <summary>
    /// Copies buffer core data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    protected abstract void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes);

    /// <summary>
    /// Copies texture core data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="srcX">The src x value used by this operation.</param>
    /// <param name="srcY">The src y value used by this operation.</param>
    /// <param name="srcZ">The src z value used by this operation.</param>
    /// <param name="srcMipLevel">The src mip level value used by this operation.</param>
    /// <param name="srcBaseArrayLayer">The src base array layer value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="dstX">The dst x value used by this operation.</param>
    /// <param name="dstY">The dst y value used by this operation.</param>
    /// <param name="dstZ">The dst z value used by this operation.</param>
    /// <param name="dstMipLevel">The dst mip level value used by this operation.</param>
    /// <param name="dstBaseArrayLayer">The dst base array layer value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
    protected abstract void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount);

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validate indirect offset logic for this backend.
    /// </summary>
    /// <param name="offset">The byte offset used by this operation.</param>
    private static void validateIndirectOffset(uint offset) {
        if (offset % 4 != 0) {
            throw new VeldridException($"{nameof(offset)} must be a multiple of 4.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validate indirect buffer logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    private static void validateIndirectBuffer(DeviceBuffer indirectBuffer) {
        if ((indirectBuffer.Usage & BufferUsage.IndirectBuffer) != BufferUsage.IndirectBuffer) {
            throw new VeldridException($"{nameof(indirectBuffer)} parameter must have been created with BufferUsage.IndirectBuffer. Instead, it was {indirectBuffer.Usage}.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validate indirect stride logic for this backend.
    /// </summary>
    /// <param name="stride">The stride value used by this operation.</param>
    /// <param name="argumentSize">The argument size value used by this operation.</param>
    private static void validateIndirectStride(uint stride, int argumentSize) {
        if (stride < argumentSize || stride % 4 != 0) {
            throw new VeldridException($"{nameof(stride)} parameter must be a multiple of 4, and must be larger than the size of the corresponding argument structure.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validate draw indirect support logic for this backend.
    /// </summary>
    private void validateDrawIndirectSupport() {
        if (!this.features.DrawIndirect) {
            throw new VeldridException("Indirect drawing is not supported by this device.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validate index buffer logic for this backend.
    /// </summary>
    /// <param name="indexCount">The index count value used by this operation.</param>
    private void validateIndexBuffer(uint indexCount) {
#if VALIDATE_USAGE
        if (this.indexBuffer == null) {
            throw new VeldridException($"An index buffer must be bound before {nameof(CommandList)}.{nameof(DrawIndexed)} can be called.");
        }

        uint indexFormatSize = this.indexFormat == IndexFormat.UInt16 ? 2u : 4u;
        uint bytesNeeded = indexCount * indexFormatSize;

        if (this.indexBuffer.SizeInBytes < bytesNeeded) {
            throw new VeldridException($"The active index buffer does not contain enough data to satisfy the given draw command. {bytesNeeded} bytes are needed, but the buffer only contains {this.indexBuffer.SizeInBytes}.");
        }
#endif
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the pre draw validation logic for this backend.
    /// </summary>
    private void preDrawValidation() {
#if VALIDATE_USAGE

        if (this.GraphicsPipeline == null) {
            throw new VeldridException($"A graphics {nameof(Pipeline)} must be set in order to issue draw commands.");
        }

        if (this.Framebuffer == null) {
            throw new VeldridException($"A {nameof(Veldrith.Framebuffer)} must be set in order to issue draw commands.");
        }

        if (!this.GraphicsPipeline.GraphicsOutputDescription.Equals(this.Framebuffer.OutputDescription)) {
            throw new VeldridException($"The {nameof(OutputDescription)} of the current graphics {nameof(Pipeline)} is not compatible with the current {nameof(Veldrith.Framebuffer)}.");
        }
#endif
    }

    /// <summary>
    /// Sets the pipeline core value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    private protected abstract void SetPipelineCore(Pipeline pipeline);

    /// <summary>
    /// Sets the vertex buffer core value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected abstract void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset);

    /// <summary>
    /// Sets the index buffer core value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected abstract void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset);

    /// <summary>
    /// Executes the clear color target core logic for this backend.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="clearColor">The clear color value used by this operation.</param>
    private protected abstract void ClearColorTargetCore(uint index, RgbaFloat clearColor);

    /// <summary>
    /// Executes the clear depth stencil core logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    private protected abstract void ClearDepthStencilCore(float depth, byte stencil);

    /// <summary>
    /// Executes the draw core logic for this backend.
    /// </summary>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected abstract void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart);

    /// <summary>
    /// Executes the draw indexed core logic for this backend.
    /// </summary>
    /// <param name="indexCount">The index count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="indexStart">The index start value used by this operation.</param>
    /// <param name="vertexOffset">The vertex offset value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected abstract void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart);

    /// <summary>
    /// Updates the buffer core state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    private protected abstract void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes);

    /// <summary>
    /// Executes the generate mipmaps core logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    private protected abstract void GenerateMipmapsCore(Texture texture);

    /// <summary>
    /// Executes the push debug group core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected abstract void PushDebugGroupCore(string name);

    /// <summary>
    /// Executes the pop debug group core logic for this backend.
    /// </summary>
    private protected abstract void PopDebugGroupCore();

    /// <summary>
    /// Executes the insert debug marker core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected abstract void InsertDebugMarkerCore(string name);

    /// <summary>
    /// Uploads backend-specific push-constant data to the active pipeline.
    /// </summary>
    /// <param name="offset">The byte offset inside the push-constant range.</param>
    /// <param name="data">A pointer to source data.</param>
    /// <param name="sizeInBytes">The number of bytes to upload.</param>
    private protected abstract void PushConstantsCore(uint offset, IntPtr data, uint sizeInBytes);

#if VALIDATE_USAGE

    /// <summary>
    /// Stores the index buffer value used during command execution.
    /// </summary>
    private DeviceBuffer indexBuffer;

    /// <summary>
    /// Stores the index format value used during command execution.
    /// </summary>
    private IndexFormat indexFormat;
#endif
}
