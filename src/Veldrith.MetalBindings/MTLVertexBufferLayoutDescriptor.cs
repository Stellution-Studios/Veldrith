using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLVertexBufferLayoutDescriptor struct.
/// </summary>
public struct MTLVertexBufferLayoutDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLVertexBufferLayoutDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
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
    /// Represents the sel_stepFunction field.
    /// </summary>
    private static readonly Selector sel_stepFunction = "stepFunction";

    /// <summary>
    /// Represents the sel_setStepFunction field.
    /// </summary>
    private static readonly Selector sel_setStepFunction = "setStepFunction:";

    /// <summary>
    /// Represents the sel_stride field.
    /// </summary>
    private static readonly Selector sel_stride = "stride";

    /// <summary>
    /// Represents the sel_setStride field.
    /// </summary>
    private static readonly Selector sel_setStride = "setStride:";

    /// <summary>
    /// Represents the sel_stepRate field.
    /// </summary>
    private static readonly Selector sel_stepRate = "stepRate";

    /// <summary>
    /// Represents the sel_setStepRate field.
    /// </summary>
    private static readonly Selector sel_setStepRate = "setStepRate:";
}