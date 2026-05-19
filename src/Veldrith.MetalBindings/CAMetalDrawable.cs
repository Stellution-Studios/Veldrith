using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]
public struct CAMetalDrawable {
    public readonly IntPtr NativePtr;
    public bool IsNull => this.NativePtr == IntPtr.Zero;
    public MTLTexture texture => objc_msgSend<MTLTexture>(this.NativePtr, Selectors.texture);
}