using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLRenderPipelineState struct.
/// </summary>
public struct MTLRenderPipelineState {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineState" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLRenderPipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }
}