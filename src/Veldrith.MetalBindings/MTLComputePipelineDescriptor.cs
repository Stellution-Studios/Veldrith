using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLComputePipelineDescriptor struct.
/// </summary>
public struct MTLComputePipelineDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
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
    /// Represents the sel_computeFunction field.
    /// </summary>
    private static readonly Selector sel_computeFunction = "computeFunction";

    /// <summary>
    /// Represents the sel_setComputeFunction field.
    /// </summary>
    private static readonly Selector sel_setComputeFunction = "setComputeFunction:";

    /// <summary>
    /// Represents the sel_buffers field.
    /// </summary>
    private static readonly Selector sel_buffers = "buffers";
}