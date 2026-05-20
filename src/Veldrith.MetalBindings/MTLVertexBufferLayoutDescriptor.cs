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
    public MTLVertexStepFunction StepFunction {
        get => (MTLVertexStepFunction)uint_objc_msgSend(this.NativePtr, _selStepFunction);
        set => objc_msgSend(this.NativePtr, _selSetStepFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets stride.
    /// </summary>
    public UIntPtr Stride {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selStride);
        set => objc_msgSend(this.NativePtr, _selSetStride, value);
    }

    /// <summary>
    /// Gets or sets stepRate.
    /// </summary>
    public UIntPtr StepRate {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selStepRate);
        set => objc_msgSend(this.NativePtr, _selSetStepRate, value);
    }

    /// <summary>
    /// Stores the sel step function state used by this instance.
    /// </summary>
    private static readonly Selector _selStepFunction = "stepFunction";

    /// <summary>
    /// Stores the sel set step function state used by this instance.
    /// </summary>
    private static readonly Selector _selSetStepFunction = "setStepFunction:";

    /// <summary>
    /// Stores the sel stride state used by this instance.
    /// </summary>
    private static readonly Selector _selStride = "stride";

    /// <summary>
    /// Stores the sel set stride state used by this instance.
    /// </summary>
    private static readonly Selector _selSetStride = "setStride:";

    /// <summary>
    /// Stores the sel step rate state used by this instance.
    /// </summary>
    private static readonly Selector _selStepRate = "stepRate";

    /// <summary>
    /// Stores the sel set step rate state used by this instance.
    /// </summary>
    private static readonly Selector _selSetStepRate = "setStepRate:";
}