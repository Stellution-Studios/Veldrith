using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the CommandList class.
/// </summary>
public abstract class CommandList : IDeviceResource, IDisposable {

    /// <summary>
    /// Stores the value associated with <c>_structuredBufferAlignment</c>.
    /// </summary>
    private readonly uint _structuredBufferAlignment;

    /// <summary>
    /// Stores the value associated with <c>_uniformBufferAlignment</c>.
    /// </summary>
    private readonly uint _uniformBufferAlignment;

    /// <summary>
    /// Stores the value associated with <c>features</c>.
    /// </summary>
    private readonly GraphicsDeviceFeatures features;

    /// <summary>
    /// Stores the value associated with <c>ComputePipeline</c>.
    /// </summary>
    private protected Pipeline ComputePipeline;

    /// <summary>
    /// Stores the value associated with <c>Framebuffer</c>.
    /// </summary>
    private protected Framebuffer Framebuffer;

    /// <summary>
    /// Stores the value associated with <c>GraphicsPipeline</c>.
    /// </summary>
    private protected Pipeline GraphicsPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandList" /> type.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <param name="features">Specifies the value of <paramref name="features" />.</param>
    /// <param name="uniformAlignment">Specifies the value of <paramref name="uniformAlignment" />.</param>
    /// <param name="structuredAlignment">Specifies the value of <paramref name="structuredAlignment" />.</param>
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
    /// Executes the Dispose operation.
    /// </summary>
    public abstract void Dispose();

    #endregion

    /// <summary>
    /// Executes the Begin operation.
    /// </summary>
    public abstract void Begin();

    /// <summary>
    /// Executes the End operation.
    /// </summary>
    public abstract void End();

    /// <summary>
    /// Executes the SetPipeline operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
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
    /// Executes the SetVertexBuffer operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    public void SetVertexBuffer(uint index, DeviceBuffer buffer) {
        this.SetVertexBuffer(index, buffer, 0);
    }

    /// <summary>
    /// Executes the SetVertexBuffer operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    public void SetVertexBuffer(uint index, DeviceBuffer buffer, uint offset) {
#if VALIDATE_USAGE
        if ((buffer.Usage & BufferUsage.VertexBuffer) == 0) {
            throw new VeldridException("Buffer cannot be bound as a vertex buffer because it was not created with BufferUsage.VertexBuffer.");
        }
#endif
        this.SetVertexBufferCore(index, buffer, offset);
    }

    /// <summary>
    /// Executes the SetIndexBuffer operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    public void SetIndexBuffer(DeviceBuffer buffer, IndexFormat format) {
        this.SetIndexBuffer(buffer, format, 0);
    }

    /// <summary>
    /// Executes the SetIndexBuffer operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
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
    /// Executes the SetGraphicsResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    public unsafe void SetGraphicsResourceSet(uint slot, ResourceSet rs) {
        this.SetGraphicsResourceSet(slot, rs, 0, ref Unsafe.AsRef<uint>(null));
    }

    /// <summary>
    /// Executes the SetGraphicsResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    public void SetGraphicsResourceSet(uint slot, ResourceSet rs, uint[] dynamicOffsets) {
        this.SetGraphicsResourceSet(slot, rs, (uint)dynamicOffsets.Length, ref dynamicOffsets[0]);
    }

    /// <summary>
    /// Executes the SetGraphicsResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
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
    /// Executes the SetComputeResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    public unsafe void SetComputeResourceSet(uint slot, ResourceSet rs) {
        this.SetComputeResourceSet(slot, rs, 0, ref Unsafe.AsRef<uint>(null));
    }

    /// <summary>
    /// Executes the SetComputeResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    public void SetComputeResourceSet(uint slot, ResourceSet rs, uint[] dynamicOffsets) {
        this.SetComputeResourceSet(slot, rs, (uint)dynamicOffsets.Length, ref dynamicOffsets[0]);
    }

    /// <summary>
    /// Executes the SetComputeResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
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
    /// Executes the SetFramebuffer operation.
    /// </summary>
    /// <param name="fb">Specifies the value of <paramref name="fb" />.</param>
    public void SetFramebuffer(Framebuffer fb) {
        if (this.Framebuffer != fb) {
            this.Framebuffer = fb;
            this.SetFramebufferCore(fb);
            this.SetFullViewports();
            this.SetFullScissorRects();
        }
    }

    /// <summary>
    /// Executes the ClearColorTarget operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="clearColor">Specifies the value of <paramref name="clearColor" />.</param>
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
    /// Executes the ClearDepthStencil operation.
    /// </summary>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    public void ClearDepthStencil(float depth) {
        this.ClearDepthStencil(depth, 0);
    }

    /// <summary>
    /// Executes the ClearDepthStencil operation.
    /// </summary>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="stencil">Specifies the value of <paramref name="stencil" />.</param>
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
    /// Executes the SetFullViewports operation.
    /// </summary>
    public void SetFullViewports() {
        this.SetViewport(0, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));

        for (uint index = 1; index < this.Framebuffer.ColorTargets.Count; index++) {
            this.SetViewport(index, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));
        }
    }

    /// <summary>
    /// Executes the SetFullViewport operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void SetFullViewport(uint index) {
        this.SetViewport(index, new Viewport(0, 0, this.Framebuffer.Width, this.Framebuffer.Height, 0, 1));
    }

    /// <summary>
    /// Executes the SetViewport operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="viewport">Specifies the value of <paramref name="viewport" />.</param>
    public void SetViewport(uint index, Viewport viewport) {
        this.SetViewport(index, ref viewport);
    }

    /// <summary>
    /// Executes the SetViewport operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="viewport">Specifies the value of <paramref name="viewport" />.</param>
    public abstract void SetViewport(uint index, ref Viewport viewport);

    /// <summary>
    /// Executes the SetFullScissorRects operation.
    /// </summary>
    public void SetFullScissorRects() {
        this.SetScissorRect(0, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);

        for (uint index = 1; index < this.Framebuffer.ColorTargets.Count; index++) {
            this.SetScissorRect(index, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);
        }
    }

    /// <summary>
    /// Executes the SetFullScissorRect operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void SetFullScissorRect(uint index) {
        this.SetScissorRect(index, 0, 0, this.Framebuffer.Width, this.Framebuffer.Height);
    }

    /// <summary>
    /// Executes the SetScissorRect operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public abstract void SetScissorRect(uint index, uint x, uint y, uint width, uint height);

    /// <summary>
    /// Executes the Draw operation.
    /// </summary>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    public void Draw(uint vertexCount) {
        this.Draw(vertexCount, 1, 0, 0);
    }

    /// <summary>
    /// Executes the Draw operation.
    /// </summary>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="vertexStart">Specifies the value of <paramref name="vertexStart" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    public void Draw(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        this.preDrawValidation();
        this.DrawCore(vertexCount, instanceCount, vertexStart, instanceStart);
    }

    /// <summary>
    /// Executes the DrawIndexed operation.
    /// </summary>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
    public void DrawIndexed(uint indexCount) {
        this.DrawIndexed(indexCount, 1, 0, 0, 0);
    }

    /// <summary>
    /// Executes the DrawIndexed operation.
    /// </summary>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="indexStart">Specifies the value of <paramref name="indexStart" />.</param>
    /// <param name="vertexOffset">Specifies the value of <paramref name="vertexOffset" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
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
    /// Executes the DrawIndirect operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    public unsafe void DrawIndirect(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.validateDrawIndirectSupport();
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        validateIndirectStride(stride, sizeof(IndirectDrawArguments));
        this.preDrawValidation();

        this.DrawIndirectCore(indirectBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the DrawIndexedIndirect operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    public unsafe void DrawIndexedIndirect(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.validateDrawIndirectSupport();
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        validateIndirectStride(stride, sizeof(IndirectDrawIndexedArguments));
        this.preDrawValidation();

        this.DrawIndexedIndirectCore(indirectBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the Dispatch operation.
    /// </summary>
    /// <param name="groupCountX">Specifies the value of <paramref name="groupCountX" />.</param>
    /// <param name="groupCountY">Specifies the value of <paramref name="groupCountY" />.</param>
    /// <param name="groupCountZ">Specifies the value of <paramref name="groupCountZ" />.</param>
    public abstract void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ);

    /// <summary>
    /// Executes the DispatchIndirect operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    public void DispatchIndirect(DeviceBuffer indirectBuffer, uint offset) {
        validateIndirectBuffer(indirectBuffer);
        validateIndirectOffset(offset);
        this.DispatchIndirectCore(indirectBuffer, offset);
    }

    /// <summary>
    /// Executes the ResolveTexture operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
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
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" /> storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    public unsafe void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, T source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)(&source), (uint)sizeof(T));
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
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
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
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
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    public void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, T[] source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)source);
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer" /> region with new data.
    /// This function must be used with a blittable value type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of data to upload.</typeparam>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
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
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// An offset, in bytes, from the beginning of the <see cref="DeviceBuffer" />'s storage, at
    /// which new data will be uploaded.
    /// </param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    public void UpdateBuffer<T>(DeviceBuffer buffer, uint bufferOffsetInBytes, Span<T> source) where T : unmanaged {
        this.UpdateBuffer(buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)source);
    }

    /// <summary>
    /// Executes the UpdateBuffer operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
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
    /// Executes the CopyBuffer operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sourceOffset">Specifies the value of <paramref name="sourceOffset" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="destinationOffset">Specifies the value of <paramref name="destinationOffset" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    public void CopyBuffer(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
#if VALIDATE_USAGE
#endif
        if (sizeInBytes == 0) {
            return;
        }

        this.CopyBufferCore(source, sourceOffset, destination, destinationOffset, sizeInBytes);
    }

    /// <summary>
    /// Executes the CopyTexture operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
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
    /// Executes the CopyTexture operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
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
    /// Executes the CopyTexture operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="srcX">Specifies the value of <paramref name="srcX" />.</param>
    /// <param name="srcY">Specifies the value of <paramref name="srcY" />.</param>
    /// <param name="srcZ">Specifies the value of <paramref name="srcZ" />.</param>
    /// <param name="srcMipLevel">Specifies the value of <paramref name="srcMipLevel" />.</param>
    /// <param name="srcBaseArrayLayer">Specifies the value of <paramref name="srcBaseArrayLayer" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="dstX">Specifies the value of <paramref name="dstX" />.</param>
    /// <param name="dstY">Specifies the value of <paramref name="dstY" />.</param>
    /// <param name="dstZ">Specifies the value of <paramref name="dstZ" />.</param>
    /// <param name="dstMipLevel">Specifies the value of <paramref name="dstMipLevel" />.</param>
    /// <param name="dstBaseArrayLayer">Specifies the value of <paramref name="dstBaseArrayLayer" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="layerCount">Specifies the value of <paramref name="layerCount" />.</param>
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
    /// Executes the GenerateMipmaps operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    public void GenerateMipmaps(Texture texture) {
        if ((texture.Usage & TextureUsage.GenerateMipmaps) == 0) {
            throw new VeldridException($"{nameof(this.GenerateMipmaps)} requires a target Texture with {nameof(TextureUsage)}.{nameof(TextureUsage.GenerateMipmaps)}");
        }

        if (texture.MipLevels > 1) {
            this.GenerateMipmapsCore(texture);
        }
    }

    /// <summary>
    /// Executes the PushDebugGroup operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    public void PushDebugGroup(string name) {
        this.PushDebugGroupCore(name);
    }

    /// <summary>
    /// Executes the PopDebugGroup operation.
    /// </summary>
    public void PopDebugGroup() {
        this.PopDebugGroupCore();
    }

    /// <summary>
    /// Executes the InsertDebugMarker operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    public void InsertDebugMarker(string name) {
        this.InsertDebugMarkerCore(name);
    }

    /// <summary>
    /// Executes the ClearCachedState operation.
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
    /// Executes the SetGraphicsResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected abstract void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets);

    // TODO: private protected

    /// <summary>
    /// Executes the SetComputeResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected abstract void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets);

    /// <summary>
    /// Executes the SetFramebufferCore operation.
    /// </summary>
    /// <param name="fb">Specifies the value of <paramref name="fb" />.</param>
    protected abstract void SetFramebufferCore(Framebuffer fb);

    // TODO: private protected

    /// <summary>
    /// Executes the DrawIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected abstract void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride);

    // TODO: private protected

    /// <summary>
    /// Executes the DrawIndexedIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected abstract void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride);

    // TODO: private protected

    /// <summary>
    /// Executes the DispatchIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    protected abstract void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset);

    /// <summary>
    /// Executes the ResolveTextureCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    protected abstract void ResolveTextureCore(Texture source, Texture destination);

    /// <summary>
    /// Executes the CopyBufferCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sourceOffset">Specifies the value of <paramref name="sourceOffset" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="destinationOffset">Specifies the value of <paramref name="destinationOffset" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    protected abstract void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes);

    /// <summary>
    /// Executes the CopyTextureCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="srcX">Specifies the value of <paramref name="srcX" />.</param>
    /// <param name="srcY">Specifies the value of <paramref name="srcY" />.</param>
    /// <param name="srcZ">Specifies the value of <paramref name="srcZ" />.</param>
    /// <param name="srcMipLevel">Specifies the value of <paramref name="srcMipLevel" />.</param>
    /// <param name="srcBaseArrayLayer">Specifies the value of <paramref name="srcBaseArrayLayer" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="dstX">Specifies the value of <paramref name="dstX" />.</param>
    /// <param name="dstY">Specifies the value of <paramref name="dstY" />.</param>
    /// <param name="dstZ">Specifies the value of <paramref name="dstZ" />.</param>
    /// <param name="dstMipLevel">Specifies the value of <paramref name="dstMipLevel" />.</param>
    /// <param name="dstBaseArrayLayer">Specifies the value of <paramref name="dstBaseArrayLayer" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="layerCount">Specifies the value of <paramref name="layerCount" />.</param>
    protected abstract void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount);

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validateIndirectOffset operation.
    /// </summary>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private static void validateIndirectOffset(uint offset) {
        if (offset % 4 != 0) {
            throw new VeldridException($"{nameof(offset)} must be a multiple of 4.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validateIndirectBuffer operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    private static void validateIndirectBuffer(DeviceBuffer indirectBuffer) {
        if ((indirectBuffer.Usage & BufferUsage.IndirectBuffer) != BufferUsage.IndirectBuffer) {
            throw new VeldridException($"{nameof(indirectBuffer)} parameter must have been created with BufferUsage.IndirectBuffer. Instead, it was {indirectBuffer.Usage}.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validateIndirectStride operation.
    /// </summary>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    /// <param name="argumentSize">Specifies the value of <paramref name="argumentSize" />.</param>
    private static void validateIndirectStride(uint stride, int argumentSize) {
        if (stride < argumentSize || stride % 4 != 0) {
            throw new VeldridException($"{nameof(stride)} parameter must be a multiple of 4, and must be larger than the size of the corresponding argument structure.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validateDrawIndirectSupport operation.
    /// </summary>
    private void validateDrawIndirectSupport() {
        if (!this.features.DrawIndirect) {
            throw new VeldridException("Indirect drawing is not supported by this device.");
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validateIndexBuffer operation.
    /// </summary>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
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
    /// Executes the preDrawValidation operation.
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
    /// Executes the SetPipelineCore operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
    private protected abstract void SetPipelineCore(Pipeline pipeline);

    /// <summary>
    /// Executes the SetVertexBufferCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private protected abstract void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset);

    /// <summary>
    /// Executes the SetIndexBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private protected abstract void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset);

    /// <summary>
    /// Executes the ClearColorTargetCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="clearColor">Specifies the value of <paramref name="clearColor" />.</param>
    private protected abstract void ClearColorTargetCore(uint index, RgbaFloat clearColor);

    /// <summary>
    /// Executes the ClearDepthStencilCore operation.
    /// </summary>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="stencil">Specifies the value of <paramref name="stencil" />.</param>
    private protected abstract void ClearDepthStencilCore(float depth, byte stencil);

    /// <summary>
    /// Executes the DrawCore operation.
    /// </summary>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="vertexStart">Specifies the value of <paramref name="vertexStart" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    private protected abstract void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart);

    /// <summary>
    /// Executes the DrawIndexedCore operation.
    /// </summary>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="indexStart">Specifies the value of <paramref name="indexStart" />.</param>
    /// <param name="vertexOffset">Specifies the value of <paramref name="vertexOffset" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    private protected abstract void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart);

    /// <summary>
    /// Executes the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    private protected abstract void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes);

    /// <summary>
    /// Executes the GenerateMipmapsCore operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    private protected abstract void GenerateMipmapsCore(Texture texture);

    /// <summary>
    /// Executes the PushDebugGroupCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    private protected abstract void PushDebugGroupCore(string name);

    /// <summary>
    /// Executes the PopDebugGroupCore operation.
    /// </summary>
    private protected abstract void PopDebugGroupCore();

    /// <summary>
    /// Executes the InsertDebugMarkerCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    private protected abstract void InsertDebugMarkerCore(string name);

#if VALIDATE_USAGE

    /// <summary>
    /// Stores the value associated with <c>indexBuffer</c>.
    /// </summary>
    private DeviceBuffer indexBuffer;

    /// <summary>
    /// Stores the value associated with <c>indexFormat</c>.
    /// </summary>
    private IndexFormat indexFormat;
#endif
}