using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPipelineState data structure used by the graphics runtime.
/// </summary>
public struct MTLRenderPipelineState {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineState" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLRenderPipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }
}