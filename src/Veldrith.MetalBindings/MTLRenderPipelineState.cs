using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPipelineState struct.
/// </summary>
public struct MTLRenderPipelineState {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineState" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLRenderPipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }
}