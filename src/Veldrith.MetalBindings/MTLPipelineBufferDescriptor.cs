using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLPipelineBufferDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLPipelineBufferDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLPipelineBufferDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLPipelineBufferDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets mutability.
    /// </summary>
    public MTLMutability mutability {
        get => (MTLMutability)uint_objc_msgSend(this.NativePtr, sel_mutability);
        set => objc_msgSend(this.NativePtr, sel_setMutability, (uint)value);
    }

    /// <summary>
    /// Stores the sel mutability state used by this instance.
    /// </summary>
    private static readonly Selector sel_mutability = "mutability";

    /// <summary>
    /// Stores the sel set mutability state used by this instance.
    /// </summary>
    private static readonly Selector sel_setMutability = "setMutability:";
}