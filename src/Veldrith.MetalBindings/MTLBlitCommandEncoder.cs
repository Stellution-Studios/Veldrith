using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLBlitCommandEncoder data structure used by the graphics runtime.
/// </summary>
public struct MTLBlitCommandEncoder {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Copies value data between resources.
    /// </summary>
    /// <param name="sourceBuffer">The source buffer value used by this operation.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destinationBuffer">The destination buffer value used by this operation.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    public void copy(MTLBuffer sourceBuffer, UIntPtr sourceOffset, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr size) {
        objc_msgSend(this.NativePtr, sel_copyFromBuffer0, sourceBuffer, sourceOffset, destinationBuffer, destinationOffset, size);
    }

    /// <summary>
    /// Copies from buffer data between resources.
    /// </summary>
    /// <param name="sourceBuffer">The source buffer value used by this operation.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="sourceBytesPerRow">The source bytes per row value used by this operation.</param>
    /// <param name="sourceBytesPerImage">The source bytes per image value used by this operation.</param>
    /// <param name="sourceSize">The source size value used by this operation.</param>
    /// <param name="destinationTexture">The destination texture value used by this operation.</param>
    /// <param name="destinationSlice">The destination slice value used by this operation.</param>
    /// <param name="destinationLevel">The destination level value used by this operation.</param>
    /// <param name="destinationOrigin">The destination origin value used by this operation.</param>
    /// <param name="isMacOS">The is mac os value used by this operation.</param>
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
    /// Copies from buffer i os data between resources.
    /// </summary>
    /// <param name="encoder">The encoder value used by this operation.</param>
    /// <param name="sourceBuffer">The source buffer value used by this operation.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="sourceBytesPerRow">The source bytes per row value used by this operation.</param>
    /// <param name="sourceBytesPerImage">The source bytes per image value used by this operation.</param>
    /// <param name="sourceSize">The source size value used by this operation.</param>
    /// <param name="destinationTexture">The destination texture value used by this operation.</param>
    /// <param name="destinationSlice">The destination slice value used by this operation.</param>
    /// <param name="destinationLevel">The destination level value used by this operation.</param>
    /// <param name="destinationOriginX">The destination origin x value used by this operation.</param>
    /// <param name="destinationOriginY">The destination origin y value used by this operation.</param>
    /// <param name="destinationOriginZ">The destination origin z value used by this operation.</param>
    private static extern void copyFromBuffer_iOS(IntPtr encoder, IntPtr sourceBuffer, UIntPtr sourceOffset, UIntPtr sourceBytesPerRow, UIntPtr sourceBytesPerImage, MTLSize sourceSize, IntPtr destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, UIntPtr destinationOriginX, UIntPtr destinationOriginY, UIntPtr destinationOriginZ);

    /// <summary>
    /// Copies texture to buffer data between resources.
    /// </summary>
    /// <param name="sourceTexture">The source texture value used by this operation.</param>
    /// <param name="sourceSlice">The source slice value used by this operation.</param>
    /// <param name="sourceLevel">The source level value used by this operation.</param>
    /// <param name="sourceOrigin">The source origin value used by this operation.</param>
    /// <param name="sourceSize">The source size value used by this operation.</param>
    /// <param name="destinationBuffer">The destination buffer value used by this operation.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="destinationBytesPerRow">The destination bytes per row value used by this operation.</param>
    /// <param name="destinationBytesPerImage">The destination bytes per image value used by this operation.</param>
    public void copyTextureToBuffer(MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr destinationBytesPerRow, UIntPtr destinationBytesPerImage) {
        objc_msgSend(this.NativePtr, sel_copyFromTexture0, sourceTexture, sourceSlice, sourceLevel, sourceOrigin, sourceSize, destinationBuffer, destinationOffset, destinationBytesPerRow, destinationBytesPerImage);
    }

    /// <summary>
    /// Executes the generate mipmaps for texture logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    public void generateMipmapsForTexture(MTLTexture texture) {
        objc_msgSend(this.NativePtr, sel_generateMipmapsForTexture, texture.NativePtr);
    }

    /// <summary>
    /// Executes the synchronize resource logic for this backend.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    public void synchronizeResource(IntPtr resource) {
        objc_msgSend(this.NativePtr, sel_synchronizeResource, resource);
    }

    /// <summary>
    /// Ends the encoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Executes the push debug group logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes the pop debug group logic for this backend.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Executes the insert debug signpost logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }

    /// <summary>
    /// Copies from texture data between resources.
    /// </summary>
    /// <param name="sourceTexture">The source texture value used by this operation.</param>
    /// <param name="sourceSlice">The source slice value used by this operation.</param>
    /// <param name="sourceLevel">The source level value used by this operation.</param>
    /// <param name="sourceOrigin">The source origin value used by this operation.</param>
    /// <param name="sourceSize">The source size value used by this operation.</param>
    /// <param name="destinationTexture">The destination texture value used by this operation.</param>
    /// <param name="destinationSlice">The destination slice value used by this operation.</param>
    /// <param name="destinationLevel">The destination level value used by this operation.</param>
    /// <param name="destinationOrigin">The destination origin value used by this operation.</param>
    /// <param name="isMacOS">The is mac os value used by this operation.</param>
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
    /// Copies from texture i os data between resources.
    /// </summary>
    /// <param name="encoder">The encoder value used by this operation.</param>
    /// <param name="sourceTexture">The source texture value used by this operation.</param>
    /// <param name="sourceSlice">The source slice value used by this operation.</param>
    /// <param name="sourceLevel">The source level value used by this operation.</param>
    /// <param name="sourceOrigin">The source origin value used by this operation.</param>
    /// <param name="sourceSize">The source size value used by this operation.</param>
    /// <param name="destinationTexture">The destination texture value used by this operation.</param>
    /// <param name="destinationSlice">The destination slice value used by this operation.</param>
    /// <param name="destinationLevel">The destination level value used by this operation.</param>
    /// <param name="destinationOriginX">The destination origin x value used by this operation.</param>
    /// <param name="destinationOriginY">The destination origin y value used by this operation.</param>
    /// <param name="destinationOriginZ">The destination origin z value used by this operation.</param>
    private static extern void copyFromTexture_iOS(IntPtr encoder, IntPtr sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, IntPtr destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, UIntPtr destinationOriginX, UIntPtr destinationOriginY, UIntPtr destinationOriginZ);

    /// <summary>
    /// Stores the sel copy from buffer0 state used by this instance.
    /// </summary>
    private static readonly Selector sel_copyFromBuffer0 = "copyFromBuffer:sourceOffset:toBuffer:destinationOffset:size:";

    /// <summary>
    /// Stores the sel copy from buffer1 state used by this instance.
    /// </summary>
    private static readonly Selector sel_copyFromBuffer1 = "copyFromBuffer:sourceOffset:sourceBytesPerRow:sourceBytesPerImage:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:";

    /// <summary>
    /// Stores the sel copy from texture0 state used by this instance.
    /// </summary>
    private static readonly Selector sel_copyFromTexture0 = "copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toBuffer:destinationOffset:destinationBytesPerRow:destinationBytesPerImage:";

    /// <summary>
    /// Stores the sel copy from texture1 state used by this instance.
    /// </summary>
    private static readonly Selector sel_copyFromTexture1 = "copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:";

    /// <summary>
    /// Stores the sel generate mipmaps for texture state used by this instance.
    /// </summary>
    private static readonly Selector sel_generateMipmapsForTexture = "generateMipmapsForTexture:";

    /// <summary>
    /// Stores the sel synchronize resource state used by this instance.
    /// </summary>
    private static readonly Selector sel_synchronizeResource = "synchronizeResource:";

    /// <summary>
    /// Stores the sel end encoding state used by this instance.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";
}