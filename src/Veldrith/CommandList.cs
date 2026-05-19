using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Veldrith;

/// <summary>
/// Represents the CommandList class.
/// </summary>
public abstract class CommandList : IDeviceResource, IDisposable {

    /// <summary>
    /// Represents the _structuredBufferAlignment field.
    /// </summary>
    private readonly uint _structuredBufferAlignment;

    /// <summary>
    /// Represents the _uniformBufferAlignment field.
    /// </summary>
    private readonly uint _uniformBufferAlignment;

    /// <summary>
    /// Represents the features field.
    /// </summary>
    private readonly GraphicsDeviceFeatures features;

    /// <summary>
    /// Represents the ComputePipeline field.
    /// </summary>
    private protected Pipeline ComputePipeline;

    /// <summary>
    /// Represents the Framebuffer field.
    /// </summary>
    private protected Framebuffer Framebuffer;

    /// <summary>
    /// Represents the GraphicsPipeline field.
    /// </summary>
    private protected Pipeline GraphicsPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandList" /> type.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <param name="features">The value of features.</param>
    /// <param name="uniformAlignment">The value of uniformAlignment.</param>
    /// <param name="structuredAlignment">The value of structuredAlignment.</param>
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
    /// tools.
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public abstract void Dispose();

    #endregion

    /// <summary>
    /// Performs the Begin operation.
    /// </summary>
    public abstract void Begin();

    /// <summary>
    /// Performs the End operation.
    /// </summary>
    public abstract void End();

    /// <summary>
    /// Performs the SetPipeline operation.
    /// </summary>
    /// <param name="pipeline">The value of pipeline.</param>
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
    /// Performs the SetVertexBuffer operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="buffer">The value of buffer.</param>
    public void SetVertexBuffer(uint index, DeviceBuffer buffer) {
        this.SetVertexBuffer(index, buffer, 0);
    }

    /// <summary>
    /// Performs the SetVertexBuffer operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
    public void SetVertexBuffer(uint index, DeviceBuffer buffer, uint offset) {
#if VALIDATE_USAGE
        if ((buffer.Usage & BufferUsage.VertexBuffer) == 0) {
            throw new VeldridException("Buffer cannot be bound as a vertex buffer because it was not created with BufferUsage.VertexBuffer.");
        }
#endif
        this.SetVertexBufferCore(index, buffer, offset);
    }

    /// <summary>
    /// Performs the SetIndexBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="format">The value of format.</param>
    public void SetIndexBuffer(DeviceBuffer buffer, IndexFormat format) {
        this.SetIndexBuffer(buffer, format, 0);
    }

    /// <summary>
    /// Performs the SetIndexBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="offset">The value of offset.</param>
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
    /// Performs the SetGraphicsResourceSet operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    public unsafe void SetGraphicsResourceSet(uint slot, ResourceSet rs) {
        this.SetGraphicsResourceSet(slot, rs, 0, ref Unsafe.AsRef<uint>(null));
    }

    /// <summary>
    /// Performs the SetGraphicsResourceSet operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
    public void SetGraphicsResourceSet(uint slot, ResourceSet rs, uint[] dynamicOffsets) {
        this.SetGraphicsResourceSet(slot, rs, (uint)dynamicOffsets.Length, ref dynamicOffsets[0]);
    }

    /// <summary>
    /// Performs the SetGraphicsResourceSet operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    /// <param name="dynamicOffsetsCount">The value of dynamicOffsetsCount.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
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
    /// Performs the SetComputeResourceSet operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    public unsafe void SetComputeResourceSet(uint slot, ResourceSet rs) {
        this.SetComputeResourceSet(slot, rs, 0, ref Unsafe.AsRef<uint>(null));
    }

    /// <summary>
    /// Performs the SetComputeResourceSet operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
    public void SetComputeResourceSet(uint slot, ResourceSet rs, uint[] dynamicOffsets) {
        this.SetComputeResourceSet(slot, rs, (uint)dynamicOffsets.Length, ref dynamicOffsets[0]);
    }

    /// <summary>
    /// Performs the SetComputeResourceSet operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    /// <param name="dynamicOffsetsCount">The value of dynamicOffsetsCount.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
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
    /// Performs the SetFramebuffer operation.
    /// </summary>
    /// <param name="fb">The value of fb.</param>
    public void SetFramebuffer(Framebuffer fb) {
        if (this.Framebuffer != fb) {
            this.Framebuffer = fb;
            this.SetFramebufferCore(fb);
            this.SetFullViewports();
            this.SetFullScissorRects();
        }
    }

    /// <summary>
    /// Performs the ClearColorTarget operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="clearColor">The value of clearColor.</param>
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
    /// Performs the ClearDepthStencil operation.
    /// </summary>
    /// <param name="depth">The value of depth.</param>
    public void ClearDepthStencil(float depth) {
        this.ClearDepthStencil(depth, 0);
    }

    /// <summary>
    /// Performs the ClearDepthStencil operation.
    /// </summary>
    /// <param name="depth">The value of depth.</param>
    /// <param name="stencil">The value of stencil.</param>
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
    /// Performs the SetFullViewports operation.
    /// </summary>
    public void SetFullViewports() {
        this.SetViewport(0, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));

        for (uint index = 1; index < this.Framebuffer.ColorTargets.Count; index++) {
            this.SetViewport(index, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));
        }
    }

    /// <summary>
    /// Performs the SetFullViewport operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    public void SetFullViewport(uint index) {
        this.SetViewport(index, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));
    }

    /// <summary>
    /// Performs the SetViewport operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="viewport">The value of viewport.</param>
    public void SetViewport(uint index, Viewport viewport) {
        this.SetViewport(index, ref viewport);
    }

    /// <summary>
    /// Performs the SetViewport operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="viewport">The value of viewport.</param>
    public abstract void SetViewport(uint index, ref Viewport viewport);

    /// <summary>
    /// Performs the SetFullScissorRects operation.
    /// </summary>
    public void SetFullScissorRects() {
        this.SetScissorRect(0, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);

        for (uint index = 1; index < this.Framebuffer.ColorTargets.Count; index++) {
            this.SetScissorRect(index, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);
        }
    }

    /// <summary>
    /// Performs the SetFullScissorRect operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    public void SetFullScissorRect(uint index) {
        this.SetScissorRect(index, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);
    }

    /// <summary>
    /// Performs the SetScissorRect operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    public abstract void SetScissorRect(uint index, uint x, uint y, uint width, uint height);

    /// <summary>
    /// Performs the Draw operation.
    /// </summary>
    /// <param name="vertexCount">The value of vertexCount.</param>
    public void Draw(uint vertexCount) {
        this.Draw(vertexCount, 1, 0, 0);
    }

    /// <summary>
    /// Performs the Draw operation.
    /// </summary>
    /// <param name="vertexCount">The value of vertexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="vertexStart">The value of vertexStart.</param>
    /// <param name="instanceStart">The value of instanceStart.</param>
    public void Draw(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        this.preDrawValidation();
        this.DrawCore(vertexCount, instanceCount, vertexStart, instanceStart);
    }

    /// <summary>
    /// Performs the DrawIndexed operation.
    /// </summary>
    /// <param name="indexCount">The value of indexCount.</param>
    public void DrawIndexed(uint indexCount) {
        this.DrawIndexed(indexCount, 1, 0, 0, 0);
    }

    /// <summary>
    /// Performs the DrawIndexed operation.
    /// </summary>
    /// <param name="indexCount">The value of indexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="indexStart">The value of indexStart.</param>
    /// <param name="vertexOffset">The value of vertexOffset.</param>
    /// <param name="instanceStart">The value of instanceStart.</param>
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
    /// Performs the DrawIndirect operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="drawCount">The value of drawCount.</param>
    /// <param name="stride">The value of stride.</param>
    public unsafe void DrawIndirect(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.validateDrawIndirectSupport();
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        validateIndirectStride(stride, sizeof(IndirectDrawArguments));
        this.preDrawValidation();

        this.DrawIndirectCore(indirectBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Performs the DrawIndexedIndirect operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="drawCount">The value of drawCount.</param>
    /// <param name="stride">The value of stride.</param>
    public unsafe void DrawIndexedIndirect(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.validateDrawIndirectSupport();
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        validateIndirectStride(stride, sizeof(IndirectDrawIndexedArguments));
        this.preDrawValidation();

        this.DrawIndexedIndirectCore(indirectBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Performs the Dispatch operation.
    /// </summary>
    /// <param name="groupCountX">The value of groupCountX.</param>
    /// <param name="groupCountY">The value of groupCountY.</param>
    /// <param name="groupCountZ">The value of groupCountZ.</param>
    public abstract void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ);

    /// <summary>
    /// Performs the DispatchIndirect operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    public void DispatchIndirect(DeviceBuffer indirectBuffer, uint offset) {
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        this.DispatchIndirectCore(indirectBuffer, offset);
    }

    /// <summary>
    /// Performs the ResolveTexture operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="destination">The value of destination.</param>
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
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">The resource to update.</param>
    /// <param name="bufferOffsetInBytes">
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" /> storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">The value to upload.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, T source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)(&source), (uint)sizeof(T));
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">The resource to update.</param>
    /// <param name="bufferOffsetInBytes">
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">A reference to the single value to upload.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, ref T source) where T : unmanaged {
        fixed (T* ptr = &source) {
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)ptr, Util.USizeOf<T>());
        }
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">The resource to update.</param>
    /// <param name="bufferOffsetInBytes">
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">A reference to the first of a series of values to upload.</param>
    /// <param name="sizeInBytes">The total size of the uploaded data, in bytes.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, ref T source, uint sizeInBytes) where T : unmanaged {
        fixed (T* ptr = &source) {
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)ptr, sizeInBytes);
        }
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">The resource to update.</param>
    /// <param name="bufferOffsetInBytes">
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">An array containing the data to upload.</param>
    public void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, T[] source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)source);
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">The resource to update.</param>
    /// <param name="bufferOffsetInBytes">
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">An readonly span containing the data to upload.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, ReadOnlySpan<T> source) where T : unmanaged {
        fixed (void* pin = &MemoryMarshal.GetReference(source)) {
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)pin, (uint)(sizeof(T) * source.Length));
        }
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">The resource to update.</param>
    /// <param name="bufferOffsetInBytes">
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">An span containing the data to upload.</param>
    public void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, Span<T> source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)source);
    }

    /// <summary>
    /// Performs the UpdateBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="bufferOffsetInBytes">The value of bufferOffsetInBytes.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
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
    /// Performs the CopyBuffer operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    public void CopyBuffer(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
#if VALIDATE_USAGE
#endif
        if (sizeInBytes == 0) {
            return;
        }

        this.CopyBufferCore(source, sourceOffset, destination, destinationOffset, sizeInBytes);
    }

    /// <summary>
    /// Performs the CopyTexture operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="destination">The value of destination.</param>
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
    /// Performs the CopyTexture operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
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
    /// Performs the CopyTexture operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="srcX">The value of srcX.</param>
    /// <param name="srcY">The value of srcY.</param>
    /// <param name="srcZ">The value of srcZ.</param>
    /// <param name="srcMipLevel">The value of srcMipLevel.</param>
    /// <param name="srcBaseArrayLayer">The value of srcBaseArrayLayer.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="dstX">The value of dstX.</param>
    /// <param name="dstY">The value of dstY.</param>
    /// <param name="dstZ">The value of dstZ.</param>
    /// <param name="dstMipLevel">The value of dstMipLevel.</param>
    /// <param name="dstBaseArrayLayer">The value of dstBaseArrayLayer.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="layerCount">The value of layerCount.</param>
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
    /// Performs the GenerateMipmaps operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    public void GenerateMipmaps(Texture texture) {
        if ((texture.Usage & TextureUsage.GenerateMipmaps) == 0) {
            throw new VeldridException($"{nameof(this.GenerateMipmaps)} requires a target Texture with {nameof(TextureUsage)}.{nameof(TextureUsage.GenerateMipmaps)}");
        }

        if (texture.MipLevels > 1) {
            this.GenerateMipmapsCore(texture);
        }
    }

    /// <summary>
    /// Performs the PushDebugGroup operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    public void PushDebugGroup(string name) {
        this.PushDebugGroupCore(name);
    }

    /// <summary>
    /// Performs the PopDebugGroup operation.
    /// </summary>
    public void PopDebugGroup() {
        this.PopDebugGroupCore();
    }

    /// <summary>
    /// Performs the InsertDebugMarker operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    public void InsertDebugMarker(string name) {
        this.InsertDebugMarkerCore(name);
    }

    /// <summary>
    /// Performs the ClearCachedState operation.
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
    /// Performs the SetGraphicsResourceSetCore operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    /// <param name="dynamicOffsetsCount">The value of dynamicOffsetsCount.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
    protected abstract void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets);

    // TODO: private protected

    /// <summary>
    /// Performs the SetComputeResourceSetCore operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="set">The value of set.</param>
    /// <param name="dynamicOffsetsCount">The value of dynamicOffsetsCount.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
    protected abstract void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets);

    /// <summary>
    /// Performs the SetFramebufferCore operation.
    /// </summary>
    /// <param name="fb">The value of fb.</param>
    protected abstract void SetFramebufferCore(Framebuffer fb);

    // TODO: private protected

    /// <summary>
    /// Performs the DrawIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="drawCount">The value of drawCount.</param>
    /// <param name="stride">The value of stride.</param>
    protected abstract void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride);

    // TODO: private protected

    /// <summary>
    /// Performs the DrawIndexedIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="drawCount">The value of drawCount.</param>
    /// <param name="stride">The value of stride.</param>
    protected abstract void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride);

    // TODO: private protected

    /// <summary>
    /// Performs the DispatchIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    protected abstract void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset);

    /// <summary>
    /// Performs the ResolveTextureCore operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="destination">The value of destination.</param>
    protected abstract void ResolveTextureCore(Texture source, Texture destination);

    /// <summary>
    /// Performs the CopyBufferCore operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    protected abstract void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes);

    /// <summary>
    /// Performs the CopyTextureCore operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="srcX">The value of srcX.</param>
    /// <param name="srcY">The value of srcY.</param>
    /// <param name="srcZ">The value of srcZ.</param>
    /// <param name="srcMipLevel">The value of srcMipLevel.</param>
    /// <param name="srcBaseArrayLayer">The value of srcBaseArrayLayer.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="dstX">The value of dstX.</param>
    /// <param name="dstY">The value of dstY.</param>
    /// <param name="dstZ">The value of dstZ.</param>
    /// <param name="dstMipLevel">The value of dstMipLevel.</param>
    /// <param name="dstBaseArrayLayer">The value of dstBaseArrayLayer.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="layerCount">The value of layerCount.</param>
    protected abstract void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount);

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Performs the validateIndirectOffset operation.
    /// </summary>
    /// <param name="offset">The value of offset.</param>
    private static void validateIndirectOffset(uint offset) {
        if (offset % 4 != 0) {
            throw new VeldridException($"{nameof(offset)} must be a multiple of 4.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Performs the validateIndirectBuffer operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    private static void validateIndirectBuffer(DeviceBuffer indirectBuffer) {
        if ((indirectBuffer.Usage & BufferUsage.IndirectBuffer) != BufferUsage.IndirectBuffer) {
            throw new VeldridException($"{nameof(indirectBuffer)} parameter must have been created with BufferUsage.IndirectBuffer. Instead, it was {indirectBuffer.Usage}.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Performs the validateIndirectStride operation.
    /// </summary>
    /// <param name="stride">The value of stride.</param>
    /// <param name="argumentSize">The value of argumentSize.</param>
    private static void validateIndirectStride(uint stride, int argumentSize) {
        if (stride < argumentSize || stride % 4 != 0) {
            throw new VeldridException($"{nameof(stride)} parameter must be a multiple of 4, and must be larger than the size of the corresponding argument structure.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Performs the validateDrawIndirectSupport operation.
    /// </summary>
    private void validateDrawIndirectSupport() {
        if (!this.features.DrawIndirect) {
            throw new VeldridException("Indirect drawing is not supported by this device.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Performs the validateIndexBuffer operation.
    /// </summary>
    /// <param name="indexCount">The value of indexCount.</param>
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
    /// Performs the preDrawValidation operation.
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
    /// Performs the SetPipelineCore operation.
    /// </summary>
    /// <param name="pipeline">The value of pipeline.</param>
    private protected abstract void SetPipelineCore(Pipeline pipeline);

    /// <summary>
    /// Performs the SetVertexBufferCore operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
    private protected abstract void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset);

    /// <summary>
    /// Performs the SetIndexBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="offset">The value of offset.</param>
    private protected abstract void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset);

    /// <summary>
    /// Performs the ClearColorTargetCore operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="clearColor">The value of clearColor.</param>
    private protected abstract void ClearColorTargetCore(uint index, RgbaFloat clearColor);

    /// <summary>
    /// Performs the ClearDepthStencilCore operation.
    /// </summary>
    /// <param name="depth">The value of depth.</param>
    /// <param name="stencil">The value of stencil.</param>
    private protected abstract void ClearDepthStencilCore(float depth, byte stencil);

    /// <summary>
    /// Performs the DrawCore operation.
    /// </summary>
    /// <param name="vertexCount">The value of vertexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="vertexStart">The value of vertexStart.</param>
    /// <param name="instanceStart">The value of instanceStart.</param>
    private protected abstract void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart);

    /// <summary>
    /// Performs the DrawIndexedCore operation.
    /// </summary>
    /// <param name="indexCount">The value of indexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="indexStart">The value of indexStart.</param>
    /// <param name="vertexOffset">The value of vertexOffset.</param>
    /// <param name="instanceStart">The value of instanceStart.</param>
    private protected abstract void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart);

    /// <summary>
    /// Performs the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="bufferOffsetInBytes">The value of bufferOffsetInBytes.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    private protected abstract void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes);

    /// <summary>
    /// Performs the GenerateMipmapsCore operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    private protected abstract void GenerateMipmapsCore(Texture texture);

    /// <summary>
    /// Performs the PushDebugGroupCore operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    private protected abstract void PushDebugGroupCore(string name);

    /// <summary>
    /// Performs the PopDebugGroupCore operation.
    /// </summary>
    private protected abstract void PopDebugGroupCore();

    /// <summary>
    /// Performs the InsertDebugMarkerCore operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    private protected abstract void InsertDebugMarkerCore(string name);

#if VALIDATE_USAGE

    /// <summary>
    /// Represents the indexBuffer field.
    /// </summary>
    private DeviceBuffer indexBuffer;

    /// <summary>
    /// Represents the indexFormat field.
    /// </summary>
    private IndexFormat indexFormat;
#endif
}