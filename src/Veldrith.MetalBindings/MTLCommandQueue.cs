using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLCommandQueue struct.
/// </summary>
public struct MTLCommandQueue {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    [Pure]

    /// <summary>
    /// Performs the commandBuffer operation.
    /// </summary>
    /// <returns>The result of the commandBuffer operation.</returns>
    public MTLCommandBuffer commandBuffer() {
        return objc_msgSend<MTLCommandBuffer>(this.NativePtr, sel_commandBuffer);
    }

    /// <summary>
    /// Performs the insertDebugCaptureBoundary operation.
    /// </summary>
    public void insertDebugCaptureBoundary() {
        objc_msgSend(this.NativePtr, sel_insertDebugCaptureBoundary);
    }

    /// <summary>
    /// Represents the sel_commandBuffer field.
    /// </summary>
    private static readonly Selector sel_commandBuffer = "commandBuffer";

    /// <summary>
    /// Represents the sel_insertDebugCaptureBoundary field.
    /// </summary>
    private static readonly Selector sel_insertDebugCaptureBoundary = "insertDebugCaptureBoundary";
}