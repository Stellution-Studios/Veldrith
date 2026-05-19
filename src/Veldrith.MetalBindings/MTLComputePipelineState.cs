using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLComputePipelineState struct.
/// </summary>
public struct MTLComputePipelineState {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLComputePipelineState" /> class.
    /// </summary>
    public MTLComputePipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;
}