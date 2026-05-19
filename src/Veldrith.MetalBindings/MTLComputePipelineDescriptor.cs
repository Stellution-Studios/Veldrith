using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLComputePipelineDescriptor struct.
/// </summary>
public struct MTLComputePipelineDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
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
    /// Stores the value associated with <c>sel_computeFunction</c>.
    /// </summary>
    private static readonly Selector sel_computeFunction = "computeFunction";

    /// <summary>
    /// Stores the value associated with <c>sel_setComputeFunction</c>.
    /// </summary>
    private static readonly Selector sel_setComputeFunction = "setComputeFunction:";

    /// <summary>
    /// Stores the value associated with <c>sel_buffers</c>.
    /// </summary>
    private static readonly Selector sel_buffers = "buffers";
}