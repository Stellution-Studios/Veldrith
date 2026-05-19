using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLSamplerDescriptor struct.
/// </summary>
public struct MTLSamplerDescriptor {

    /// <summary>
    /// Represents the s_class field.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(MTLSamplerDescriptor));

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes New.
    /// </summary>
    public static MTLSamplerDescriptor New() {
        return s_class.AllocInit<MTLSamplerDescriptor>();
    }

    /// <summary>
    /// Gets or sets rAddressMode.
    /// </summary>
    public MTLSamplerAddressMode rAddressMode {
        get => (MTLSamplerAddressMode)uint_objc_msgSend(this.NativePtr, sel_rAddressMode);
        set => objc_msgSend(this.NativePtr, sel_setRAddressMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets sAddressMode.
    /// </summary>
    public MTLSamplerAddressMode sAddressMode {
        get => (MTLSamplerAddressMode)uint_objc_msgSend(this.NativePtr, sel_sAddressMode);
        set => objc_msgSend(this.NativePtr, sel_setSAddressMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets tAddressMode.
    /// </summary>
    public MTLSamplerAddressMode tAddressMode {
        get => (MTLSamplerAddressMode)uint_objc_msgSend(this.NativePtr, sel_tAddressMode);
        set => objc_msgSend(this.NativePtr, sel_setTAddressMode, (uint)value);
    }

    /// <summary>
    /// Gets or sets minFilter.
    /// </summary>
    public MTLSamplerMinMagFilter minFilter {
        get => (MTLSamplerMinMagFilter)uint_objc_msgSend(this.NativePtr, sel_minFilter);
        set => objc_msgSend(this.NativePtr, sel_setMinFilter, (uint)value);
    }

    /// <summary>
    /// Gets or sets magFilter.
    /// </summary>
    public MTLSamplerMinMagFilter magFilter {
        get => (MTLSamplerMinMagFilter)uint_objc_msgSend(this.NativePtr, sel_magFilter);
        set => objc_msgSend(this.NativePtr, sel_setMagFilter, (uint)value);
    }

    /// <summary>
    /// Gets or sets mipFilter.
    /// </summary>
    public MTLSamplerMipFilter mipFilter {
        get => (MTLSamplerMipFilter)uint_objc_msgSend(this.NativePtr, sel_mipFilter);
        set => objc_msgSend(this.NativePtr, sel_setMipFilter, (uint)value);
    }

    /// <summary>
    /// Gets or sets lodMinClamp.
    /// </summary>
    public float lodMinClamp {
        get => float_objc_msgSend(this.NativePtr, sel_lodMinClamp);
        set => objc_msgSend(this.NativePtr, sel_setLodMinClamp, value);
    }

    /// <summary>
    /// Gets or sets lodMaxClamp.
    /// </summary>
    public float lodMaxClamp {
        get => float_objc_msgSend(this.NativePtr, sel_lodMaxClamp);
        set => objc_msgSend(this.NativePtr, sel_setLodMaxClamp, value);
    }

    /// <summary>
    /// Gets or sets lodAverage.
    /// </summary>
    public Bool8 lodAverage {
        get => bool8_objc_msgSend(this.NativePtr, sel_lodAverage);
        set => objc_msgSend(this.NativePtr, sel_setLodAverage, value);
    }

    /// <summary>
    /// Gets or sets maxAnisotropy.
    /// </summary>
    public UIntPtr maxAnisotropy {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_maxAnisotropy);
        set => objc_msgSend(this.NativePtr, sel_setMaAnisotropy, value);
    }

    /// <summary>
    /// Gets or sets compareFunction.
    /// </summary>
    public MTLCompareFunction compareFunction {
        get => (MTLCompareFunction)uint_objc_msgSend(this.NativePtr, sel_compareFunction);
        set => objc_msgSend(this.NativePtr, sel_setCompareFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets borderColor.
    /// </summary>
    public MTLSamplerBorderColor borderColor {
        get => (MTLSamplerBorderColor)uint_objc_msgSend(this.NativePtr, sel_borderColor);
        set => objc_msgSend(this.NativePtr, sel_setBorderColor, (uint)value);
    }

    /// <summary>
    /// Represents the sel_rAddressMode field.
    /// </summary>
    private static readonly Selector sel_rAddressMode = "rAddressMode";

    /// <summary>
    /// Represents the sel_setRAddressMode field.
    /// </summary>
    private static readonly Selector sel_setRAddressMode = "setRAddressMode:";

    /// <summary>
    /// Represents the sel_sAddressMode field.
    /// </summary>
    private static readonly Selector sel_sAddressMode = "sAddressMode";

    /// <summary>
    /// Represents the sel_setSAddressMode field.
    /// </summary>
    private static readonly Selector sel_setSAddressMode = "setSAddressMode:";

    /// <summary>
    /// Represents the sel_tAddressMode field.
    /// </summary>
    private static readonly Selector sel_tAddressMode = "tAddressMode";

    /// <summary>
    /// Represents the sel_setTAddressMode field.
    /// </summary>
    private static readonly Selector sel_setTAddressMode = "setTAddressMode:";

    /// <summary>
    /// Represents the sel_minFilter field.
    /// </summary>
    private static readonly Selector sel_minFilter = "minFilter";

    /// <summary>
    /// Represents the sel_setMinFilter field.
    /// </summary>
    private static readonly Selector sel_setMinFilter = "setMinFilter:";

    /// <summary>
    /// Represents the sel_magFilter field.
    /// </summary>
    private static readonly Selector sel_magFilter = "magFilter";

    /// <summary>
    /// Represents the sel_setMagFilter field.
    /// </summary>
    private static readonly Selector sel_setMagFilter = "setMagFilter:";

    /// <summary>
    /// Represents the sel_mipFilter field.
    /// </summary>
    private static readonly Selector sel_mipFilter = "mipFilter";

    /// <summary>
    /// Represents the sel_setMipFilter field.
    /// </summary>
    private static readonly Selector sel_setMipFilter = "setMipFilter:";

    /// <summary>
    /// Represents the sel_lodMinClamp field.
    /// </summary>
    private static readonly Selector sel_lodMinClamp = "lodMinClamp";

    /// <summary>
    /// Represents the sel_setLodMinClamp field.
    /// </summary>
    private static readonly Selector sel_setLodMinClamp = "setLodMinClamp:";

    /// <summary>
    /// Represents the sel_lodMaxClamp field.
    /// </summary>
    private static readonly Selector sel_lodMaxClamp = "lodMaxClamp";

    /// <summary>
    /// Represents the sel_setLodMaxClamp field.
    /// </summary>
    private static readonly Selector sel_setLodMaxClamp = "setLodMaxClamp:";

    /// <summary>
    /// Represents the sel_lodAverage field.
    /// </summary>
    private static readonly Selector sel_lodAverage = "lodAverage";

    /// <summary>
    /// Represents the sel_setLodAverage field.
    /// </summary>
    private static readonly Selector sel_setLodAverage = "setLodAverage:";

    /// <summary>
    /// Represents the sel_maxAnisotropy field.
    /// </summary>
    private static readonly Selector sel_maxAnisotropy = "maxAnisotropy";

    /// <summary>
    /// Represents the sel_setMaAnisotropy field.
    /// </summary>
    private static readonly Selector sel_setMaAnisotropy = "setMaxAnisotropy:";

    /// <summary>
    /// Represents the sel_compareFunction field.
    /// </summary>
    private static readonly Selector sel_compareFunction = "compareFunction";

    /// <summary>
    /// Represents the sel_setCompareFunction field.
    /// </summary>
    private static readonly Selector sel_setCompareFunction = "setCompareFunction:";

    /// <summary>
    /// Represents the sel_borderColor field.
    /// </summary>
    private static readonly Selector sel_borderColor = "borderColor";

    /// <summary>
    /// Represents the sel_setBorderColor field.
    /// </summary>
    private static readonly Selector sel_setBorderColor = "setBorderColor:";
}