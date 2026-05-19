using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLComputePipelineState struct.
/// </summary>
public struct MTLComputePipelineState {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLComputePipelineState" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLComputePipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;
}