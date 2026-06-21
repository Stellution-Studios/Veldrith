using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Provides Objective-C interop bindings for MTLTexture.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct MTLTexture {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLTexture" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLTexture(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes the replace region logic for this backend.
    /// </summary>
    /// <param name="region">The region value used by this operation.</param>
    /// <param name="mipmapLevel">The mipmap level value used by this operation.</param>
    /// <param name="slice">The slice value used by this operation.</param>
    /// <param name="pixelBytes">The pixel bytes value used by this operation.</param>
    /// <param name="bytesPerRow">The bytes per row value used by this operation.</param>
    /// <param name="bytesPerImage">The bytes per image value used by this operation.</param>
    public void ReplaceRegion(MTLRegion region, UIntPtr mipmapLevel, UIntPtr slice, void* pixelBytes, UIntPtr bytesPerRow, UIntPtr bytesPerImage) {
        ObjcMsgSend(this.NativePtr, sel_replaceRegion, region, mipmapLevel, slice, (IntPtr)pixelBytes, bytesPerRow, bytesPerImage);
    }

    /// <summary>
    /// Executes the new texture view logic for this backend.
    /// </summary>
    /// <param name="pixelFormat">The pixel format value used by this operation.</param>
    /// <param name="textureType">The texture type value used by this operation.</param>
    /// <param name="levelRange">The level range value used by this operation.</param>
    /// <param name="sliceRange">The slice range value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLTexture NewTextureView(MTLPixelFormat pixelFormat, MTLTextureType textureType, NSRange levelRange, NSRange sliceRange) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newTextureView, (uint)pixelFormat, (uint)textureType, levelRange, sliceRange);
        return new MTLTexture(ret);
    }

    /// <summary>
    /// Stores the sel replace region state used by this instance.
    /// </summary>
    private static readonly Selector sel_replaceRegion = "replaceRegion:mipmapLevel:slice:withBytes:bytesPerRow:bytesPerImage:";

    /// <summary>
    /// Stores the sel new texture view state used by this instance.
    /// </summary>
    private static readonly Selector sel_newTextureView = "newTextureViewWithPixelFormat:textureType:levels:slices:";
}
