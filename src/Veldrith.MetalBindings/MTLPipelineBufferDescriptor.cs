using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLPipelineBufferDescriptor struct.
/// </summary>
public struct MTLPipelineBufferDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLPipelineBufferDescriptor" /> class.
    /// </summary>
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
    /// Represents the sel_mutability field.
    /// </summary>
    private static readonly Selector sel_mutability = "mutability";

    /// <summary>
    /// Represents the sel_setMutability field.
    /// </summary>
    private static readonly Selector sel_setMutability = "setMutability:";
}