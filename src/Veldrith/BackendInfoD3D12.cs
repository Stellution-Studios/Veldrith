using System;

namespace Veldrith;

/// <summary>
/// Exposes backend-specific native handles and metadata for BackendInfoD3D12.
/// </summary>
public sealed class BackendInfoD3D12 {

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInfoD3D12" /> type.
    /// </summary>
    /// <param name="device">The device value used by this operation.</param>
    internal BackendInfoD3D12(IntPtr device) {
        this.Device = device;
    }

    /// <summary>
    /// Gets a pointer to the native ID3D12Device.
    /// </summary>
    public IntPtr Device { get; }
}