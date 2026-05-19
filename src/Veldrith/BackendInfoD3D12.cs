using System;

namespace Veldrith;

/// <summary>
/// Represents the BackendInfoD3D12 class.
/// </summary>
public sealed class BackendInfoD3D12 {

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInfoD3D12" /> type.
    /// </summary>
    /// <param name="device">The value of device.</param>
    internal BackendInfoD3D12(IntPtr device) {
        this.Device = device;
    }

    /// <summary>
    /// Gets a pointer to the native ID3D12Device.
    /// This is <see cref="IntPtr.Zero" /> while the backend is running in fallback mode.
    /// </summary>
    public IntPtr Device { get; }
}