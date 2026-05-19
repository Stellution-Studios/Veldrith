using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLTextureDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLTextureDescriptor {

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(MTLTextureDescriptor));

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
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
    /// Stores the sel texture type state used by this instance.
    /// </summary>
    private static readonly Selector sel_textureType = "textureType";

    /// <summary>
    /// Stores the sel set texture type state used by this instance.
    /// </summary>
    private static readonly Selector sel_setTextureType = "setTextureType:";

    /// <summary>
    /// Stores the sel width value used during command execution.
    /// </summary>
    private static readonly Selector sel_width = "width";

    /// <summary>
    /// Stores the sel set width value used during command execution.
    /// </summary>
    private static readonly Selector sel_setWidth = "setWidth:";

    /// <summary>
    /// Stores the sel height value used during command execution.
    /// </summary>
    private static readonly Selector sel_height = "height";

    /// <summary>
    /// Stores the sel set height value used during command execution.
    /// </summary>
    private static readonly Selector sel_setHeight = "setHeight:";

    /// <summary>
    /// Stores the sel depth value used during command execution.
    /// </summary>
    private static readonly Selector sel_depth = "depth";

    /// <summary>
    /// Stores the sel set depth value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepth = "setDepth:";

    /// <summary>
    /// Stores the sel mipmap level count value used during command execution.
    /// </summary>
    private static readonly Selector sel_mipmapLevelCount = "mipmapLevelCount";

    /// <summary>
    /// Stores the sel set mipmap level count value used during command execution.
    /// </summary>
    private static readonly Selector sel_setMipmapLevelCount = "setMipmapLevelCount:";

    /// <summary>
    /// Stores the sel sample count value used during command execution.
    /// </summary>
    private static readonly Selector sel_sampleCount = "sampleCount";

    /// <summary>
    /// Stores the sel set sample count value used during command execution.
    /// </summary>
    private static readonly Selector sel_setSampleCount = "setSampleCount:";

    /// <summary>
    /// Stores the sel array length collection used by this instance.
    /// </summary>
    private static readonly Selector sel_arrayLength = "arrayLength";

    /// <summary>
    /// Stores the sel set array length collection used by this instance.
    /// </summary>
    private static readonly Selector sel_setArrayLength = "setArrayLength:";

    /// <summary>
    /// Stores the sel resource options state used by this instance.
    /// </summary>
    private static readonly Selector sel_resourceOptions = "resourceOptions";

    /// <summary>
    /// Stores the sel set resource options state used by this instance.
    /// </summary>
    private static readonly Selector sel_setResourceOptions = "setResourceOptions:";

    /// <summary>
    /// Caches sel cpu cache mode to reduce repeated allocations and lookups.
    /// </summary>
    private static readonly Selector sel_cpuCacheMode = "cpuCacheMode";

    /// <summary>
    /// Caches sel set cpu cache mode to reduce repeated allocations and lookups.
    /// </summary>
    private static readonly Selector sel_setCpuCacheMode = "setCpuCacheMode:";

    /// <summary>
    /// Stores the sel storage mode state used by this instance.
    /// </summary>
    private static readonly Selector sel_storageMode = "storageMode";

    /// <summary>
    /// Stores the sel set storage mode state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStorageMode = "setStorageMode:";

    /// <summary>
    /// Stores the sel texture usage state used by this instance.
    /// </summary>
    private static readonly Selector sel_textureUsage = "textureUsage";

    /// <summary>
    /// Stores the sel set texture usage state used by this instance.
    /// </summary>
    private static readonly Selector sel_setTextureUsage = "setTextureUsage:";
}