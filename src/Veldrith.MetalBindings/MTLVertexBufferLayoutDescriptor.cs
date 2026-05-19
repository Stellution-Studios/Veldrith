using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLVertexBufferLayoutDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLVertexBufferLayoutDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLVertexBufferLayoutDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
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
    /// Stores the sel step function state used by this instance.
    /// </summary>
    private static readonly Selector sel_stepFunction = "stepFunction";

    /// <summary>
    /// Stores the sel set step function state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStepFunction = "setStepFunction:";

    /// <summary>
    /// Stores the sel stride state used by this instance.
    /// </summary>
    private static readonly Selector sel_stride = "stride";

    /// <summary>
    /// Stores the sel set stride state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStride = "setStride:";

    /// <summary>
    /// Stores the sel step rate state used by this instance.
    /// </summary>
    private static readonly Selector sel_stepRate = "stepRate";

    /// <summary>
    /// Stores the sel set step rate state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStepRate = "setStepRate:";
}