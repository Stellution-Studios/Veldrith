using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CAMetalDrawable data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CAMetalDrawable {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Gets or sets texture.
    /// </summary>
    public MTLTexture Texture => ObjcMsgSend<MTLTexture>(this.NativePtr, Selectors.Texture);
}
