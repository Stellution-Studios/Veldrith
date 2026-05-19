using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLBlitCommandEncoder struct.
/// </summary>
public struct MTLBlitCommandEncoder {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes the copy operation.
    /// </summary>
    /// <param name="sourceBuffer">Specifies the value of <paramref name="sourceBuffer" />.</param>
    /// <param name="sourceOffset">Specifies the value of <paramref name="sourceOffset" />.</param>
    /// <param name="destinationBuffer">Specifies the value of <paramref name="destinationBuffer" />.</param>
    /// <param name="destinationOffset">Specifies the value of <paramref name="destinationOffset" />.</param>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
    public void copy(MTLBuffer sourceBuffer, UIntPtr sourceOffset, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr size) {
        objc_msgSend(this.NativePtr, sel_copyFromBuffer0, sourceBuffer, sourceOffset, destinationBuffer, destinationOffset, size);
    }

    /// <summary>
    /// Executes the copyFromBuffer operation.
    /// </summary>
    /// <param name="sourceBuffer">Specifies the value of <paramref name="sourceBuffer" />.</param>
    /// <param name="sourceOffset">Specifies the value of <paramref name="sourceOffset" />.</param>
    /// <param name="sourceBytesPerRow">Specifies the value of <paramref name="sourceBytesPerRow" />.</param>
    /// <param name="sourceBytesPerImage">Specifies the value of <paramref name="sourceBytesPerImage" />.</param>
    /// <param name="sourceSize">Specifies the value of <paramref name="sourceSize" />.</param>
    /// <param name="destinationTexture">Specifies the value of <paramref name="destinationTexture" />.</param>
    /// <param name="destinationSlice">Specifies the value of <paramref name="destinationSlice" />.</param>
    /// <param name="destinationLevel">Specifies the value of <paramref name="destinationLevel" />.</param>
    /// <param name="destinationOrigin">Specifies the value of <paramref name="destinationOrigin" />.</param>
    /// <param name="isMacOS">Specifies the value of <paramref name="isMacOS" />.</param>
    public void copyFromBuffer(MTLBuffer sourceBuffer, UIntPtr sourceOffset, UIntPtr sourceBytesPerRow, UIntPtr sourceBytesPerImage, MTLSize sourceSize, MTLTexture destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, MTLOrigin destinationOrigin, bool isMacOS) {
        if (!isMacOS) {
            copyFromBuffer_iOS(this.NativePtr, sourceBuffer.NativePtr, sourceOffset, sourceBytesPerRow, sourceBytesPerImage, sourceSize, destinationTexture.NativePtr, destinationSlice, destinationLevel, destinationOrigin.x, destinationOrigin.y, destinationOrigin.z);
        }
        else {
            objc_msgSend(this.NativePtr, sel_copyFromBuffer1, sourceBuffer.NativePtr, sourceOffset, sourceBytesPerRow, sourceBytesPerImage, sourceSize, destinationTexture.NativePtr, destinationSlice, destinationLevel, destinationOrigin);
        }
    }

    [DllImport("@rpath/metal_mono_workaround.framework/metal_mono_workaround", EntryPoint = "copyFromBuffer")]

    /// <summary>
    /// Executes the copyFromBuffer_iOS operation.
    /// </summary>
    /// <param name="encoder">Specifies the value of <paramref name="encoder" />.</param>
    /// <param name="sourceBuffer">Specifies the value of <paramref name="sourceBuffer" />.</param>
    /// <param name="sourceOffset">Specifies the value of <paramref name="sourceOffset" />.</param>
    /// <param name="sourceBytesPerRow">Specifies the value of <paramref name="sourceBytesPerRow" />.</param>
    /// <param name="sourceBytesPerImage">Specifies the value of <paramref name="sourceBytesPerImage" />.</param>
    /// <param name="sourceSize">Specifies the value of <paramref name="sourceSize" />.</param>
    /// <param name="destinationTexture">Specifies the value of <paramref name="destinationTexture" />.</param>
    /// <param name="destinationSlice">Specifies the value of <paramref name="destinationSlice" />.</param>
    /// <param name="destinationLevel">Specifies the value of <paramref name="destinationLevel" />.</param>
    /// <param name="destinationOriginX">Specifies the value of <paramref name="destinationOriginX" />.</param>
    /// <param name="destinationOriginY">Specifies the value of <paramref name="destinationOriginY" />.</param>
    /// <param name="destinationOriginZ">Specifies the value of <paramref name="destinationOriginZ" />.</param>
    private static extern void copyFromBuffer_iOS(IntPtr encoder, IntPtr sourceBuffer, UIntPtr sourceOffset, UIntPtr sourceBytesPerRow, UIntPtr sourceBytesPerImage, MTLSize sourceSize, IntPtr destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, UIntPtr destinationOriginX, UIntPtr destinationOriginY, UIntPtr destinationOriginZ);

    /// <summary>
    /// Executes the copyTextureToBuffer operation.
    /// </summary>
    /// <param name="sourceTexture">Specifies the value of <paramref name="sourceTexture" />.</param>
    /// <param name="sourceSlice">Specifies the value of <paramref name="sourceSlice" />.</param>
    /// <param name="sourceLevel">Specifies the value of <paramref name="sourceLevel" />.</param>
    /// <param name="sourceOrigin">Specifies the value of <paramref name="sourceOrigin" />.</param>
    /// <param name="sourceSize">Specifies the value of <paramref name="sourceSize" />.</param>
    /// <param name="destinationBuffer">Specifies the value of <paramref name="destinationBuffer" />.</param>
    /// <param name="destinationOffset">Specifies the value of <paramref name="destinationOffset" />.</param>
    /// <param name="destinationBytesPerRow">Specifies the value of <paramref name="destinationBytesPerRow" />.</param>
    /// <param name="destinationBytesPerImage">Specifies the value of <paramref name="destinationBytesPerImage" />.</param>
    public void copyTextureToBuffer(MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr destinationBytesPerRow, UIntPtr destinationBytesPerImage) {
        objc_msgSend(this.NativePtr, sel_copyFromTexture0, sourceTexture, sourceSlice, sourceLevel, sourceOrigin, sourceSize, destinationBuffer, destinationOffset, destinationBytesPerRow, destinationBytesPerImage);
    }

    /// <summary>
    /// Executes the generateMipmapsForTexture operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    public void generateMipmapsForTexture(MTLTexture texture) {
        objc_msgSend(this.NativePtr, sel_generateMipmapsForTexture, texture.NativePtr);
    }

    /// <summary>
    /// Executes the synchronizeResource operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    public void synchronizeResource(IntPtr resource) {
        objc_msgSend(this.NativePtr, sel_synchronizeResource, resource);
    }

    /// <summary>
    /// Executes the endEncoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Executes the pushDebugGroup operation.
    /// </summary>
    /// <param name="string">Specifies the value of <paramref name="string" />.</param>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes the popDebugGroup operation.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Executes the insertDebugSignpost operation.
    /// </summary>
    /// <param name="string">Specifies the value of <paramref name="string" />.</param>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }

    /// <summary>
    /// Executes the copyFromTexture operation.
    /// </summary>
    /// <param name="sourceTexture">Specifies the value of <paramref name="sourceTexture" />.</param>
    /// <param name="sourceSlice">Specifies the value of <paramref name="sourceSlice" />.</param>
    /// <param name="sourceLevel">Specifies the value of <paramref name="sourceLevel" />.</param>
    /// <param name="sourceOrigin">Specifies the value of <paramref name="sourceOrigin" />.</param>
    /// <param name="sourceSize">Specifies the value of <paramref name="sourceSize" />.</param>
    /// <param name="destinationTexture">Specifies the value of <paramref name="destinationTexture" />.</param>
    /// <param name="destinationSlice">Specifies the value of <paramref name="destinationSlice" />.</param>
    /// <param name="destinationLevel">Specifies the value of <paramref name="destinationLevel" />.</param>
    /// <param name="destinationOrigin">Specifies the value of <paramref name="destinationOrigin" />.</param>
    /// <param name="isMacOS">Specifies the value of <paramref name="isMacOS" />.</param>
    public void copyFromTexture(MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLTexture destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, MTLOrigin destinationOrigin, bool isMacOS) {
        if (!isMacOS) {
            copyFromTexture_iOS(this.NativePtr, sourceTexture.NativePtr, sourceSlice, sourceLevel, sourceOrigin, sourceSize, destinationTexture.NativePtr, destinationSlice, destinationLevel, destinationOrigin.x, destinationOrigin.y, destinationOrigin.z);
        }
        else {
            objc_msgSend(this.NativePtr, sel_copyFromTexture1, sourceTexture, sourceSlice, sourceLevel, sourceOrigin, sourceSize, destinationTexture, destinationSlice, destinationLevel, destinationOrigin);
        }
    }

    [DllImport("@rpath/metal_mono_workaround.framework/metal_mono_workaround", EntryPoint = "copyFromTexture")]

    /// <summary>
    /// Executes the copyFromTexture_iOS operation.
    /// </summary>
    /// <param name="encoder">Specifies the value of <paramref name="encoder" />.</param>
    /// <param name="sourceTexture">Specifies the value of <paramref name="sourceTexture" />.</param>
    /// <param name="sourceSlice">Specifies the value of <paramref name="sourceSlice" />.</param>
    /// <param name="sourceLevel">Specifies the value of <paramref name="sourceLevel" />.</param>
    /// <param name="sourceOrigin">Specifies the value of <paramref name="sourceOrigin" />.</param>
    /// <param name="sourceSize">Specifies the value of <paramref name="sourceSize" />.</param>
    /// <param name="destinationTexture">Specifies the value of <paramref name="destinationTexture" />.</param>
    /// <param name="destinationSlice">Specifies the value of <paramref name="destinationSlice" />.</param>
    /// <param name="destinationLevel">Specifies the value of <paramref name="destinationLevel" />.</param>
    /// <param name="destinationOriginX">Specifies the value of <paramref name="destinationOriginX" />.</param>
    /// <param name="destinationOriginY">Specifies the value of <paramref name="destinationOriginY" />.</param>
    /// <param name="destinationOriginZ">Specifies the value of <paramref name="destinationOriginZ" />.</param>
    private static extern void copyFromTexture_iOS(IntPtr encoder, IntPtr sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, IntPtr destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, UIntPtr destinationOriginX, UIntPtr destinationOriginY, UIntPtr destinationOriginZ);

    /// <summary>
    /// Stores the value associated with <c>sel_copyFromBuffer0</c>.
    /// </summary>
    private static readonly Selector sel_copyFromBuffer0 = "copyFromBuffer:sourceOffset:toBuffer:destinationOffset:size:";

    /// <summary>
    /// Stores the value associated with <c>sel_copyFromBuffer1</c>.
    /// </summary>
    private static readonly Selector sel_copyFromBuffer1 = "copyFromBuffer:sourceOffset:sourceBytesPerRow:sourceBytesPerImage:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:";

    /// <summary>
    /// Stores the value associated with <c>sel_copyFromTexture0</c>.
    /// </summary>
    private static readonly Selector sel_copyFromTexture0 = "copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toBuffer:destinationOffset:destinationBytesPerRow:destinationBytesPerImage:";

    /// <summary>
    /// Stores the value associated with <c>sel_copyFromTexture1</c>.
    /// </summary>
    private static readonly Selector sel_copyFromTexture1 = "copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:";

    /// <summary>
    /// Stores the value associated with <c>sel_generateMipmapsForTexture</c>.
    /// </summary>
    private static readonly Selector sel_generateMipmapsForTexture = "generateMipmapsForTexture:";

    /// <summary>
    /// Stores the value associated with <c>sel_synchronizeResource</c>.
    /// </summary>
    private static readonly Selector sel_synchronizeResource = "synchronizeResource:";

    /// <summary>
    /// Stores the value associated with <c>sel_endEncoding</c>.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";
}