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
    /// Initializes a new instance of the <see cref="MTLTexture" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLTexture(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Performs the replaceRegion operation.
    /// </summary>
    /// <param name="region">The value of region.</param>
    /// <param name="mipmapLevel">The value of mipmapLevel.</param>
    /// <param name="slice">The value of slice.</param>
    /// <param name="pixelBytes">The value of pixelBytes.</param>
    /// <param name="bytesPerRow">The value of bytesPerRow.</param>
    /// <param name="bytesPerImage">The value of bytesPerImage.</param>
    public void replaceRegion(MTLRegion region, UIntPtr mipmapLevel, UIntPtr slice, void* pixelBytes, UIntPtr bytesPerRow, UIntPtr bytesPerImage) {
        objc_msgSend(this.NativePtr, sel_replaceRegion, region, mipmapLevel, slice, (IntPtr)pixelBytes, bytesPerRow, bytesPerImage);
    }

    /// <summary>
    /// Performs the newTextureView operation.
    /// </summary>
    /// <param name="pixelFormat">The value of pixelFormat.</param>
    /// <param name="textureType">The value of textureType.</param>
    /// <param name="levelRange">The value of levelRange.</param>
    /// <param name="sliceRange">The value of sliceRange.</param>
    /// <returns>The result of the newTextureView operation.</returns>
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