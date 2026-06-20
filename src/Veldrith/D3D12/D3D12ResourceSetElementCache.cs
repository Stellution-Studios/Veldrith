using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Stores pre-resolved D3D12 resources for one ResourceSet element.
/// </summary>
internal readonly struct D3D12ResourceSetElementCache {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceSetElementCache" /> struct.
    /// </summary>
    /// <param name="buffer">The D3D12 buffer referenced by this element.</param>
    /// <param name="bufferOffset">The static byte offset from a buffer range.</param>
    /// <param name="textureView">The D3D12 texture view referenced by this element.</param>
    /// <param name="sampler">The D3D12 sampler referenced by this element.</param>
    /// <param name="srvDescriptor">The persistent CPU SRV descriptor.</param>
    /// <param name="uavDescriptor">The persistent CPU UAV descriptor.</param>
    /// <param name="samplerDescriptor">The persistent CPU sampler descriptor.</param>
    internal D3D12ResourceSetElementCache(D3D12DeviceBuffer buffer, uint bufferOffset, D3D12TextureView textureView, D3D12Sampler sampler, CpuDescriptorHandle srvDescriptor, CpuDescriptorHandle uavDescriptor, CpuDescriptorHandle samplerDescriptor) {
        this.Buffer = buffer;
        this.BufferOffset = bufferOffset;
        this.TextureView = textureView;
        this.Sampler = sampler;
        this.SrvDescriptor = srvDescriptor;
        this.UavDescriptor = uavDescriptor;
        this.SamplerDescriptor = samplerDescriptor;
    }

    /// <summary>
    /// Gets the D3D12 buffer referenced by this element.
    /// </summary>
    internal D3D12DeviceBuffer Buffer { get; }

    /// <summary>
    /// Gets the static byte offset from a DeviceBufferRange binding.
    /// </summary>
    internal uint BufferOffset { get; }

    /// <summary>
    /// Gets the D3D12 texture view referenced by this element.
    /// </summary>
    internal D3D12TextureView TextureView { get; }

    /// <summary>
    /// Gets the D3D12 sampler referenced by this element.
    /// </summary>
    internal D3D12Sampler Sampler { get; }

    /// <summary>
    /// Gets the persistent CPU SRV descriptor for this element.
    /// </summary>
    internal CpuDescriptorHandle SrvDescriptor { get; }

    /// <summary>
    /// Gets the persistent CPU UAV descriptor for this element.
    /// </summary>
    internal CpuDescriptorHandle UavDescriptor { get; }

    /// <summary>
    /// Gets the persistent CPU sampler descriptor for this element.
    /// </summary>
    internal CpuDescriptorHandle SamplerDescriptor { get; }
}
