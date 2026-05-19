using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLBlitCommandEncoder struct.
/// </summary>
public struct MTLBlitCommandEncoder {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Performs the copy operation.
    /// </summary>
    /// <param name="sourceBuffer">The value of sourceBuffer.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="destinationBuffer">The value of destinationBuffer.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="size">The value of size.</param>
    public void copy(MTLBuffer sourceBuffer, UIntPtr sourceOffset, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr size) {
        objc_msgSend(this.NativePtr, sel_copyFromBuffer0, sourceBuffer, sourceOffset, destinationBuffer, destinationOffset, size);
    }

    /// <summary>
    /// Performs the copyFromBuffer operation.
    /// </summary>
    /// <param name="sourceBuffer">The value of sourceBuffer.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="sourceBytesPerRow">The value of sourceBytesPerRow.</param>
    /// <param name="sourceBytesPerImage">The value of sourceBytesPerImage.</param>
    /// <param name="sourceSize">The value of sourceSize.</param>
    /// <param name="destinationTexture">The value of destinationTexture.</param>
    /// <param name="destinationSlice">The value of destinationSlice.</param>
    /// <param name="destinationLevel">The value of destinationLevel.</param>
    /// <param name="destinationOrigin">The value of destinationOrigin.</param>
    /// <param name="isMacOS">The value of isMacOS.</param>
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
    /// Performs the copyFromBuffer_iOS operation.
    /// </summary>
    /// <param name="encoder">The value of encoder.</param>
    /// <param name="sourceBuffer">The value of sourceBuffer.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="sourceBytesPerRow">The value of sourceBytesPerRow.</param>
    /// <param name="sourceBytesPerImage">The value of sourceBytesPerImage.</param>
    /// <param name="sourceSize">The value of sourceSize.</param>
    /// <param name="destinationTexture">The value of destinationTexture.</param>
    /// <param name="destinationSlice">The value of destinationSlice.</param>
    /// <param name="destinationLevel">The value of destinationLevel.</param>
    /// <param name="destinationOriginX">The value of destinationOriginX.</param>
    /// <param name="destinationOriginY">The value of destinationOriginY.</param>
    /// <param name="destinationOriginZ">The value of destinationOriginZ.</param>
    private static extern void copyFromBuffer_iOS(IntPtr encoder, IntPtr sourceBuffer, UIntPtr sourceOffset, UIntPtr sourceBytesPerRow, UIntPtr sourceBytesPerImage, MTLSize sourceSize, IntPtr destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, UIntPtr destinationOriginX, UIntPtr destinationOriginY, UIntPtr destinationOriginZ);

    /// <summary>
    /// Performs the copyTextureToBuffer operation.
    /// </summary>
    /// <param name="sourceTexture">The value of sourceTexture.</param>
    /// <param name="sourceSlice">The value of sourceSlice.</param>
    /// <param name="sourceLevel">The value of sourceLevel.</param>
    /// <param name="sourceOrigin">The value of sourceOrigin.</param>
    /// <param name="sourceSize">The value of sourceSize.</param>
    /// <param name="destinationBuffer">The value of destinationBuffer.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="destinationBytesPerRow">The value of destinationBytesPerRow.</param>
    /// <param name="destinationBytesPerImage">The value of destinationBytesPerImage.</param>
    public void copyTextureToBuffer(MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr destinationBytesPerRow, UIntPtr destinationBytesPerImage) {
        objc_msgSend(this.NativePtr, sel_copyFromTexture0, sourceTexture, sourceSlice, sourceLevel, sourceOrigin, sourceSize, destinationBuffer, destinationOffset, destinationBytesPerRow, destinationBytesPerImage);
    }

    /// <summary>
    /// Performs the generateMipmapsForTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    public void generateMipmapsForTexture(MTLTexture texture) {
        objc_msgSend(this.NativePtr, sel_generateMipmapsForTexture, texture.NativePtr);
    }

    /// <summary>
    /// Performs the synchronizeResource operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    public void synchronizeResource(IntPtr resource) {
        objc_msgSend(this.NativePtr, sel_synchronizeResource, resource);
    }

    /// <summary>
    /// Performs the endEncoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Performs the pushDebugGroup operation.
    /// </summary>
    /// <param name="string">The value of string.</param>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Performs the popDebugGroup operation.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Performs the insertDebugSignpost operation.
    /// </summary>
    /// <param name="string">The value of string.</param>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }

    /// <summary>
    /// Performs the copyFromTexture operation.
    /// </summary>
    /// <param name="sourceTexture">The value of sourceTexture.</param>
    /// <param name="sourceSlice">The value of sourceSlice.</param>
    /// <param name="sourceLevel">The value of sourceLevel.</param>
    /// <param name="sourceOrigin">The value of sourceOrigin.</param>
    /// <param name="sourceSize">The value of sourceSize.</param>
    /// <param name="destinationTexture">The value of destinationTexture.</param>
    /// <param name="destinationSlice">The value of destinationSlice.</param>
    /// <param name="destinationLevel">The value of destinationLevel.</param>
    /// <param name="destinationOrigin">The value of destinationOrigin.</param>
    /// <param name="isMacOS">The value of isMacOS.</param>
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
    /// Performs the copyFromTexture_iOS operation.
    /// </summary>
    /// <param name="encoder">The value of encoder.</param>
    /// <param name="sourceTexture">The value of sourceTexture.</param>
    /// <param name="sourceSlice">The value of sourceSlice.</param>
    /// <param name="sourceLevel">The value of sourceLevel.</param>
    /// <param name="sourceOrigin">The value of sourceOrigin.</param>
    /// <param name="sourceSize">The value of sourceSize.</param>
    /// <param name="destinationTexture">The value of destinationTexture.</param>
    /// <param name="destinationSlice">The value of destinationSlice.</param>
    /// <param name="destinationLevel">The value of destinationLevel.</param>
    /// <param name="destinationOriginX">The value of destinationOriginX.</param>
    /// <param name="destinationOriginY">The value of destinationOriginY.</param>
    /// <param name="destinationOriginZ">The value of destinationOriginZ.</param>
    private static extern void copyFromTexture_iOS(IntPtr encoder, IntPtr sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, IntPtr destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, UIntPtr destinationOriginX, UIntPtr destinationOriginY, UIntPtr destinationOriginZ);

    /// <summary>
    /// Represents the sel_copyFromBuffer0 field.
    /// </summary>
    private static readonly Selector sel_copyFromBuffer0 = "copyFromBuffer:sourceOffset:toBuffer:destinationOffset:size:";

    /// <summary>
    /// Represents the sel_copyFromBuffer1 field.
    /// </summary>
    private static readonly Selector sel_copyFromBuffer1 = "copyFromBuffer:sourceOffset:sourceBytesPerRow:sourceBytesPerImage:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:";

    /// <summary>
    /// Represents the sel_copyFromTexture0 field.
    /// </summary>
    private static readonly Selector sel_copyFromTexture0 = "copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toBuffer:destinationOffset:destinationBytesPerRow:destinationBytesPerImage:";

    /// <summary>
    /// Represents the sel_copyFromTexture1 field.
    /// </summary>
    private static readonly Selector sel_copyFromTexture1 = "copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:";

    /// <summary>
    /// Represents the sel_generateMipmapsForTexture field.
    /// </summary>
    private static readonly Selector sel_generateMipmapsForTexture = "generateMipmapsForTexture:";

    /// <summary>
    /// Represents the sel_synchronizeResource field.
    /// </summary>
    private static readonly Selector sel_synchronizeResource = "synchronizeResource:";

    /// <summary>
    /// Represents the sel_endEncoding field.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";
}