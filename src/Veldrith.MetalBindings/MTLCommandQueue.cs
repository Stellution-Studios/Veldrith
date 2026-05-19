using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLCommandQueue struct.
/// </summary>
public struct MTLCommandQueue {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    [Pure]

    /// <summary>
    /// Executes the commandBuffer operation.
    /// </summary>
    /// <returns>Returns the result produced by the commandBuffer operation.</returns>
    public MTLCommandBuffer commandBuffer() {
        return objc_msgSend<MTLCommandBuffer>(this.NativePtr, sel_commandBuffer);
    }

    /// <summary>
    /// Executes the insertDebugCaptureBoundary operation.
    /// </summary>
    public void insertDebugCaptureBoundary() {
        objc_msgSend(this.NativePtr, sel_insertDebugCaptureBoundary);
    }

    /// <summary>
    /// Stores the value associated with <c>sel_commandBuffer</c>.
    /// </summary>
    private static readonly Selector sel_commandBuffer = "commandBuffer";

    /// <summary>
    /// Stores the value associated with <c>sel_insertDebugCaptureBoundary</c>.
    /// </summary>
    private static readonly Selector sel_insertDebugCaptureBoundary = "insertDebugCaptureBoundary";
}