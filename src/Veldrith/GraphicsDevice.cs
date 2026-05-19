using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Veldrith.D3D12;
using Veldrith.MTL;
using Veldrith.Vk;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the GraphicsDevice class.
/// </summary>
public abstract class GraphicsDevice : IDisposable {

    /// <summary>
    /// Stores the value associated with <c>_deferredDisposalLock</c>.
    /// </summary>
    private readonly object _deferredDisposalLock = new();

    /// <summary>
    /// Stores the value associated with <c>_disposables</c>.
    /// </summary>
    private readonly List<IDisposable> _disposables = new();

    /// <summary>
    /// Stores the value associated with <c>_aniso4XSampler</c>.
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
    /// If true, (0, 0) refers to the top-left texel of a Texture. If false, (0, 0) refers to the bottom-left
    /// texel of a Texture. This property is useful for determining how the output of a Framebuffer should be sampled.
    /// </summary>
    public abstract bool IsUvOriginTopLeft { get; }

    /// <summary>
    /// Gets a value indicating whether this device's depth values range from 0 to 1.
    /// If false, depth values instead range from -1 to 1.
    /// </summary>
    public abstract bool IsDepthRangeZeroToOne { get; }

    /// <summary>
    /// Gets a value indicating whether this device's clip space Y values increase from top (-1) to bottom (1).
    /// If false, clip space Y values instead increase from bottom (-1) to top (1).
    /// </summary>
    public abstract bool IsClipSpaceYInverted { get; }

    /// <summary>
    /// Gets the <see cref="ResourceFactory" /> controlled by this instance.
    /// </summary>
    public abstract ResourceFactory ResourceFactory { get; }

    /// <summary>
    /// Retrieves the main Swapchain for this device. This property is only valid if the device was created with a main
    /// Swapchain, and will return null otherwise.
    /// </summary>
    public abstract Swapchain MainSwapchain { get; }

    /// <summary>
    /// Gets a <see cref="GraphicsDeviceFeatures" /> which enumerates the optional features supported by this instance.
    /// </summary>
    public abstract GraphicsDeviceFeatures Features { get; }

    /// <summary>
    /// Executes the GetUniformBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetUniformBufferMinOffsetAlignmentCore operation.</returns>
    public uint UniformBufferMinOffsetAlignment => this.GetUniformBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Executes the GetStructuredBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetStructuredBufferMinOffsetAlignmentCore operation.</returns>
    public uint StructuredBufferMinOffsetAlignment => this.GetStructuredBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Gets a <see cref="Framebuffer" /> object representing the render targets of the main swapchain.
    /// This is equivalent to <see cref="MainSwapchain" />.<see cref="Swapchain.Framebuffer" />.
    /// If this GraphicsDevice was created without a main Swapchain, then this returns null.
    /// </summary>
    public Framebuffer SwapchainFramebuffer => this.MainSwapchain?.Framebuffer;

    /// <summary>
    /// Gets a simple 4x anisotropic-filtered <see cref="Sampler" /> object owned by this instance.
    /// This object is created with <see cref="SamplerDescription.ANISO4_X" />.
    /// This property can only be used when <see cref="GraphicsDeviceFeatures.SamplerAnisotropy" /> is supported.
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
    /// system's
    /// vertical refresh rate.
    /// This is equivalent to <see cref="MainSwapchain" />.<see cref="Swapchain.SyncToVerticalBlank" />.
    /// This property cannot be set if this GraphicsDevice was created without a main Swapchain.
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
    /// occurs.
    /// This will only have an effect if <see cref="SyncToVerticalBlank" /> is false.
    /// </summary>
    public virtual bool AllowTearing { get; set; }

    /// <summary>
    /// Gets a simple point-filtered <see cref="Sampler" /> object owned by this instance.
    /// This object is created with <see cref="SamplerDescription.POINT" />.
    /// </summary>
    public Sampler PointSampler { get; private set; }

    /// <summary>
    /// Gets a simple linear-filtered <see cref="Sampler" /> object owned by this instance.
    /// This object is created with <see cref="SamplerDescription.LINEAR" />.
    /// </summary>
    public Sampler LinearSampler { get; private set; }

    #region Disposal

    /// <summary>
    /// Executes the Dispose operation.
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
    /// Executes the IsBackendSupported operation.
    /// </summary>
    /// <param name="backend">Specifies the value of <paramref name="backend" />.</param>
    /// <returns>Returns the result produced by the IsBackendSupported operation.</returns>
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
    /// Executes the SubmitCommands operation.
    /// </summary>
    /// <param name="commandList">Specifies the value of <paramref name="commandList" />.</param>
    public void SubmitCommands(CommandList commandList) {
        this.SubmitCommandsCore(commandList, null);
    }

    /// <summary>
    /// Executes the SubmitCommands operation.
    /// </summary>
    /// <param name="commandList">Specifies the value of <paramref name="commandList" />.</param>
    /// <param name="fence">Specifies the value of <paramref name="fence" />.</param>
    public void SubmitCommands(CommandList commandList, Fence fence) {
        this.SubmitCommandsCore(commandList, fence);
    }

    /// <summary>
    /// Executes the WaitForFence operation.
    /// </summary>
    /// <param name="fence">Specifies the value of <paramref name="fence" />.</param>
    public void WaitForFence(Fence fence) {
        if (!this.WaitForFence(fence, ulong.MaxValue)) {
            throw new VeldridException("The operation timed out before the Fence was signaled.");
        }
    }

    /// <summary>
    /// Executes the WaitForFence operation.
    /// </summary>
    /// <param name="fence">Specifies the value of <paramref name="fence" />.</param>
    /// <param name="timeout">Specifies the value of <paramref name="timeout" />.</param>
    /// <returns>Returns the result produced by the WaitForFence operation.</returns>
    public bool WaitForFence(Fence fence, TimeSpan timeout) {
        return this.WaitForFence(fence, (ulong)timeout.TotalMilliseconds * 1_000_000);
    }

    /// <summary>
    /// Executes the WaitForFence operation.
    /// </summary>
    /// <param name="fence">Specifies the value of <paramref name="fence" />.</param>
    /// <param name="nanosecondTimeout">Specifies the value of <paramref name="nanosecondTimeout" />.</param>
    /// <returns>Returns the result produced by the WaitForFence operation.</returns>
    public abstract bool WaitForFence(Fence fence, ulong nanosecondTimeout);

    /// <summary>
    /// Executes the WaitForFences operation.
    /// </summary>
    /// <param name="fences">Specifies the value of <paramref name="fences" />.</param>
    /// <param name="waitAll">Specifies the value of <paramref name="waitAll" />.</param>
    public void WaitForFences(Fence[] fences, bool waitAll) {
        if (!this.WaitForFences(fences, waitAll, ulong.MaxValue)) {
            throw new VeldridException("The operation timed out before the Fence(s) were signaled.");
        }
    }

    /// <summary>
    /// Executes the WaitForFences operation.
    /// </summary>
    /// <param name="fences">Specifies the value of <paramref name="fences" />.</param>
    /// <param name="waitAll">Specifies the value of <paramref name="waitAll" />.</param>
    /// <param name="timeout">Specifies the value of <paramref name="timeout" />.</param>
    /// <returns>Returns the result produced by the WaitForFences operation.</returns>
    public bool WaitForFences(Fence[] fences, bool waitAll, TimeSpan timeout) {
        return this.WaitForFences(fences, waitAll, (ulong)timeout.TotalMilliseconds * 1_000_000);
    }

    /// <summary>
    /// Executes the WaitForFences operation.
    /// </summary>
    /// <param name="fences">Specifies the value of <paramref name="fences" />.</param>
    /// <param name="waitAll">Specifies the value of <paramref name="waitAll" />.</param>
    /// <param name="nanosecondTimeout">Specifies the value of <paramref name="nanosecondTimeout" />.</param>
    /// <returns>Returns the result produced by the WaitForFences operation.</returns>
    public abstract bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout);

    /// <summary>
    /// Executes the ResetFence operation.
    /// </summary>
    /// <param name="fence">Specifies the value of <paramref name="fence" />.</param>
    public abstract void ResetFence(Fence fence);

    /// <summary>
    /// Executes the SwapBuffers operation.
    /// </summary>
    public void SwapBuffers() {
        if (this.MainSwapchain == null) {
            throw new VeldridException("This GraphicsDevice was created without a main Swapchain, so the requested operation cannot be performed.");
        }

        this.SwapBuffers(this.MainSwapchain);
    }

    /// <summary>
    /// Executes the SwapBuffers operation.
    /// </summary>
    /// <param name="swapchain">Specifies the value of <paramref name="swapchain" />.</param>
    public void SwapBuffers(Swapchain swapchain) {
        this.SwapBuffersCore(swapchain);
    }

    /// <summary>
    /// Executes the ResizeMainWindow operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public void ResizeMainWindow(uint width, uint height) {
        if (this.MainSwapchain == null) {
            throw new VeldridException("This GraphicsDevice was created without a main Swapchain, so the requested operation cannot be performed.");
        }

        this.MainSwapchain.Resize(width, height);
    }

    /// <summary>
    /// Executes the WaitForIdle operation.
    /// </summary>
    public void WaitForIdle() {
        this.WaitForIdleCore();
        this.FlushDeferredDisposals();
    }

    /// <summary>
    /// Executes the WaitForNextFrameReady operation.
    /// </summary>
    public void WaitForNextFrameReady() {
        this.WaitForNextFrameReadyCore();
    }

    /// <summary>
    /// Executes the GetSampleCountLimit operation.
    /// </summary>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="depthFormat">Specifies the value of <paramref name="depthFormat" />.</param>
    /// <returns>Returns the result produced by the GetSampleCountLimit operation.</returns>
    public abstract TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat);

    /// <summary>
    /// Executes the Map operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <returns>Returns the result produced by the Map operation.</returns>
    public MappedResource Map(IMappableResource resource, MapMode mode) {
        return this.Map(resource, mode, 0);
    }

    /// <summary>
    /// Executes the Map operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <returns>Returns the result produced by the Map operation.</returns>
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
    /// structured
    /// view over that region. For Texture resources, this overload maps the first subresource.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <typeparam name="T">The blittable value type which mapped data is viewed as.</typeparam>
    /// <returns>A <see cref="MappedResource" /> structure describing the mapped data region.</returns>
    public MappedResourceView<T> Map<T>(IMappableResource resource, MapMode mode) where T : unmanaged {
        return this.Map<T>(resource, mode, 0);
    }

    /// <summary>
    /// Maps a <see cref="DeviceBuffer" /> or <see cref="Texture" /> into a CPU-accessible data region, and returns a
    /// structured
    /// view over that region.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <typeparam name="T">The blittable value type which mapped data is viewed as.</typeparam>
    /// <returns>A <see cref="MappedResource" /> structure describing the mapped data region.</returns>
    public MappedResourceView<T> Map<T>(IMappableResource resource, MapMode mode, uint subresource)
        where T : unmanaged {
        MappedResource mappedResource = this.Map(resource, mode, subresource);
        return new MappedResourceView<T>(mappedResource);
    }

    /// <summary>
    /// Executes the Unmap operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    public void Unmap(IMappableResource resource) {
        this.Unmap(resource, 0);
    }

    /// <summary>
    /// Executes the Unmap operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    public void Unmap(IMappableResource resource, uint subresource) {
        this.UnmapCore(resource, subresource);
    }


    /// <summary>
    /// Executes the UpdateTexture operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    public void UpdateTexture(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
#if VALIDATE_USAGE
        ValidateUpdateTextureParameters(texture, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
#endif
        this.UpdateTextureCore(texture, source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Updates a portion of a <see cref="Texture" /> resource with new data contained in an array
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// An array containing the data to upload. This must contain tightly-packed pixel data for the
    /// region specified.
    /// </param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// The mipmap level to update. Must be less than the total number of mipmaps contained in the
    /// <see cref="Texture" />.
    /// </param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// The array layer to update. Must be less than the total array layer count contained in the
    /// <see cref="Texture" />.
    /// </param>
    public void UpdateTexture<T>(Texture texture, T[] source, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) where T : unmanaged {
        this.UpdateTexture(texture, (ReadOnlySpan<T>)source, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Updates a portion of a <see cref="Texture" /> resource with new data contained in an array
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// A readonly span containing the data to upload. This must contain tightly-packed pixel data for the
    /// region specified.
    /// </param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// The mipmap level to update. Must be less than the total number of mipmaps contained in the
    /// <see cref="Texture" />.
    /// </param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// The array layer to update. Must be less than the total array layer count contained in the
    /// <see cref="Texture" />.
    /// </param>
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
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// A readonly span containing the data to upload. This must contain tightly-packed pixel data for the
    /// region specified.
    /// </param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// The mipmap level to update. Must be less than the total number of mipmaps contained in the
    /// <see cref="Texture" />.
    /// </param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// The array layer to update. Must be less than the total array layer count contained in the
    /// <see cref="Texture" />.
    /// </param>
    public void UpdateTexture<T>(Texture texture, Span<T> source, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) where T : unmanaged {
        this.UpdateTexture(texture, (ReadOnlySpan<T>)source, x, y, z, width, height, depth, mipLevel, arrayLayer);
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
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)ptr, (uint)sizeof(T));
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
            throw new VeldridException($"The data size given to UpdateBuffer is too large. The given buffer can only hold {buffer.SizeInBytes} total bytes. The requested update would require {bufferOffsetInBytes + sizeInBytes} bytes.");
        }

        if (sizeInBytes == 0) {
            return;
        }

        this.UpdateBufferCore(buffer, bufferOffsetInBytes, source, sizeInBytes);
    }

    /// <summary>
    /// Executes the GetPixelFormatSupport operation.
    /// </summary>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="type">Specifies the value of <paramref name="type" />.</param>
    /// <param name="usage">Specifies the value of <paramref name="usage" />.</param>
    /// <returns>Returns the result produced by the GetPixelFormatSupport operation.</returns>
    public bool GetPixelFormatSupport(PixelFormat format, TextureType type, TextureUsage usage) {
        return this.GetPixelFormatSupportCore(format, type, usage, out _);
    }

    /// <summary>
    /// Executes the GetPixelFormatSupport operation.
    /// </summary>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="type">Specifies the value of <paramref name="type" />.</param>
    /// <param name="usage">Specifies the value of <paramref name="usage" />.</param>
    /// <param name="properties">Specifies the value of <paramref name="properties" />.</param>
    /// <returns>Returns the result produced by the GetPixelFormatSupport operation.</returns>
    public bool GetPixelFormatSupport(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties) {
        return this.GetPixelFormatSupportCore(format, type, usage, out properties);
    }

    /// <summary>
    /// Executes the DisposeWhenIdle operation.
    /// </summary>
    /// <param name="disposable">Specifies the value of <paramref name="disposable" />.</param>
    public void DisposeWhenIdle(IDisposable disposable) {
        lock (this._deferredDisposalLock) {
            this._disposables.Add(disposable);
        }
    }

    /// <summary>
    /// Executes the GetUniformBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetUniformBufferMinOffsetAlignmentCore operation.</returns>
    internal abstract uint GetUniformBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Executes the GetStructuredBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetStructuredBufferMinOffsetAlignmentCore operation.</returns>
    internal abstract uint GetStructuredBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Executes the MapCore operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <returns>Returns the result produced by the MapCore operation.</returns>
    protected abstract MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource);

    /// <summary>
    /// Executes the UnmapCore operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    protected abstract void UnmapCore(IMappableResource resource, uint subresource);

    /// <summary>
    /// Executes the PlatformDispose operation.
    /// </summary>
    protected abstract void PlatformDispose();

    /// <summary>
    /// Executes the PostDeviceCreated operation.
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
    /// Executes the ValidateUpdateTextureParameters operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
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
    /// Executes the FlushDeferredDisposals operation.
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
    /// Executes the SubmitCommandsCore operation.
    /// </summary>
    /// <param name="commandList">Specifies the value of <paramref name="commandList" />.</param>
    /// <param name="fence">Specifies the value of <paramref name="fence" />.</param>
    private protected abstract void SubmitCommandsCore(CommandList commandList, Fence fence);

    /// <summary>
    /// Executes the SwapBuffersCore operation.
    /// </summary>
    /// <param name="swapchain">Specifies the value of <paramref name="swapchain" />.</param>
    private protected abstract void SwapBuffersCore(Swapchain swapchain);

    /// <summary>
    /// Executes the WaitForIdleCore operation.
    /// </summary>
    private protected abstract void WaitForIdleCore();

    /// <summary>
    /// Executes the WaitForNextFrameReadyCore operation.
    /// </summary>
    private protected abstract void WaitForNextFrameReadyCore();

    /// <summary>
    /// Executes the UpdateTextureCore operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    private protected abstract void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer);

    /// <summary>
    /// Executes the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    private protected abstract void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes);

    /// <summary>
    /// Executes the GetPixelFormatSupportCore operation.
    /// </summary>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="type">Specifies the value of <paramref name="type" />.</param>
    /// <param name="usage">Specifies the value of <paramref name="usage" />.</param>
    /// <param name="properties">Specifies the value of <paramref name="properties" />.</param>
    /// <returns>Returns the result produced by the GetPixelFormatSupportCore operation.</returns>
    private protected abstract bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties);

    /// <summary>
    /// Executes the GetD3D12Info operation.
    /// </summary>
    /// <param name="info">Specifies the value of <paramref name="info" />.</param>
    /// <returns>Returns the result produced by the GetD3D12Info operation.</returns>
    public virtual bool GetD3D12Info(out BackendInfoD3D12 info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Executes the GetD3D12Info operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetD3D12Info operation.</returns>
    public BackendInfoD3D12 GetD3D12Info() {
        if (!this.GetD3D12Info(out BackendInfoD3D12 info)) {
            throw new VeldridException($"{nameof(GetD3D12Info)} can only be used on a D3D12 GraphicsDevice.");
        }

        return info;
    }

    /// <summary>
    /// Executes the CreateD3D12 operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the CreateD3D12 operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options) {
        return new D3D12GraphicsDevice(options, null);
    }

    /// <summary>
    /// Executes the CreateD3D12 operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="swapchainDescription">Specifies the value of <paramref name="swapchainDescription" />.</param>
    /// <returns>Returns the result produced by the CreateD3D12 operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new D3D12GraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Executes the CreateD3D12 operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="hwnd">Specifies the value of <paramref name="hwnd" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <returns>Returns the result produced by the CreateD3D12 operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options, IntPtr hwnd, uint width, uint height) {
        SwapchainDescription swapchainDescription = new(SwapchainSource.CreateWin32(hwnd, IntPtr.Zero), width, height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new D3D12GraphicsDevice(options, swapchainDescription);
    }

#if !EXCLUDE_VULKAN_BACKEND

    /// <summary>
    /// Executes the GetVulkanInfo operation.
    /// </summary>
    /// <param name="info">Specifies the value of <paramref name="info" />.</param>
    /// <returns>Returns the result produced by the GetVulkanInfo operation.</returns>
    public virtual bool GetVulkanInfo(out BackendInfoVulkan info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Executes the GetVulkanInfo operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetVulkanInfo operation.</returns>
    public BackendInfoVulkan GetVulkanInfo() {
        if (!this.GetVulkanInfo(out BackendInfoVulkan info)) {
            throw new VeldridException($"{nameof(GetVulkanInfo)} can only be used on a Vulkan GraphicsDevice.");
        }

        return info;
    }
#endif

#if !EXCLUDE_METAL_BACKEND

    /// <summary>
    /// Executes the GetMetalInfo operation.
    /// </summary>
    /// <param name="info">Specifies the value of <paramref name="info" />.</param>
    /// <returns>Returns the result produced by the GetMetalInfo operation.</returns>
    public virtual bool GetMetalInfo(out BackendInfoMetal info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Executes the GetMetalInfo operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetMetalInfo operation.</returns>
    public BackendInfoMetal GetMetalInfo() {
        if (!this.GetMetalInfo(out BackendInfoMetal info)) {
            throw new VeldridException($"{nameof(GetMetalInfo)} can only be used on a Metal GraphicsDevice.");
        }

        return info;
    }

    /// <summary>
    /// Executes the UpdateActiveDisplay operation.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="w">Specifies the value of <paramref name="w" />.</param>
    /// <param name="h">Specifies the value of <paramref name="h" />.</param>
    public virtual void UpdateActiveDisplay(int x, int y, int w, int h) { }

    /// <summary>
    /// Executes the GetActualRefreshPeriod operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetActualRefreshPeriod operation.</returns>
    public virtual double GetActualRefreshPeriod() {
        return -1.0f;
    }
#endif

#if !EXCLUDE_VULKAN_BACKEND

    /// <summary>
    /// Executes the CreateVulkan operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options) {
        return new VkGraphicsDevice(options, null);
    }

    /// <summary>
    /// Executes the CreateVulkan operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="vkOptions">Specifies the value of <paramref name="vkOptions" />.</param>
    /// <returns>Returns the result produced by the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, VulkanDeviceOptions vkOptions) {
        return new VkGraphicsDevice(options, null, vkOptions);
    }

    /// <summary>
    /// Creates a Vulkan graphics device using the provided options and swapchain description.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="swapchainDescription">Specifies the value of <paramref name="swapchainDescription" />.</param>
    /// <returns>A newly created Vulkan graphics device.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new VkGraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Executes the CreateVulkan operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="swapchainDescription">Specifies the value of <paramref name="swapchainDescription" />.</param>
    /// <param name="vkOptions">Specifies the value of <paramref name="vkOptions" />.</param>
    /// <returns>Returns the result produced by the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription, VulkanDeviceOptions vkOptions) {
        return new VkGraphicsDevice(options, swapchainDescription, vkOptions);
    }

    /// <summary>
    /// Executes the CreateVulkan operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="surfaceSource">Specifies the value of <paramref name="surfaceSource" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <returns>Returns the result produced by the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, VkSurfaceSource surfaceSource, uint width, uint height) {
        SwapchainDescription scDesc = new(surfaceSource.GetSurfaceSource(), width, height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new VkGraphicsDevice(options, scDesc);
    }
#endif

#if !EXCLUDE_METAL_BACKEND

    /// <summary>
    /// Executes the CreateMetal operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options) {
        return new MtlGraphicsDevice(options, null);
    }

    /// <summary>
    /// Executes the CreateMetal operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="metalOptions">Specifies the value of <paramref name="metalOptions" />.</param>
    /// <returns>Returns the result produced by the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, MetalDeviceOptions metalOptions) {
        return new MtlGraphicsDevice(options, null, metalOptions);
    }

    /// <summary>
    /// Executes the CreateMetal operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="swapchainDescription">Specifies the value of <paramref name="swapchainDescription" />.</param>
    /// <returns>Returns the result produced by the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new MtlGraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Executes the CreateMetal operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="swapchainDescription">Specifies the value of <paramref name="swapchainDescription" />.</param>
    /// <param name="metalOptions">Specifies the value of <paramref name="metalOptions" />.</param>
    /// <returns>Returns the result produced by the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription, MetalDeviceOptions metalOptions) {
        return new MtlGraphicsDevice(options, swapchainDescription, metalOptions);
    }

    /// <summary>
    /// Executes the CreateMetal operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="nsWindow">Specifies the value of <paramref name="nsWindow" />.</param>
    /// <returns>Returns the result produced by the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, IntPtr nsWindow) {
        SwapchainDescription swapchainDesc = new(new NSWindowSwapchainSource(nsWindow), 0, 0, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new MtlGraphicsDevice(options, swapchainDesc);
    }

    /// <summary>
    /// Executes the CreateMetal operation.
    /// </summary>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="nsWindow">Specifies the value of <paramref name="nsWindow" />.</param>
    /// <param name="metalOptions">Specifies the value of <paramref name="metalOptions" />.</param>
    /// <returns>Returns the result produced by the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, IntPtr nsWindow, MetalDeviceOptions metalOptions) {
        SwapchainDescription swapchainDesc = new(new NSWindowSwapchainSource(nsWindow), 0, 0, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new MtlGraphicsDevice(options, swapchainDesc, metalOptions);
    }
#endif
}
