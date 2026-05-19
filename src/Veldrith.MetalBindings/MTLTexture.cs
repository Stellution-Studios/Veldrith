using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLTexture struct.
/// </summary>
public unsafe struct MTLTexture {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLTexture" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLTexture(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes the replaceRegion operation.
    /// </summary>
    /// <param name="region">Specifies the value of <paramref name="region" />.</param>
    /// <param name="mipmapLevel">Specifies the value of <paramref name="mipmapLevel" />.</param>
    /// <param name="slice">Specifies the value of <paramref name="slice" />.</param>
    /// <param name="pixelBytes">Specifies the value of <paramref name="pixelBytes" />.</param>
    /// <param name="bytesPerRow">Specifies the value of <paramref name="bytesPerRow" />.</param>
    /// <param name="bytesPerImage">Specifies the value of <paramref name="bytesPerImage" />.</param>
    public void replaceRegion(MTLRegion region, UIntPtr mipmapLevel, UIntPtr slice, void* pixelBytes, UIntPtr bytesPerRow, UIntPtr bytesPerImage) {
        objc_msgSend(this.NativePtr, sel_replaceRegion, region, mipmapLevel, slice, (IntPtr)pixelBytes, bytesPerRow, bytesPerImage);
    }

    /// <summary>
    /// Executes the newTextureView operation.
    /// </summary>
    /// <param name="pixelFormat">Specifies the value of <paramref name="pixelFormat" />.</param>
    /// <param name="textureType">Specifies the value of <paramref name="textureType" />.</param>
    /// <param name="levelRange">Specifies the value of <paramref name="levelRange" />.</param>
    /// <param name="sliceRange">Specifies the value of <paramref name="sliceRange" />.</param>
    /// <returns>Returns the result produced by the newTextureView operation.</returns>
    public MTLTexture newTextureView(MTLPixelFormat pixelFormat, MTLTextureType textureType, NSRange levelRange, NSRange sliceRange) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newTextureView, (uint)pixelFormat, (uint)textureType, levelRange, sliceRange);
        return new MTLTexture(ret);
    }

    /// <summary>
    /// Stores the value associated with <c>sel_replaceRegion</c>.
    /// </summary>
    private static readonly Selector sel_replaceRegion = "replaceRegion:mipmapLevel:slice:withBytes:bytesPerRow:bytesPerImage:";

    /// <summary>
    /// Stores the value associated with <c>sel_newTextureView</c>.
    /// </summary>
    private static readonly Selector sel_newTextureView = "newTextureViewWithPixelFormat:textureType:levels:slices:";
}