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
    /// Initializes a new instance of the <see cref="MTLRenderPipelineState" /> class.
    /// </summary>
    public MTLRenderPipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }
}