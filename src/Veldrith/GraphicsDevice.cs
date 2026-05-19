using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Veldrith.D3D12;
using Veldrith.MTL;
using Veldrith.Vk;

namespace Veldrith;

/// <summary>
/// Represents the GraphicsDevice type used by the graphics runtime.
/// </summary>
public abstract class GraphicsDevice : IDisposable {

    /// <summary>
    /// Synchronizes access to the deferred disposal lock state.
    /// </summary>
    private readonly object _deferredDisposalLock = new();

    /// <summary>
    /// Stores the disposables state used by this instance.
    /// </summary>
    private readonly List<IDisposable> _disposables = new();

    /// <summary>
    /// Stores the aniso4 xsampler state used by this instance.
    /// </summary>
    private Sampler _aniso4XSampler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDevice" /> type.
    /// </summary>
    internal GraphicsDevice() { }

    /// <summary>
    /// Gets the name of the device.
    /// </summary>
    public abstract string DeviceName { get; }

    /// <summary>
    /// Gets the name of the device vendor.
    /// </summary>
    public abstract string VendorName { get; }

    /// <summary>
    /// Gets the API version of the graphics backend.
    /// </summary>
    public abstract GraphicsApiVersion ApiVersion { get; }

    /// <summary>
    /// Gets a value identifying the specific graphics API used by this instance.
    /// </summary>
    public abstract GraphicsBackend BackendType { get; }

    /// <summary>
    /// Gets a value identifying whether texture coordinates begin in the top left corner of a Texture.
    /// </summary>
    public abstract bool IsUvOriginTopLeft { get; }

    /// <summary>
    /// Gets a value indicating whether this device's depth values range from 0 to 1.
    /// </summary>
    public abstract bool IsDepthRangeZeroToOne { get; }

    /// <summary>
    /// Gets a value indicating whether this device's clip space Y values increase from top (-1) to bottom (1).
    /// </summary>
    public abstract bool IsClipSpaceYInverted { get; }

    /// <summary>
    /// Gets the <see cref="ResourceFactory" /> controlled by this instance.
    /// </summary>
    public abstract ResourceFactory ResourceFactory { get; }

    /// <summary>
    /// Retrieves the main Swapchain for this device. This property is only valid if the device was created with a main
    /// </summary>
    public abstract Swapchain MainSwapchain { get; }

    /// <summary>
    /// Gets a <see cref="GraphicsDeviceFeatures" /> which enumerates the optional features supported by this instance.
    /// </summary>
    public abstract GraphicsDeviceFeatures Features { get; }

    /// <summary>
    /// Gets the uniform buffer min offset alignment core value.
    /// </summary>
    public uint UniformBufferMinOffsetAlignment => this.GetUniformBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Gets the structured buffer min offset alignment core value.
    /// </summary>
    public uint StructuredBufferMinOffsetAlignment => this.GetStructuredBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Gets a <see cref="Framebuffer" /> object representing the render targets of the main swapchain.
    /// </summary>
    public Framebuffer SwapchainFramebuffer => this.MainSwapchain?.Framebuffer;

    /// <summary>
    /// Gets a simple 4x anisotropic-filtered <see cref="Sampler" /> object owned by this instance.
    /// </summary>
    public Sampler Aniso4XSampler {
        get {
            if (!this.Features.SamplerAnisotropy) {
                throw new VeldridException("GraphicsDevice.Aniso4xSampler cannot be used unless GraphicsDeviceFeatures.SamplerAnisotropy is supported.");
            }

            Debug.Assert(this._aniso4XSampler != null);
            return this._aniso4XSampler;
        }
    }

    /// <summary>
    /// Gets or sets whether the main Swapchain's <see cref="SwapBuffers()" /> should be synchronized to the window
    /// </summary>
    public virtual bool SyncToVerticalBlank {
        get => this.MainSwapchain?.SyncToVerticalBlank ?? false;
        set {
            if (this.MainSwapchain == null) {
                throw new VeldridException("This GraphicsDevice was created without a main Swapchain. This property cannot be set.");
            }

            this.MainSwapchain.SyncToVerticalBlank = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the graphics device should allow frames to be displayed as fast as possible even if tearing
    /// </summary>
    public virtual bool AllowTearing { get; set; }

    /// <summary>
    /// Gets a simple point-filtered <see cref="Sampler" /> object owned by this instance.
    /// </summary>
    public Sampler PointSampler { get; private set; }

    /// <summary>
    /// Gets a simple linear-filtered <see cref="Sampler" /> object owned by this instance.
    /// </summary>
    public Sampler LinearSampler { get; private set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public void Dispose() {
        this.WaitForIdle();
        this.PointSampler.Dispose();
        this.LinearSampler.Dispose();
        this._aniso4XSampler?.Dispose();
        this.PlatformDispose();
    }

    #endregion

    /// <summary>
    /// Executes the is backend supported logic for this backend.
    /// </summary>
    /// <param name="backend">The backend value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public static bool IsBackendSupported(GraphicsBackend backend) {
        switch (backend) {
            case GraphicsBackend.Direct3D12: return D3D12GraphicsDevice.IsSupported();
            case GraphicsBackend.Vulkan:
#if !EXCLUDE_VULKAN_BACKEND
                return VkGraphicsDevice.IsSupported();
#else
                    return false;
#endif
            case GraphicsBackend.Metal:
#if !EXCLUDE_METAL_BACKEND
                return MtlGraphicsDevice.IsSupported();
#else
                    return false;
#endif
            default: throw Illegal.Value<GraphicsBackend>();
        }
    }

    /// <summary>
    /// Executes the submit commands logic for this backend.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    public void SubmitCommands(CommandList commandList) {
        this.SubmitCommandsCore(commandList, null);
    }

    /// <summary>
    /// Executes the submit commands logic for this backend.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    public void SubmitCommands(CommandList commandList, Fence fence) {
        this.SubmitCommandsCore(commandList, fence);
    }

    /// <summary>
    /// Executes the wait for fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    public void WaitForFence(Fence fence) {
        if (!this.WaitForFence(fence, ulong.MaxValue)) {
            throw new VeldridException("The operation timed out before the Fence was signaled.");
        }
    }

    /// <summary>
    /// Executes the wait for fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    /// <param name="timeout">The timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool WaitForFence(Fence fence, TimeSpan timeout) {
        return this.WaitForFence(fence, (ulong)timeout.TotalMilliseconds * 1_000_000);
    }

    /// <summary>
    /// Executes the wait for fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public abstract bool WaitForFence(Fence fence, ulong nanosecondTimeout);

    /// <summary>
    /// Executes the wait for fences logic for this backend.
    /// </summary>
    /// <param name="fences">The synchronization fence used by this operation.</param>
    /// <param name="waitAll">The wait all value used by this operation.</param>
    public void WaitForFences(Fence[] fences, bool waitAll) {
        if (!this.WaitForFences(fences, waitAll, ulong.MaxValue)) {
            throw new VeldridException("The operation timed out before the Fence(s) were signaled.");
        }
    }

    /// <summary>
    /// Executes the wait for fences logic for this backend.
    /// </summary>
    /// <param name="fences">The synchronization fence used by this operation.</param>
    /// <param name="waitAll">The wait all value used by this operation.</param>
    /// <param name="timeout">The timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool WaitForFences(Fence[] fences, bool waitAll, TimeSpan timeout) {
        return this.WaitForFences(fences, waitAll, (ulong)timeout.TotalMilliseconds * 1_000_000);
    }

    /// <summary>
    /// Executes the wait for fences logic for this backend.
    /// </summary>
    /// <param name="fences">The synchronization fence used by this operation.</param>
    /// <param name="waitAll">The wait all value used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public abstract bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout);

    /// <summary>
    /// Executes the reset fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    public abstract void ResetFence(Fence fence);

    /// <summary>
    /// Executes the swap buffers logic for this backend.
    /// </summary>
    public void SwapBuffers() {
        if (this.MainSwapchain == null) {
            throw new VeldridException("This GraphicsDevice was created without a main Swapchain, so the requested operation cannot be performed.");
        }

        this.SwapBuffers(this.MainSwapchain);
    }

    /// <summary>
    /// Executes the swap buffers logic for this backend.
    /// </summary>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    public void SwapBuffers(Swapchain swapchain) {
        this.SwapBuffersCore(swapchain);
    }

    /// <summary>
    /// Executes the resize main window logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public void ResizeMainWindow(uint width, uint height) {
        if (this.MainSwapchain == null) {
            throw new VeldridException("This GraphicsDevice was created without a main Swapchain, so the requested operation cannot be performed.");
        }

        this.MainSwapchain.Resize(width, height);
    }

    /// <summary>
    /// Executes the wait for idle logic for this backend.
    /// </summary>
    public void WaitForIdle() {
        this.WaitForIdleCore();
        this.FlushDeferredDisposals();
    }

    /// <summary>
    /// Executes the wait for next frame ready logic for this backend.
    /// </summary>
    public void WaitForNextFrameReady() {
        this.WaitForNextFrameReadyCore();
    }

    /// <summary>
    /// Gets the sample count limit value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat);

    /// <summary>
    /// Maps the value resource for CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MappedResource Map(IMappableResource resource, MapMode mode) {
        return this.Map(resource, mode, 0);
    }

    /// <summary>
    /// Maps the value resource for CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MappedResource Map(IMappableResource resource, MapMode mode, uint subresource) {
#if VALIDATE_USAGE
        if (resource is DeviceBuffer buffer) {
            if ((buffer.Usage & BufferUsage.Dynamic) != BufferUsage.Dynamic
                && (buffer.Usage & BufferUsage.Staging) != BufferUsage.Staging) {
                throw new VeldridException("Buffers must have the Staging or Dynamic usage flag to be mapped.");
            }

            if (subresource != 0) {
                throw new VeldridException("Subresource must be 0 for Buffer resources.");
            }

            if ((mode == MapMode.Read || mode == MapMode.ReadWrite) && (buffer.Usage & BufferUsage.Staging) == 0) {
                throw new VeldridException($"{nameof(MapMode)}.{nameof(MapMode.Read)} and {nameof(MapMode)}.{nameof(MapMode.ReadWrite)} can only be used on buffers created with {nameof(BufferUsage)}.{nameof(BufferUsage.Staging)}.");
            }
        }
        else if (resource is Texture tex) {
            if ((tex.Usage & TextureUsage.Staging) == 0) {
                throw new VeldridException("Texture must have the Staging usage flag to be mapped.");
            }

            if (subresource >= tex.ArrayLayers * tex.MipLevels) {
                throw new VeldridException("Subresource must be less than the number of subresources in the Texture being mapped.");
            }
        }
#endif

        return this.MapCore(resource, mode, subresource);
    }

    /// <summary>
    /// Maps a <see cref="DeviceBuffer" /> or <see cref="Texture" /> into a CPU-accessible data region, and returns a
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    public MappedResourceView<T> Map<T>(IMappableResource resource, MapMode mode) where T : unmanaged {
        return this.Map<T>(resource, mode, 0);
    }

    /// <summary>
    /// Maps a <see cref="DeviceBuffer" /> or <see cref="Texture" /> into a CPU-accessible data region, and returns a
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    public MappedResourceView<T> Map<T>(IMappableResource resource, MapMode mode, uint subresource)
        where T : unmanaged {
        MappedResource mappedResource = this.Map(resource, mode, subresource);
        return new MappedResourceView<T>(mappedResource);
    }

    /// <summary>
    /// Unmaps the value resource from CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    public void Unmap(IMappableResource resource) {
        this.Unmap(resource, 0);
    }

    /// <summary>
    /// Unmaps the value resource from CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    public void Unmap(IMappableResource resource, uint subresource) {
        this.UnmapCore(resource, subresource);
    }


    /// <summary>
    /// Updates the texture state for this command sequence.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    public void UpdateTexture(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
#if VALIDATE_USAGE
        ValidateUpdateTextureParameters(texture, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
#endif
        this.UpdateTextureCore(texture, source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Updates a portion of a <see cref="Texture" /> resource with new data contained in an array
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    public void UpdateTexture<T>(Texture texture, T[] source, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) where T : unmanaged {
        this.UpdateTexture(texture, (ReadOnlySpan<T>)source, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Updates a portion of a <see cref="Texture" /> resource with new data contained in an array
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    public unsafe void UpdateTexture<T>(Texture texture, ReadOnlySpan<T> source, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) where T : unmanaged {
        uint sizeInBytes = (uint)(sizeof(T) * source.Length);
#if VALIDATE_USAGE
        ValidateUpdateTextureParameters(texture, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
#endif

        fixed (void* pin = &MemoryMarshal.GetReference(source)) {
            this.UpdateTextureCore(texture, (IntPtr)pin, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
        }
    }

    /// <summary>
    /// Updates a portion of a <see cref="Texture" /> resource with new data contained in an array
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    public void UpdateTexture<T>(Texture texture, Span<T> source, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) where T : unmanaged {
        this.UpdateTexture(texture, (ReadOnlySpan<T>)source, x, y, z, width, height, depth, mipLevel, arrayLayer);
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
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)ptr, (uint)sizeof(T));
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
            throw new VeldridException($"The data size given to UpdateBuffer is too large. The given buffer can only hold {buffer.SizeInBytes} total bytes. The requested update would require {bufferOffsetInBytes + sizeInBytes} bytes.");
        }

        if (sizeInBytes == 0) {
            return;
        }

        this.UpdateBufferCore(buffer, bufferOffsetInBytes, source, sizeInBytes);
    }

    /// <summary>
    /// Gets the pixel format support value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool GetPixelFormatSupport(PixelFormat format, TextureType type, TextureUsage usage) {
        return this.GetPixelFormatSupportCore(format, type, usage, out _);
    }

    /// <summary>
    /// Gets the pixel format support value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="properties">The properties value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool GetPixelFormatSupport(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties) {
        return this.GetPixelFormatSupportCore(format, type, usage, out properties);
    }

    /// <summary>
    /// Executes the dispose when idle logic for this backend.
    /// </summary>
    /// <param name="disposable">The disposable value used by this operation.</param>
    public void DisposeWhenIdle(IDisposable disposable) {
        lock (this._deferredDisposalLock) {
            this._disposables.Add(disposable);
        }
    }

    /// <summary>
    /// Gets the uniform buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal abstract uint GetUniformBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Gets the structured buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal abstract uint GetStructuredBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Maps the core resource for CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource);

    /// <summary>
    /// Unmaps the core resource from CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    protected abstract void UnmapCore(IMappableResource resource, uint subresource);

    /// <summary>
    /// Executes the platform dispose logic for this backend.
    /// </summary>
    protected abstract void PlatformDispose();

    /// <summary>
    /// Executes the post device created logic for this backend.
    /// </summary>
    protected void PostDeviceCreated() {
        this.PointSampler = this.ResourceFactory.CreateSampler(SamplerDescription.POINT);
        this.LinearSampler = this.ResourceFactory.CreateSampler(SamplerDescription.LINEAR);
        if (this.Features.SamplerAnisotropy) {
            this._aniso4XSampler = this.ResourceFactory.CreateSampler(SamplerDescription.ANISO4_X);
        }
    }

    [Conditional("VALIDATE_USAGE")]

    /// <summary>
    /// Executes the validate update texture parameters logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    private static void ValidateUpdateTextureParameters(Texture texture, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        if (FormatHelpers.IsCompressedFormat(texture.Format)) {
            if (x % 4 != 0 || y % 4 != 0 || height % 4 != 0 || width % 4 != 0) {
                Util.GetMipDimensions(texture, mipLevel, out uint mipWidth, out uint mipHeight, out _);
                if (width != mipWidth && height != mipHeight) {
                    throw new VeldridException("Updates to block-compressed textures must use a region that is block-size aligned and sized.");
                }
            }
        }

        uint expectedSize = FormatHelpers.GetRegionSize(width, height, depth, texture.Format);

        if (sizeInBytes < expectedSize) {
            throw new VeldridException($"The data size is less than expected for the given update region. At least {expectedSize} bytes must be provided, but only {sizeInBytes} were.");
        }

        // Compressed textures don't necessarily need to have a Texture.Width and Texture.Height that are a multiple of 4.
        // But the mipdata width and height *does* need to be a multiple of 4.
        uint roundedTextureWidth, roundedTextureHeight;

        if (FormatHelpers.IsCompressedFormat(texture.Format)) {
            roundedTextureWidth = (texture.Width + 3) / 4 * 4;
            roundedTextureHeight = (texture.Height + 3) / 4 * 4;
        }
        else {
            roundedTextureWidth = texture.Width;
            roundedTextureHeight = texture.Height;
        }

        if (x + width > roundedTextureWidth || y + height > roundedTextureHeight || z + depth > texture.Depth) {
            throw new VeldridException("The given region does not fit into the Texture.");
        }

        if (mipLevel >= texture.MipLevels) {
            throw new VeldridException($"{nameof(mipLevel)} ({mipLevel}) must be less than the Texture's mip level count ({texture.MipLevels}).");
        }

        uint effectiveArrayLayers = texture.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) {
            effectiveArrayLayers *= 6;
        }

        if (arrayLayer >= effectiveArrayLayers) {
            throw new VeldridException($"{nameof(arrayLayer)} ({arrayLayer}) must be less than the Texture's effective array layer count ({effectiveArrayLayers}).");
        }
    }

    /// <summary>
    /// Executes the flush deferred disposals logic for this backend.
    /// </summary>
    private void FlushDeferredDisposals() {
        lock (this._deferredDisposalLock) {
            foreach (IDisposable disposable in this._disposables) {
                disposable.Dispose();
            }

            this._disposables.Clear();
        }
    }

    /// <summary>
    /// Executes the submit commands core logic for this backend.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private protected abstract void SubmitCommandsCore(CommandList commandList, Fence fence);

    /// <summary>
    /// Executes the swap buffers core logic for this backend.
    /// </summary>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    private protected abstract void SwapBuffersCore(Swapchain swapchain);

    /// <summary>
    /// Executes the wait for idle core logic for this backend.
    /// </summary>
    private protected abstract void WaitForIdleCore();

    /// <summary>
    /// Executes the wait for next frame ready core logic for this backend.
    /// </summary>
    private protected abstract void WaitForNextFrameReadyCore();

    /// <summary>
    /// Updates the texture core state for this command sequence.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    private protected abstract void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer);

    /// <summary>
    /// Updates the buffer core state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    private protected abstract void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes);

    /// <summary>
    /// Gets the pixel format support core value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="properties">The properties value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private protected abstract bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties);

    /// <summary>
    /// Gets the d3 d12 info value.
    /// </summary>
    /// <param name="info">The info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public virtual bool GetD3D12Info(out BackendInfoD3D12 info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Gets the d3 d12 info value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public BackendInfoD3D12 GetD3D12Info() {
        if (!this.GetD3D12Info(out BackendInfoD3D12 info)) {
            throw new VeldridException($"{nameof(GetD3D12Info)} can only be used on a D3D12 GraphicsDevice.");
        }

        return info;
    }

    /// <summary>
    /// Creates the d3 d12 instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options) {
        return new D3D12GraphicsDevice(options, null);
    }

    /// <summary>
    /// Creates the d3 d12 instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDescription">The swapchain description value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new D3D12GraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Creates the d3 d12 instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="hwnd">The hwnd value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options, IntPtr hwnd, uint width, uint height) {
        SwapchainDescription swapchainDescription = new(SwapchainSource.CreateWin32(hwnd, IntPtr.Zero), width, height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new D3D12GraphicsDevice(options, swapchainDescription);
    }

#if !EXCLUDE_VULKAN_BACKEND

    /// <summary>
    /// Gets the vulkan info value.
    /// </summary>
    /// <param name="info">The info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public virtual bool GetVulkanInfo(out BackendInfoVulkan info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Gets the vulkan info value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public BackendInfoVulkan GetVulkanInfo() {
        if (!this.GetVulkanInfo(out BackendInfoVulkan info)) {
            throw new VeldridException($"{nameof(GetVulkanInfo)} can only be used on a Vulkan GraphicsDevice.");
        }

        return info;
    }
#endif

#if !EXCLUDE_METAL_BACKEND

    /// <summary>
    /// Gets the metal info value.
    /// </summary>
    /// <param name="info">The info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public virtual bool GetMetalInfo(out BackendInfoMetal info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Gets the metal info value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public BackendInfoMetal GetMetalInfo() {
        if (!this.GetMetalInfo(out BackendInfoMetal info)) {
            throw new VeldridException($"{nameof(GetMetalInfo)} can only be used on a Metal GraphicsDevice.");
        }

        return info;
    }

    /// <summary>
    /// Updates the active display state for this command sequence.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="w">The w value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
    public virtual void UpdateActiveDisplay(int x, int y, int w, int h) { }

    /// <summary>
    /// Gets the actual refresh period value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public virtual double GetActualRefreshPeriod() {
        return -1.0f;
    }
#endif

#if !EXCLUDE_VULKAN_BACKEND

    /// <summary>
    /// Creates the vulkan instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options) {
        return new VkGraphicsDevice(options, null);
    }

    /// <summary>
    /// Creates the vulkan instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="vkOptions">The vk options value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, VulkanDeviceOptions vkOptions) {
        return new VkGraphicsDevice(options, null, vkOptions);
    }

    /// <summary>
    /// Creates a Vulkan graphics device using the provided options and swapchain description.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDescription">The swapchain description value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new VkGraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Creates the vulkan instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDescription">The swapchain description value used by this operation.</param>
    /// <param name="vkOptions">The vk options value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription, VulkanDeviceOptions vkOptions) {
        return new VkGraphicsDevice(options, swapchainDescription, vkOptions);
    }

    /// <summary>
    /// Creates the vulkan instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="surfaceSource">The surface source value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, VkSurfaceSource surfaceSource, uint width, uint height) {
        SwapchainDescription scDesc = new(surfaceSource.GetSurfaceSource(), width, height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new VkGraphicsDevice(options, scDesc);
    }
#endif

#if !EXCLUDE_METAL_BACKEND

    /// <summary>
    /// Creates the metal instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options) {
        return new MtlGraphicsDevice(options, null);
    }

    /// <summary>
    /// Creates the metal instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="metalOptions">The metal options value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, MetalDeviceOptions metalOptions) {
        return new MtlGraphicsDevice(options, null, metalOptions);
    }

    /// <summary>
    /// Creates the metal instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDescription">The swapchain description value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new MtlGraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Creates the metal instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDescription">The swapchain description value used by this operation.</param>
    /// <param name="metalOptions">The metal options value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription, MetalDeviceOptions metalOptions) {
        return new MtlGraphicsDevice(options, swapchainDescription, metalOptions);
    }

    /// <summary>
    /// Creates the metal instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="nsWindow">The ns window value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, IntPtr nsWindow) {
        SwapchainDescription swapchainDesc = new(new NSWindowSwapchainSource(nsWindow), 0, 0, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new MtlGraphicsDevice(options, swapchainDesc);
    }

    /// <summary>
    /// Creates the metal instance used by this backend.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="nsWindow">The ns window value used by this operation.</param>
    /// <param name="metalOptions">The metal options value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, IntPtr nsWindow, MetalDeviceOptions metalOptions) {
        SwapchainDescription swapchainDesc = new(new NSWindowSwapchainSource(nsWindow), 0, 0, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new MtlGraphicsDevice(options, swapchainDesc, metalOptions);
    }
#endif
}