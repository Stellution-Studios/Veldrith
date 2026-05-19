using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLCommandQueue data structure used by the graphics runtime.
/// </summary>
public struct MTLCommandQueue {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    [Pure]

    /// <summary>
    /// Executes the command buffer logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public MTLCommandBuffer commandBuffer() {
        return objc_msgSend<MTLCommandBuffer>(this.NativePtr, sel_commandBuffer);
    }

    /// <summary>
    /// Executes the insert debug capture boundary logic for this backend.
    /// </summary>
    public void insertDebugCaptureBoundary() {
        objc_msgSend(this.NativePtr, sel_insertDebugCaptureBoundary);
    }

    /// <summary>
    /// Stores the sel command buffer state used by this instance.
    /// </summary>
    private static readonly Selector sel_commandBuffer = "commandBuffer";

    /// <summary>
    /// Stores the sel insert debug capture boundary state used by this instance.
    /// </summary>
    private static readonly Selector sel_insertDebugCaptureBoundary = "insertDebugCaptureBoundary";
}