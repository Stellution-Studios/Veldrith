using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLComputePipelineState data structure used by the graphics runtime.
/// </summary>
public struct MTLComputePipelineState {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLComputePipelineState" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLComputePipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;
}