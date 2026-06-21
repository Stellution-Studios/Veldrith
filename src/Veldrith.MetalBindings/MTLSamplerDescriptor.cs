using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLSamplerDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLSamplerDescriptor {

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass _sClass = new(nameof(MTLSamplerDescriptor));

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static MTLSamplerDescriptor New() {
        return _sClass.AllocInit<MTLSamplerDescriptor>();
    }

    /// <summary>
    /// Gets or sets rAddressMode.
    /// </summary>
    public MTLSamplerAddressMode RAddressMode {
        get => (MTLSamplerAddressMode)UIntObjcMsgSend(this.NativePtr, _selRAddressMode);
        set => ObjcMsgSend(this.NativePtr, _selSetRAddressMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets sAddressMode.
    /// </summary>
    public MTLSamplerAddressMode SAddressMode {
        get => (MTLSamplerAddressMode)UIntObjcMsgSend(this.NativePtr, _selSAddressMode);
        set => ObjcMsgSend(this.NativePtr, _selSetSAddressMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets tAddressMode.
    /// </summary>
    public MTLSamplerAddressMode TAddressMode {
        get => (MTLSamplerAddressMode)UIntObjcMsgSend(this.NativePtr, _selTAddressMode);
        set => ObjcMsgSend(this.NativePtr, _selSetTAddressMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets minFilter.
    /// </summary>
    public MTLSamplerMinMagFilter MinFilter {
        get => (MTLSamplerMinMagFilter)UIntObjcMsgSend(this.NativePtr, _selMinFilter);
        set => ObjcMsgSend(this.NativePtr, _selSetMinFilter, (uint)value);
    }

    /// <summary>
    /// Gets or sets magFilter.
    /// </summary>
    public MTLSamplerMinMagFilter MagFilter {
        get => (MTLSamplerMinMagFilter)UIntObjcMsgSend(this.NativePtr, _selMagFilter);
        set => ObjcMsgSend(this.NativePtr, _selSetMagFilter, (uint)value);
    }

    /// <summary>
    /// Gets or sets mipFilter.
    /// </summary>
    public MTLSamplerMipFilter MipFilter {
        get => (MTLSamplerMipFilter)UIntObjcMsgSend(this.NativePtr, _selMipFilter);
        set => ObjcMsgSend(this.NativePtr, _selSetMipFilter, (uint)value);
    }

    /// <summary>
    /// Gets or sets lodMinClamp.
    /// </summary>
    public float LodMinClamp {
        get => FloatObjcMsgSend(this.NativePtr, _selLodMinClamp);
        set => ObjcMsgSend(this.NativePtr, _selSetLodMinClamp, value);
    }

    /// <summary>
    /// Gets or sets lodMaxClamp.
    /// </summary>
    public float LodMaxClamp {
        get => FloatObjcMsgSend(this.NativePtr, _selLodMaxClamp);
        set => ObjcMsgSend(this.NativePtr, _selSetLodMaxClamp, value);
    }

    /// <summary>
    /// Gets or sets lodAverage.
    /// </summary>
    public Bool8 LodAverage {
        get => Bool8ObjcMsgSend(this.NativePtr, _selLodAverage);
        set => ObjcMsgSend(this.NativePtr, _selSetLodAverage, value);
    }

    /// <summary>
    /// Gets or sets maxAnisotropy.
    /// </summary>
    public UIntPtr MaxAnisotropy {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selMaxAnisotropy);
        set => ObjcMsgSend(this.NativePtr, _selSetMaAnisotropy, value);
    }

    /// <summary>
    /// Gets or sets compareFunction.
    /// </summary>
    public MTLCompareFunction CompareFunction {
        get => (MTLCompareFunction)UIntObjcMsgSend(this.NativePtr, _selCompareFunction);
        set => ObjcMsgSend(this.NativePtr, _selSetCompareFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets borderColor.
    /// </summary>
    public MTLSamplerBorderColor BorderColor {
        get => (MTLSamplerBorderColor)UIntObjcMsgSend(this.NativePtr, _selBorderColor);
        set => ObjcMsgSend(this.NativePtr, _selSetBorderColor, (uint)value);
    }

    /// <summary>
    /// Stores the sel r address mode state used by this instance.
    /// </summary>
    private static readonly Selector _selRAddressMode = "rAddressMode";

    /// <summary>
    /// Stores the sel set raddress mode state used by this instance.
    /// </summary>
    private static readonly Selector _selSetRAddressMode = "setRAddressMode:";

    /// <summary>
    /// Stores the sel s address mode state used by this instance.
    /// </summary>
    private static readonly Selector _selSAddressMode = "sAddressMode";

    /// <summary>
    /// Stores the sel set saddress mode state used by this instance.
    /// </summary>
    private static readonly Selector _selSetSAddressMode = "setSAddressMode:";

    /// <summary>
    /// Stores the sel t address mode state used by this instance.
    /// </summary>
    private static readonly Selector _selTAddressMode = "tAddressMode";

    /// <summary>
    /// Stores the sel set taddress mode state used by this instance.
    /// </summary>
    private static readonly Selector _selSetTAddressMode = "setTAddressMode:";

    /// <summary>
    /// Stores the sel min filter state used by this instance.
    /// </summary>
    private static readonly Selector _selMinFilter = "minFilter";

    /// <summary>
    /// Stores the sel set min filter state used by this instance.
    /// </summary>
    private static readonly Selector _selSetMinFilter = "setMinFilter:";

    /// <summary>
    /// Stores the sel mag filter state used by this instance.
    /// </summary>
    private static readonly Selector _selMagFilter = "magFilter";

    /// <summary>
    /// Stores the sel set mag filter state used by this instance.
    /// </summary>
    private static readonly Selector _selSetMagFilter = "setMagFilter:";

    /// <summary>
    /// Stores the sel mip filter state used by this instance.
    /// </summary>
    private static readonly Selector _selMipFilter = "mipFilter";

    /// <summary>
    /// Stores the sel set mip filter state used by this instance.
    /// </summary>
    private static readonly Selector _selSetMipFilter = "setMipFilter:";

    /// <summary>
    /// Stores the sel lod min clamp state used by this instance.
    /// </summary>
    private static readonly Selector _selLodMinClamp = "lodMinClamp";

    /// <summary>
    /// Stores the sel set lod min clamp state used by this instance.
    /// </summary>
    private static readonly Selector _selSetLodMinClamp = "setLodMinClamp:";

    /// <summary>
    /// Stores the sel lod max clamp state used by this instance.
    /// </summary>
    private static readonly Selector _selLodMaxClamp = "lodMaxClamp";

    /// <summary>
    /// Stores the sel set lod max clamp state used by this instance.
    /// </summary>
    private static readonly Selector _selSetLodMaxClamp = "setLodMaxClamp:";

    /// <summary>
    /// Stores the sel lod average state used by this instance.
    /// </summary>
    private static readonly Selector _selLodAverage = "lodAverage";

    /// <summary>
    /// Stores the sel set lod average state used by this instance.
    /// </summary>
    private static readonly Selector _selSetLodAverage = "setLodAverage:";

    /// <summary>
    /// Stores the sel max anisotropy state used by this instance.
    /// </summary>
    private static readonly Selector _selMaxAnisotropy = "maxAnisotropy";

    /// <summary>
    /// Stores the sel set ma anisotropy state used by this instance.
    /// </summary>
    private static readonly Selector _selSetMaAnisotropy = "setMaxAnisotropy:";

    /// <summary>
    /// Stores the sel compare function state used by this instance.
    /// </summary>
    private static readonly Selector _selCompareFunction = "compareFunction";

    /// <summary>
    /// Stores the sel set compare function state used by this instance.
    /// </summary>
    private static readonly Selector _selSetCompareFunction = "setCompareFunction:";

    /// <summary>
    /// Stores the sel border color state used by this instance.
    /// </summary>
    private static readonly Selector _selBorderColor = "borderColor";

    /// <summary>
    /// Stores the sel set border color state used by this instance.
    /// </summary>
    private static readonly Selector _selSetBorderColor = "setBorderColor:";
}
