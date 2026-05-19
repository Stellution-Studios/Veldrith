using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLVertexBufferLayoutDescriptor struct.
/// </summary>
public struct MTLVertexBufferLayoutDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLVertexBufferLayoutDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLVertexBufferLayoutDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets stepFunction.
    /// </summary>
    public MTLVertexStepFunction stepFunction {
        get => (MTLVertexStepFunction)uint_objc_msgSend(this.NativePtr, sel_stepFunction);
        set => objc_msgSend(this.NativePtr, sel_setStepFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets stride.
    /// </summary>
    public UIntPtr stride {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_stride);
        set => objc_msgSend(this.NativePtr, sel_setStride, value);
    }

    /// <summary>
    /// Gets or sets stepRate.
    /// </summary>
    public UIntPtr stepRate {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_stepRate);
        set => objc_msgSend(this.NativePtr, sel_setStepRate, value);
    }

    /// <summary>
    /// Stores the value associated with <c>sel_stepFunction</c>.
    /// </summary>
    private static readonly Selector sel_stepFunction = "stepFunction";

    /// <summary>
    /// Stores the value associated with <c>sel_setStepFunction</c>.
    /// </summary>
    private static readonly Selector sel_setStepFunction = "setStepFunction:";

    /// <summary>
    /// Stores the value associated with <c>sel_stride</c>.
    /// </summary>
    private static readonly Selector sel_stride = "stride";

    /// <summary>
    /// Stores the value associated with <c>sel_setStride</c>.
    /// </summary>
    private static readonly Selector sel_setStride = "setStride:";

    /// <summary>
    /// Stores the value associated with <c>sel_stepRate</c>.
    /// </summary>
    private static readonly Selector sel_stepRate = "stepRate";

    /// <summary>
    /// Stores the value associated with <c>sel_setStepRate</c>.
    /// </summary>
    private static readonly Selector sel_setStepRate = "setStepRate:";
}