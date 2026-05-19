using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLTexture struct.
/// </summary>
public unsafe struct MTLTexture {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLTexture" /> class.
    /// </summary>
    public MTLTexture(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes replaceRegion.
    /// </summary>
    public void replaceRegion(MTLRegion region, UIntPtr mipmapLevel, UIntPtr slice, void* pixelBytes, UIntPtr bytesPerRow, UIntPtr bytesPerImage) {
        objc_msgSend(this.NativePtr, sel_replaceRegion, region, mipmapLevel, slice, (IntPtr)pixelBytes, bytesPerRow, bytesPerImage);
    }

    /// <summary>
    /// Executes newTextureView.
    /// </summary>
    public MTLTexture newTextureView(MTLPixelFormat pixelFormat, MTLTextureType textureType, NSRange levelRange, NSRange sliceRange) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newTextureView, (uint)pixelFormat, (uint)textureType, levelRange, sliceRange);
        return new MTLTexture(ret);
    }

    /// <summary>
    /// Represents the sel_replaceRegion field.
    /// </summary>
    private static readonly Selector sel_replaceRegion = "replaceRegion:mipmapLevel:slice:withBytes:bytesPerRow:bytesPerImage:";

    /// <summary>
    /// Represents the sel_newTextureView field.
    /// </summary>
    private static readonly Selector sel_newTextureView = "newTextureViewWithPixelFormat:textureType:levels:slices:";
}