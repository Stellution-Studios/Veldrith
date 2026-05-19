using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLPipelineBufferDescriptor struct.
/// </summary>
public struct MTLPipelineBufferDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLPipelineBufferDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
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
    /// Stores the value associated with <c>sel_mutability</c>.
    /// </summary>
    private static readonly Selector sel_mutability = "mutability";

    /// <summary>
    /// Stores the value associated with <c>sel_setMutability</c>.
    /// </summary>
    private static readonly Selector sel_setMutability = "setMutability:";
}