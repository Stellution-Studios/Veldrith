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
    public MTLFunction ComputeFunction {
        get => ObjcMsgSend<MTLFunction>(this.NativePtr, _selComputeFunction);
        set => ObjcMsgSend(this.NativePtr, _selSetComputeFunction, value.NativePtr);
    }

    public MTLPipelineBufferDescriptorArray Buffers => ObjcMsgSend<MTLPipelineBufferDescriptorArray>(this.NativePtr, _selBuffers);

    /// <summary>
    /// Stores the sel compute function state used by this instance.
    /// </summary>
    private static readonly Selector _selComputeFunction = "computeFunction";

    /// <summary>
    /// Stores the sel set compute function state used by this instance.
    /// </summary>
    private static readonly Selector _selSetComputeFunction = "setComputeFunction:";

    /// <summary>
    /// Stores the sel buffers collection used by this instance.
    /// </summary>
    private static readonly Selector _selBuffers = "buffers";
}
