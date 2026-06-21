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
    private static readonly ObjCClass _sClass = new(nameof(MTLTextureDescriptor));

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static MTLTextureDescriptor New() {
        return _sClass.AllocInit<MTLTextureDescriptor>();
    }

    /// <summary>
    /// Gets or sets textureType.
    /// </summary>
    public MTLTextureType TextureType {
        get => (MTLTextureType)UIntObjcMsgSend(this.NativePtr, _selTextureType);
        set => ObjcMsgSend(this.NativePtr, _selSetTextureType, (uint)value);
    }

    /// <summary>
    /// Gets or sets pixelFormat.
    /// </summary>
    public MTLPixelFormat PixelFormat {
        get => (MTLPixelFormat)UIntObjcMsgSend(this.NativePtr, Selectors.PixelFormat);
        set => ObjcMsgSend(this.NativePtr, Selectors.SetPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public UIntPtr Width {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selWidth);
        set => ObjcMsgSend(this.NativePtr, _selSetWidth, value);
    }

    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public UIntPtr Height {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selHeight);
        set => ObjcMsgSend(this.NativePtr, _selSetHeight, value);
    }

    /// <summary>
    /// Gets or sets depth.
    /// </summary>
    public UIntPtr Depth {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selDepth);
        set => ObjcMsgSend(this.NativePtr, _selSetDepth, value);
    }

    /// <summary>
    /// Gets or sets mipmapLevelCount.
    /// </summary>
    public UIntPtr MipmapLevelCount {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selMipmapLevelCount);
        set => ObjcMsgSend(this.NativePtr, _selSetMipmapLevelCount, value);
    }

    /// <summary>
    /// Gets or sets sampleCount.
    /// </summary>
    public UIntPtr SampleCount {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selSampleCount);
        set => ObjcMsgSend(this.NativePtr, _selSetSampleCount, value);
    }

    /// <summary>
    /// Gets or sets arrayLength.
    /// </summary>
    public UIntPtr ArrayLength {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selArrayLength);
        set => ObjcMsgSend(this.NativePtr, _selSetArrayLength, value);
    }

    /// <summary>
    /// Gets or sets resourceOptions.
    /// </summary>
    public MTLResourceOptions ResourceOptions {
        get => (MTLResourceOptions)UIntObjcMsgSend(this.NativePtr, _selResourceOptions);
        set => ObjcMsgSend(this.NativePtr, _selSetResourceOptions, (uint)value);
    }

    /// <summary>
    /// Gets or sets cpuCacheMode.
    /// </summary>
    public MTLCPUCacheMode CpuCacheMode {
        get => (MTLCPUCacheMode)UIntObjcMsgSend(this.NativePtr, _selCpuCacheMode);
        set => ObjcMsgSend(this.NativePtr, _selSetCpuCacheMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets storageMode.
    /// </summary>
    public MTLStorageMode StorageMode {
        get => (MTLStorageMode)UIntObjcMsgSend(this.NativePtr, _selStorageMode);
        set => ObjcMsgSend(this.NativePtr, _selSetStorageMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets textureUsage.
    /// </summary>
    public MTLTextureUsage TextureUsage {
        get => (MTLTextureUsage)UIntObjcMsgSend(this.NativePtr, _selTextureUsage);
        set => ObjcMsgSend(this.NativePtr, _selSetTextureUsage, (uint)value);
    }

    /// <summary>
    /// Stores the sel texture type state used by this instance.
    /// </summary>
    private static readonly Selector _selTextureType = "textureType";

    /// <summary>
    /// Stores the sel set texture type state used by this instance.
    /// </summary>
    private static readonly Selector _selSetTextureType = "setTextureType:";

    /// <summary>
    /// Stores the sel width value used during command execution.
    /// </summary>
    private static readonly Selector _selWidth = "width";

    /// <summary>
    /// Stores the sel set width value used during command execution.
    /// </summary>
    private static readonly Selector _selSetWidth = "setWidth:";

    /// <summary>
    /// Stores the sel height value used during command execution.
    /// </summary>
    private static readonly Selector _selHeight = "height";

    /// <summary>
    /// Stores the sel set height value used during command execution.
    /// </summary>
    private static readonly Selector _selSetHeight = "setHeight:";

    /// <summary>
    /// Stores the sel depth value used during command execution.
    /// </summary>
    private static readonly Selector _selDepth = "depth";

    /// <summary>
    /// Stores the sel set depth value used during command execution.
    /// </summary>
    private static readonly Selector _selSetDepth = "setDepth:";

    /// <summary>
    /// Stores the sel mipmap level count value used during command execution.
    /// </summary>
    private static readonly Selector _selMipmapLevelCount = "mipmapLevelCount";

    /// <summary>
    /// Stores the sel set mipmap level count value used during command execution.
    /// </summary>
    private static readonly Selector _selSetMipmapLevelCount = "setMipmapLevelCount:";

    /// <summary>
    /// Stores the sel sample count value used during command execution.
    /// </summary>
    private static readonly Selector _selSampleCount = "sampleCount";

    /// <summary>
    /// Stores the sel set sample count value used during command execution.
    /// </summary>
    private static readonly Selector _selSetSampleCount = "setSampleCount:";

    /// <summary>
    /// Stores the sel array length collection used by this instance.
    /// </summary>
    private static readonly Selector _selArrayLength = "arrayLength";

    /// <summary>
    /// Stores the sel set array length collection used by this instance.
    /// </summary>
    private static readonly Selector _selSetArrayLength = "setArrayLength:";

    /// <summary>
    /// Stores the sel resource options state used by this instance.
    /// </summary>
    private static readonly Selector _selResourceOptions = "resourceOptions";

    /// <summary>
    /// Stores the sel set resource options state used by this instance.
    /// </summary>
    private static readonly Selector _selSetResourceOptions = "setResourceOptions:";

    /// <summary>
    /// Caches sel cpu cache mode to reduce repeated allocations and lookups.
    /// </summary>
    private static readonly Selector _selCpuCacheMode = "cpuCacheMode";

    /// <summary>
    /// Caches sel set cpu cache mode to reduce repeated allocations and lookups.
    /// </summary>
    private static readonly Selector _selSetCpuCacheMode = "setCpuCacheMode:";

    /// <summary>
    /// Stores the sel storage mode state used by this instance.
    /// </summary>
    private static readonly Selector _selStorageMode = "storageMode";

    /// <summary>
    /// Stores the sel set storage mode state used by this instance.
    /// </summary>
    private static readonly Selector _selSetStorageMode = "setStorageMode:";

    /// <summary>
    /// Stores the sel texture usage state used by this instance.
    /// </summary>
    private static readonly Selector _selTextureUsage = "textureUsage";

    /// <summary>
    /// Stores the sel set texture usage state used by this instance.
    /// </summary>
    private static readonly Selector _selSetTextureUsage = "setTextureUsage:";
}
