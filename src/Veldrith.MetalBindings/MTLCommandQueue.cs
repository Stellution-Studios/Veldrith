using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MTLCommandQueue
    {
        public readonly IntPtr NativePtr;

        [Pure]
        public MTLCommandBuffer commandBuffer() => objc_msgSend<MTLCommandBuffer>(NativePtr, sel_commandBuffer);

        public void insertDebugCaptureBoundary() => objc_msgSend(NativePtr, sel_insertDebugCaptureBoundary);

        private static readonly Selector sel_commandBuffer = "commandBuffer";
        private static readonly Selector sel_insertDebugCaptureBoundary = "insertDebugCaptureBoundary";
    }
}
