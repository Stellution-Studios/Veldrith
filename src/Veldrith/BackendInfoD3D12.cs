using System;

namespace Veldrith;

/// <summary>
///     Exposes Direct3D12-specific functionality and metadata.
///     Can only be used on <see cref="GraphicsBackend.Direct3D12" /> devices.
/// </summary>
public sealed class BackendInfoD3D12 {
    internal BackendInfoD3D12(IntPtr device) {
        this.Device = device;
    }

    /// <summary>
    ///     Gets a pointer to the native ID3D12Device.
    ///     This is <see cref="IntPtr.Zero" /> while the backend is running in fallback mode.
    /// </summary>
    public IntPtr Device { get; }
}