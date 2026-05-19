using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLComputePipelineDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLComputePipelineDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets computeFunction.
    /// </summary>
    public MTLFunction computeFunction {
        get => objc_msgSend<MTLFunction>(this.NativePtr, sel_computeFunction);
        set => objc_msgSend(this.NativePtr, sel_setComputeFunction, value.NativePtr);
    }

    public MTLPipelineBufferDescriptorArray buffers
        => objc_msgSend<MTLPipelineBufferDescriptorArray>(this.NativePtr, sel_buffers);

    /// <summary>
    /// Stores the sel compute function state used by this instance.
    /// </summary>
    private static readonly Selector sel_computeFunction = "computeFunction";

    /// <summary>
    /// Stores the sel set compute function state used by this instance.
    /// </summary>
    private static readonly Selector sel_setComputeFunction = "setComputeFunction:";

    /// <summary>
    /// Stores the sel buffers collection used by this instance.
    /// </summary>
    private static readonly Selector sel_buffers = "buffers";
}