using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLTextureDescriptor struct.
/// </summary>
public struct MTLTextureDescriptor {

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="MTLTextureDescriptor">The value of MTLTextureDescriptor.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(MTLTextureDescriptor));

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Performs the New operation.
    /// </summary>
    /// <returns>The result of the New operation.</returns>
    public static MTLTextureDescriptor New() {
        return s_class.AllocInit<MTLTextureDescriptor>();
    }

    /// <summary>
    /// Gets or sets textureType.
    /// </summary>
    public MTLTextureType textureType {
        get => (MTLTextureType)uint_objc_msgSend(this.NativePtr, sel_textureType);
        set => objc_msgSend(this.NativePtr, sel_setTextureType, (uint)value);
    }

    /// <summary>
    /// Gets or sets pixelFormat.
    /// </summary>
    public MTLPixelFormat pixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, Selectors.pixelFormat);
        set => objc_msgSend(this.NativePtr, Selectors.setPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public UIntPtr width {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_width);
        set => objc_msgSend(this.NativePtr, sel_setWidth, value);
    }

    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public UIntPtr height {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_height);
        set => objc_msgSend(this.NativePtr, sel_setHeight, value);
    }

    /// <summary>
    /// Gets or sets depth.
    /// </summary>
    public UIntPtr depth {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_depth);
        set => objc_msgSend(this.NativePtr, sel_setDepth, value);
    }

    /// <summary>
    /// Gets or sets mipmapLevelCount.
    /// </summary>
    public UIntPtr mipmapLevelCount {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_mipmapLevelCount);
        set => objc_msgSend(this.NativePtr, sel_setMipmapLevelCount, value);
    }

    /// <summary>
    /// Gets or sets sampleCount.
    /// </summary>
    public UIntPtr sampleCount {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_sampleCount);
        set => objc_msgSend(this.NativePtr, sel_setSampleCount, value);
    }

    /// <summary>
    /// Gets or sets arrayLength.
    /// </summary>
    public UIntPtr arrayLength {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_arrayLength);
        set => objc_msgSend(this.NativePtr, sel_setArrayLength, value);
    }

    /// <summary>
    /// Gets or sets resourceOptions.
    /// </summary>
    public MTLResourceOptions resourceOptions {
        get => (MTLResourceOptions)uint_objc_msgSend(this.NativePtr, sel_resourceOptions);
        set => objc_msgSend(this.NativePtr, sel_setResourceOptions, (uint)value);
    }

    /// <summary>
    /// Gets or sets cpuCacheMode.
    /// </summary>
    public MTLCPUCacheMode cpuCacheMode {
        get => (MTLCPUCacheMode)uint_objc_msgSend(this.NativePtr, sel_cpuCacheMode);
        set => objc_msgSend(this.NativePtr, sel_setCpuCacheMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets storageMode.
    /// </summary>
    public MTLStorageMode storageMode {
        get => (MTLStorageMode)uint_objc_msgSend(this.NativePtr, sel_storageMode);
        set => objc_msgSend(this.NativePtr, sel_setStorageMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets textureUsage.
    /// </summary>
    public MTLTextureUsage textureUsage {
        get => (MTLTextureUsage)uint_objc_msgSend(this.NativePtr, sel_textureUsage);
        set => objc_msgSend(this.NativePtr, sel_setTextureUsage, (uint)value);
    }

    /// <summary>
    /// Represents the sel_textureType field.
    /// </summary>
    private static readonly Selector sel_textureType = "textureType";

    /// <summary>
    /// Represents the sel_setTextureType field.
    /// </summary>
    private static readonly Selector sel_setTextureType = "setTextureType:";

    /// <summary>
    /// Represents the sel_width field.
    /// </summary>
    private static readonly Selector sel_width = "width";

    /// <summary>
    /// Represents the sel_setWidth field.
    /// </summary>
    private static readonly Selector sel_setWidth = "setWidth:";

    /// <summary>
    /// Represents the sel_height field.
    /// </summary>
    private static readonly Selector sel_height = "height";

    /// <summary>
    /// Represents the sel_setHeight field.
    /// </summary>
    private static readonly Selector sel_setHeight = "setHeight:";

    /// <summary>
    /// Represents the sel_depth field.
    /// </summary>
    private static readonly Selector sel_depth = "depth";

    /// <summary>
    /// Represents the sel_setDepth field.
    /// </summary>
    private static readonly Selector sel_setDepth = "setDepth:";

    /// <summary>
    /// Represents the sel_mipmapLevelCount field.
    /// </summary>
    private static readonly Selector sel_mipmapLevelCount = "mipmapLevelCount";

    /// <summary>
    /// Represents the sel_setMipmapLevelCount field.
    /// </summary>
    private static readonly Selector sel_setMipmapLevelCount = "setMipmapLevelCount:";

    /// <summary>
    /// Represents the sel_sampleCount field.
    /// </summary>
    private static readonly Selector sel_sampleCount = "sampleCount";

    /// <summary>
    /// Represents the sel_setSampleCount field.
    /// </summary>
    private static readonly Selector sel_setSampleCount = "setSampleCount:";

    /// <summary>
    /// Represents the sel_arrayLength field.
    /// </summary>
    private static readonly Selector sel_arrayLength = "arrayLength";

    /// <summary>
    /// Represents the sel_setArrayLength field.
    /// </summary>
    private static readonly Selector sel_setArrayLength = "setArrayLength:";

    /// <summary>
    /// Represents the sel_resourceOptions field.
    /// </summary>
    private static readonly Selector sel_resourceOptions = "resourceOptions";

    /// <summary>
    /// Represents the sel_setResourceOptions field.
    /// </summary>
    private static readonly Selector sel_setResourceOptions = "setResourceOptions:";

    /// <summary>
    /// Represents the sel_cpuCacheMode field.
    /// </summary>
    private static readonly Selector sel_cpuCacheMode = "cpuCacheMode";

    /// <summary>
    /// Represents the sel_setCpuCacheMode field.
    /// </summary>
    private static readonly Selector sel_setCpuCacheMode = "setCpuCacheMode:";

    /// <summary>
    /// Represents the sel_storageMode field.
    /// </summary>
    private static readonly Selector sel_storageMode = "storageMode";

    /// <summary>
    /// Represents the sel_setStorageMode field.
    /// </summary>
    private static readonly Selector sel_setStorageMode = "setStorageMode:";

    /// <summary>
    /// Represents the sel_textureUsage field.
    /// </summary>
    private static readonly Selector sel_textureUsage = "textureUsage";

    /// <summary>
    /// Represents the sel_setTextureUsage field.
    /// </summary>
    private static readonly Selector sel_setTextureUsage = "setTextureUsage:";
}