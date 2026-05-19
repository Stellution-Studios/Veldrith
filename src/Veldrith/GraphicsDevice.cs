using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Veldrith.D3D12;
using Veldrith.MTL;
using Veldrith.Vk;

namespace Veldrith;

/// <summary>
/// Represents the GraphicsDevice class.
/// </summary>
public abstract class GraphicsDevice : IDisposable {

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _deferredDisposalLock = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly List<IDisposable> _disposables = new();

    /// <summary>
    /// Represents the _aniso4XSampler field.
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
    /// Performs the GetUniformBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetUniformBufferMinOffsetAlignmentCore operation.</returns>
    public uint UniformBufferMinOffsetAlignment => this.GetUniformBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Performs the GetStructuredBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetStructuredBufferMinOffsetAlignmentCore operation.</returns>
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
    /// Performs the Dispose operation.
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
    /// Performs the IsBackendSupported operation.
    /// </summary>
    /// <param name="backend">The value of backend.</param>
    /// <returns>The result of the IsBackendSupported operation.</returns>
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
    /// Performs the SubmitCommands operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    public void SubmitCommands(CommandList commandList) {
        this.SubmitCommandsCore(commandList, null);
    }

    /// <summary>
    /// Performs the SubmitCommands operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    /// <param name="fence">The value of fence.</param>
    public void SubmitCommands(CommandList commandList, Fence fence) {
        this.SubmitCommandsCore(commandList, fence);
    }

    /// <summary>
    /// Performs the WaitForFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    public void WaitForFence(Fence fence) {
        if (!this.WaitForFence(fence, ulong.MaxValue)) {
            throw new VeldridException("The operation timed out before the Fence was signaled.");
        }
    }

    /// <summary>
    /// Performs the WaitForFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    /// <param name="timeout">The value of timeout.</param>
    /// <returns>The result of the WaitForFence operation.</returns>
    public bool WaitForFence(Fence fence, TimeSpan timeout) {
        return this.WaitForFence(fence, (ulong)timeout.TotalMilliseconds * 1_000_000);
    }

    /// <summary>
    /// Performs the WaitForFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFence operation.</returns>
    public abstract bool WaitForFence(Fence fence, ulong nanosecondTimeout);

    /// <summary>
    /// Performs the WaitForFences operation.
    /// </summary>
    /// <param name="fences">The value of fences.</param>
    /// <param name="waitAll">The value of waitAll.</param>
    public void WaitForFences(Fence[] fences, bool waitAll) {
        if (!this.WaitForFences(fences, waitAll, ulong.MaxValue)) {
            throw new VeldridException("The operation timed out before the Fence(s) were signaled.");
        }
    }

    /// <summary>
    /// Performs the WaitForFences operation.
    /// </summary>
    /// <param name="fences">The value of fences.</param>
    /// <param name="waitAll">The value of waitAll.</param>
    /// <param name="timeout">The value of timeout.</param>
    /// <returns>The result of the WaitForFences operation.</returns>
    public bool WaitForFences(Fence[] fences, bool waitAll, TimeSpan timeout) {
        return this.WaitForFences(fences, waitAll, (ulong)timeout.TotalMilliseconds * 1_000_000);
    }

    /// <summary>
    /// Performs the WaitForFences operation.
    /// </summary>
    /// <param name="fences">The value of fences.</param>
    /// <param name="waitAll">The value of waitAll.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFences operation.</returns>
    public abstract bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout);

    /// <summary>
    /// Performs the ResetFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    public abstract void ResetFence(Fence fence);

    /// <summary>
    /// Performs the SwapBuffers operation.
    /// </summary>
    public void SwapBuffers() {
        if (this.MainSwapchain == null) {
            throw new VeldridException("This GraphicsDevice was created without a main Swapchain, so the requested operation cannot be performed.");
        }

        this.SwapBuffers(this.MainSwapchain);
    }

    /// <summary>
    /// Performs the SwapBuffers operation.
    /// </summary>
    /// <param name="swapchain">The value of swapchain.</param>
    public void SwapBuffers(Swapchain swapchain) {
        this.SwapBuffersCore(swapchain);
    }

    /// <summary>
    /// Performs the ResizeMainWindow operation.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    public void ResizeMainWindow(uint width, uint height) {
        if (this.MainSwapchain == null) {
            throw new VeldridException("This GraphicsDevice was created without a main Swapchain, so the requested operation cannot be performed.");
        }

        this.MainSwapchain.Resize(width, height);
    }

    /// <summary>
    /// Performs the WaitForIdle operation.
    /// </summary>
    public void WaitForIdle() {
        this.WaitForIdleCore();
        this.FlushDeferredDisposals();
    }

    /// <summary>
    /// Performs the WaitForNextFrameReady operation.
    /// </summary>
    public void WaitForNextFrameReady() {
        this.WaitForNextFrameReadyCore();
    }

    /// <summary>
    /// Performs the GetSampleCountLimit operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="depthFormat">The value of depthFormat.</param>
    /// <returns>The result of the GetSampleCountLimit operation.</returns>
    public abstract TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat);

    /// <summary>
    /// Performs the Map operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="mode">The value of mode.</param>
    /// <returns>The result of the Map operation.</returns>
    public MappedResource Map(IMappableResource resource, MapMode mode) {
        return this.Map(resource, mode, 0);
    }

    /// <summary>
    /// Performs the Map operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="mode">The value of mode.</param>
    /// <param name="subresource">The value of subresource.</param>
    /// <returns>The result of the Map operation.</returns>
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
    /// <param name="resource">The <see cref="DeviceBuffer" /> or <see cref="Texture" /> resource to map.</param>
    /// <param name="mode">The <see cref="MapMode" /> to use.</param>
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
    /// <param name="resource">The <see cref="DeviceBuffer" /> or <see cref="Texture" /> resource to map.</param>
    /// <param name="mode">The <see cref="MapMode" /> to use.</param>
    /// <param name="subresource">The subresource to map. Subresources are indexed first by mip slice, then by array layer.</param>
    /// <typeparam name="T">The blittable value type which mapped data is viewed as.</typeparam>
    /// <returns>A <see cref="MappedResource" /> structure describing the mapped data region.</returns>
    public MappedResourceView<T> Map<T>(IMappableResource resource, MapMode mode, uint subresource)
        where T : unmanaged {
        MappedResource mappedResource = this.Map(resource, mode, subresource);
        return new MappedResourceView<T>(mappedResource);
    }

    /// <summary>
    /// Performs the Unmap operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    public void Unmap(IMappableResource resource) {
        this.Unmap(resource, 0);
    }

    /// <summary>
    /// Performs the Unmap operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="subresource">The value of subresource.</param>
    public void Unmap(IMappableResource resource, uint subresource) {
        this.UnmapCore(resource, subresource);
    }


    /// <summary>
    /// Performs the UpdateTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="z">The value of z.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
    public void UpdateTexture(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
#if VALIDATE_USAGE
        ValidateUpdateTextureParameters(texture, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
#endif
        this.UpdateTextureCore(texture, source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Updates a portion of a <see cref="Texture" /> resource with new data contained in an array
    /// </summary>
    /// <param name="texture">The resource to update.</param>
    /// <param name="source">
    /// An array containing the data to upload. This must contain tightly-packed pixel data for the
    /// region specified.
    /// </param>
    /// <param name="x">The minimum X value of the updated region.</param>
    /// <param name="y">The minimum Y value of the updated region.</param>
    /// <param name="z">The minimum Z value of the updated region.</param>
    /// <param name="width">The width of the updated region, in texels.</param>
    /// <param name="height">The height of the updated region, in texels.</param>
    /// <param name="depth">The depth of the updated region, in texels.</param>
    /// <param name="mipLevel">
    /// The mipmap level to update. Must be less than the total number of mipmaps contained in the
    /// <see cref="Texture" />.
    /// </param>
    /// <param name="arrayLayer">
    /// The array layer to update. Must be less than the total array layer count contained in the
    /// <see cref="Texture" />.
    /// </param>
    public void UpdateTexture<T>(Texture texture, T[] source, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) where T : unmanaged {
        this.UpdateTexture(texture, (ReadOnlySpan<T>)source, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Updates a portion of a <see cref="Texture" /> resource with new data contained in an array
    /// </summary>
    /// <param name="texture">The resource to update.</param>
    /// <param name="source">
    /// A readonly span containing the data to upload. This must contain tightly-packed pixel data for the
    /// region specified.
    /// </param>
    /// <param name="x">The minimum X value of the updated region.</param>
    /// <param name="y">The minimum Y value of the updated region.</param>
    /// <param name="z">The minimum Z value of the updated region.</param>
    /// <param name="width">The width of the updated region, in texels.</param>
    /// <param name="height">The height of the updated region, in texels.</param>
    /// <param name="depth">The depth of the updated region, in texels.</param>
    /// <param name="mipLevel">
    /// The mipmap level to update. Must be less than the total number of mipmaps contained in the
    /// <see cref="Texture" />.
    /// </param>
    /// <param name="arrayLayer">
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
    /// <param name="texture">The resource to update.</param>
    /// <param name="source">
    /// A readonly span containing the data to upload. This must contain tightly-packed pixel data for the
    /// region specified.
    /// </param>
    /// <param name="x">The minimum X value of the updated region.</param>
    /// <param name="y">The minimum Y value of the updated region.</param>
    /// <param name="z">The minimum Z value of the updated region.</param>
    /// <param name="width">The width of the updated region, in texels.</param>
    /// <param name="height">The height of the updated region, in texels.</param>
    /// <param name="depth">The depth of the updated region, in texels.</param>
    /// <param name="mipLevel">
    /// The mipmap level to update. Must be less than the total number of mipmaps contained in the
    /// <see cref="Texture" />.
    /// </param>
    /// <param name="arrayLayer">
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
            this.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)ptr, (uint)sizeof(T));
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
    /// <param name="source">A readonly span containing the data to upload.</param>
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
    /// <param name="source">A span containing the data to upload.</param>
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
            throw new VeldridException($"The data size given to UpdateBuffer is too large. The given buffer can only hold {buffer.SizeInBytes} total bytes. The requested update would require {bufferOffsetInBytes + sizeInBytes} bytes.");
        }

        if (sizeInBytes == 0) {
            return;
        }

        this.UpdateBufferCore(buffer, bufferOffsetInBytes, source, sizeInBytes);
    }

    /// <summary>
    /// Performs the GetPixelFormatSupport operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="usage">The value of usage.</param>
    /// <returns>The result of the GetPixelFormatSupport operation.</returns>
    public bool GetPixelFormatSupport(PixelFormat format, TextureType type, TextureUsage usage) {
        return this.GetPixelFormatSupportCore(format, type, usage, out _);
    }

    /// <summary>
    /// Performs the GetPixelFormatSupport operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="properties">The value of properties.</param>
    /// <returns>The result of the GetPixelFormatSupport operation.</returns>
    public bool GetPixelFormatSupport(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties) {
        return this.GetPixelFormatSupportCore(format, type, usage, out properties);
    }

    /// <summary>
    /// Performs the DisposeWhenIdle operation.
    /// </summary>
    /// <param name="disposable">The value of disposable.</param>
    public void DisposeWhenIdle(IDisposable disposable) {
        lock (this._deferredDisposalLock) {
            this._disposables.Add(disposable);
        }
    }

    /// <summary>
    /// Performs the GetUniformBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetUniformBufferMinOffsetAlignmentCore operation.</returns>
    internal abstract uint GetUniformBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Performs the GetStructuredBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetStructuredBufferMinOffsetAlignmentCore operation.</returns>
    internal abstract uint GetStructuredBufferMinOffsetAlignmentCore();

    /// <summary>
    /// Performs the MapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="mode">The value of mode.</param>
    /// <param name="subresource">The value of subresource.</param>
    /// <returns>The result of the MapCore operation.</returns>
    protected abstract MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource);

    /// <summary>
    /// Performs the UnmapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="subresource">The value of subresource.</param>
    protected abstract void UnmapCore(IMappableResource resource, uint subresource);

    /// <summary>
    /// Performs the PlatformDispose operation.
    /// </summary>
    protected abstract void PlatformDispose();

    /// <summary>
    /// Performs the PostDeviceCreated operation.
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
    /// Performs the ValidateUpdateTextureParameters operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="z">The value of z.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
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
    /// Performs the FlushDeferredDisposals operation.
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
    /// Performs the SubmitCommandsCore operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    /// <param name="fence">The value of fence.</param>
    private protected abstract void SubmitCommandsCore(CommandList commandList, Fence fence);

    /// <summary>
    /// Performs the SwapBuffersCore operation.
    /// </summary>
    /// <param name="swapchain">The value of swapchain.</param>
    private protected abstract void SwapBuffersCore(Swapchain swapchain);

    /// <summary>
    /// Performs the WaitForIdleCore operation.
    /// </summary>
    private protected abstract void WaitForIdleCore();

    /// <summary>
    /// Performs the WaitForNextFrameReadyCore operation.
    /// </summary>
    private protected abstract void WaitForNextFrameReadyCore();

    /// <summary>
    /// Performs the UpdateTextureCore operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="z">The value of z.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
    private protected abstract void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer);

    /// <summary>
    /// Performs the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="bufferOffsetInBytes">The value of bufferOffsetInBytes.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    private protected abstract void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes);

    /// <summary>
    /// Performs the GetPixelFormatSupportCore operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="properties">The value of properties.</param>
    /// <returns>The result of the GetPixelFormatSupportCore operation.</returns>
    private protected abstract bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties);

    /// <summary>
    /// Performs the GetD3D12Info operation.
    /// </summary>
    /// <param name="info">The value of info.</param>
    /// <returns>The result of the GetD3D12Info operation.</returns>
    public virtual bool GetD3D12Info(out BackendInfoD3D12 info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Performs the GetD3D12Info operation.
    /// </summary>
    /// <returns>The result of the GetD3D12Info operation.</returns>
    public BackendInfoD3D12 GetD3D12Info() {
        if (!this.GetD3D12Info(out BackendInfoD3D12 info)) {
            throw new VeldridException($"{nameof(GetD3D12Info)} can only be used on a D3D12 GraphicsDevice.");
        }

        return info;
    }

    /// <summary>
    /// Performs the CreateD3D12 operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CreateD3D12 operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options) {
        return new D3D12GraphicsDevice(options, null);
    }

    /// <summary>
    /// Performs the CreateD3D12 operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="swapchainDescription">The value of swapchainDescription.</param>
    /// <returns>The result of the CreateD3D12 operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new D3D12GraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Performs the CreateD3D12 operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="hwnd">The value of hwnd.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <returns>The result of the CreateD3D12 operation.</returns>
    public static GraphicsDevice CreateD3D12(GraphicsDeviceOptions options, IntPtr hwnd, uint width, uint height) {
        SwapchainDescription swapchainDescription = new(SwapchainSource.CreateWin32(hwnd, IntPtr.Zero), width, height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new D3D12GraphicsDevice(options, swapchainDescription);
    }

#if !EXCLUDE_VULKAN_BACKEND

    /// <summary>
    /// Performs the GetVulkanInfo operation.
    /// </summary>
    /// <param name="info">The value of info.</param>
    /// <returns>The result of the GetVulkanInfo operation.</returns>
    public virtual bool GetVulkanInfo(out BackendInfoVulkan info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Performs the GetVulkanInfo operation.
    /// </summary>
    /// <returns>The result of the GetVulkanInfo operation.</returns>
    public BackendInfoVulkan GetVulkanInfo() {
        if (!this.GetVulkanInfo(out BackendInfoVulkan info)) {
            throw new VeldridException($"{nameof(GetVulkanInfo)} can only be used on a Vulkan GraphicsDevice.");
        }

        return info;
    }
#endif

#if !EXCLUDE_METAL_BACKEND

    /// <summary>
    /// Performs the GetMetalInfo operation.
    /// </summary>
    /// <param name="info">The value of info.</param>
    /// <returns>The result of the GetMetalInfo operation.</returns>
    public virtual bool GetMetalInfo(out BackendInfoMetal info) {
        info = null;
        return false;
    }

    /// <summary>
    /// Performs the GetMetalInfo operation.
    /// </summary>
    /// <returns>The result of the GetMetalInfo operation.</returns>
    public BackendInfoMetal GetMetalInfo() {
        if (!this.GetMetalInfo(out BackendInfoMetal info)) {
            throw new VeldridException($"{nameof(GetMetalInfo)} can only be used on a Metal GraphicsDevice.");
        }

        return info;
    }

    /// <summary>
    /// Performs the UpdateActiveDisplay operation.
    /// </summary>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="w">The value of w.</param>
    /// <param name="h">The value of h.</param>
    public virtual void UpdateActiveDisplay(int x, int y, int w, int h) { }

    /// <summary>
    /// Performs the GetActualRefreshPeriod operation.
    /// </summary>
    /// <returns>The result of the GetActualRefreshPeriod operation.</returns>
    public virtual double GetActualRefreshPeriod() {
        return -1.0f;
    }
#endif

#if !EXCLUDE_VULKAN_BACKEND

    /// <summary>
    /// Performs the CreateVulkan operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options) {
        return new VkGraphicsDevice(options, null);
    }

    /// <summary>
    /// Performs the CreateVulkan operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="vkOptions">The value of vkOptions.</param>
    /// <returns>The result of the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, VulkanDeviceOptions vkOptions) {
        return new VkGraphicsDevice(options, null, vkOptions);
    }

    /// <summary>
    /// Creates a Vulkan graphics device using the provided options and swapchain description.
    /// </summary>
    /// <param name="options">General graphics device creation options.</param>
    /// <param name="swapchainDescription">The swapchain description to initialize with.</param>
    /// <returns>A newly created Vulkan graphics device.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new VkGraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Performs the CreateVulkan operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="swapchainDescription">The value of swapchainDescription.</param>
    /// <param name="vkOptions">The value of vkOptions.</param>
    /// <returns>The result of the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription, VulkanDeviceOptions vkOptions) {
        return new VkGraphicsDevice(options, swapchainDescription, vkOptions);
    }

    /// <summary>
    /// Performs the CreateVulkan operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="surfaceSource">The value of surfaceSource.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <returns>The result of the CreateVulkan operation.</returns>
    public static GraphicsDevice CreateVulkan(GraphicsDeviceOptions options, VkSurfaceSource surfaceSource, uint width, uint height) {
        SwapchainDescription scDesc = new(surfaceSource.GetSurfaceSource(), width, height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new VkGraphicsDevice(options, scDesc);
    }
#endif

#if !EXCLUDE_METAL_BACKEND

    /// <summary>
    /// Performs the CreateMetal operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options) {
        return new MtlGraphicsDevice(options, null);
    }

    /// <summary>
    /// Performs the CreateMetal operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="metalOptions">The value of metalOptions.</param>
    /// <returns>The result of the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, MetalDeviceOptions metalOptions) {
        return new MtlGraphicsDevice(options, null, metalOptions);
    }

    /// <summary>
    /// Performs the CreateMetal operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="swapchainDescription">The value of swapchainDescription.</param>
    /// <returns>The result of the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription) {
        return new MtlGraphicsDevice(options, swapchainDescription);
    }

    /// <summary>
    /// Performs the CreateMetal operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="swapchainDescription">The value of swapchainDescription.</param>
    /// <param name="metalOptions">The value of metalOptions.</param>
    /// <returns>The result of the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, SwapchainDescription swapchainDescription, MetalDeviceOptions metalOptions) {
        return new MtlGraphicsDevice(options, swapchainDescription, metalOptions);
    }

    /// <summary>
    /// Performs the CreateMetal operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="nsWindow">The value of nsWindow.</param>
    /// <returns>The result of the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, IntPtr nsWindow) {
        SwapchainDescription swapchainDesc = new(new NSWindowSwapchainSource(nsWindow), 0, 0, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new MtlGraphicsDevice(options, swapchainDesc);
    }

    /// <summary>
    /// Performs the CreateMetal operation.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="nsWindow">The value of nsWindow.</param>
    /// <param name="metalOptions">The value of metalOptions.</param>
    /// <returns>The result of the CreateMetal operation.</returns>
    public static GraphicsDevice CreateMetal(GraphicsDeviceOptions options, IntPtr nsWindow, MetalDeviceOptions metalOptions) {
        SwapchainDescription swapchainDesc = new(new NSWindowSwapchainSource(nsWindow), 0, 0, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat);

        return new MtlGraphicsDevice(options, swapchainDesc, metalOptions);
    }
#endif
}