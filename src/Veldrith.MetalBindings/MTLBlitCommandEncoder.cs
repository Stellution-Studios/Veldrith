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
    /// Executes copy.
    /// </summary>
    public void copy(MTLBuffer sourceBuffer, UIntPtr sourceOffset, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr size) {
        objc_msgSend(this.NativePtr, sel_copyFromBuffer0, sourceBuffer, sourceOffset, destinationBuffer, destinationOffset, size);
    }

    /// <summary>
    /// Executes copyFromBuffer.
    /// </summary>
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
    /// Executes copyFromBuffer_iOS.
    /// </summary>
    private static extern void copyFromBuffer_iOS(IntPtr encoder, IntPtr sourceBuffer, UIntPtr sourceOffset, UIntPtr sourceBytesPerRow, UIntPtr sourceBytesPerImage, MTLSize sourceSize, IntPtr destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, UIntPtr destinationOriginX, UIntPtr destinationOriginY, UIntPtr destinationOriginZ);

    /// <summary>
    /// Executes copyTextureToBuffer.
    /// </summary>
    public void copyTextureToBuffer(MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLBuffer destinationBuffer, UIntPtr destinationOffset, UIntPtr destinationBytesPerRow, UIntPtr destinationBytesPerImage) {
        objc_msgSend(this.NativePtr, sel_copyFromTexture0, sourceTexture, sourceSlice, sourceLevel, sourceOrigin, sourceSize, destinationBuffer, destinationOffset, destinationBytesPerRow, destinationBytesPerImage);
    }

    /// <summary>
    /// Executes generateMipmapsForTexture.
    /// </summary>
    public void generateMipmapsForTexture(MTLTexture texture) {
        objc_msgSend(this.NativePtr, sel_generateMipmapsForTexture, texture.NativePtr);
    }

    /// <summary>
    /// Executes synchronizeResource.
    /// </summary>
    public void synchronizeResource(IntPtr resource) {
        objc_msgSend(this.NativePtr, sel_synchronizeResource, resource);
    }

    /// <summary>
    /// Executes endEncoding.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Executes pushDebugGroup.
    /// </summary>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes popDebugGroup.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Executes insertDebugSignpost.
    /// </summary>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }

    /// <summary>
    /// Executes copyFromTexture.
    /// </summary>
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
    /// Executes copyFromTexture_iOS.
    /// </summary>
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