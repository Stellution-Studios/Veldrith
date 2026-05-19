using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLSamplerDescriptor struct.
/// </summary>
public struct MTLSamplerDescriptor {

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="MTLSamplerDescriptor">Specifies the value of <paramref name="MTLSamplerDescriptor" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(MTLSamplerDescriptor));

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>Returns the result produced by the New operation.</returns>
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
    /// Stores the value associated with <c>sel_rAddressMode</c>.
    /// </summary>
    private static readonly Selector sel_rAddressMode = "rAddressMode";

    /// <summary>
    /// Stores the value associated with <c>sel_setRAddressMode</c>.
    /// </summary>
    private static readonly Selector sel_setRAddressMode = "setRAddressMode:";

    /// <summary>
    /// Stores the value associated with <c>sel_sAddressMode</c>.
    /// </summary>
    private static readonly Selector sel_sAddressMode = "sAddressMode";

    /// <summary>
    /// Stores the value associated with <c>sel_setSAddressMode</c>.
    /// </summary>
    private static readonly Selector sel_setSAddressMode = "setSAddressMode:";

    /// <summary>
    /// Stores the value associated with <c>sel_tAddressMode</c>.
    /// </summary>
    private static readonly Selector sel_tAddressMode = "tAddressMode";

    /// <summary>
    /// Stores the value associated with <c>sel_setTAddressMode</c>.
    /// </summary>
    private static readonly Selector sel_setTAddressMode = "setTAddressMode:";

    /// <summary>
    /// Stores the value associated with <c>sel_minFilter</c>.
    /// </summary>
    private static readonly Selector sel_minFilter = "minFilter";

    /// <summary>
    /// Stores the value associated with <c>sel_setMinFilter</c>.
    /// </summary>
    private static readonly Selector sel_setMinFilter = "setMinFilter:";

    /// <summary>
    /// Stores the value associated with <c>sel_magFilter</c>.
    /// </summary>
    private static readonly Selector sel_magFilter = "magFilter";

    /// <summary>
    /// Stores the value associated with <c>sel_setMagFilter</c>.
    /// </summary>
    private static readonly Selector sel_setMagFilter = "setMagFilter:";

    /// <summary>
    /// Stores the value associated with <c>sel_mipFilter</c>.
    /// </summary>
    private static readonly Selector sel_mipFilter = "mipFilter";

    /// <summary>
    /// Stores the value associated with <c>sel_setMipFilter</c>.
    /// </summary>
    private static readonly Selector sel_setMipFilter = "setMipFilter:";

    /// <summary>
    /// Stores the value associated with <c>sel_lodMinClamp</c>.
    /// </summary>
    private static readonly Selector sel_lodMinClamp = "lodMinClamp";

    /// <summary>
    /// Stores the value associated with <c>sel_setLodMinClamp</c>.
    /// </summary>
    private static readonly Selector sel_setLodMinClamp = "setLodMinClamp:";

    /// <summary>
    /// Stores the value associated with <c>sel_lodMaxClamp</c>.
    /// </summary>
    private static readonly Selector sel_lodMaxClamp = "lodMaxClamp";

    /// <summary>
    /// Stores the value associated with <c>sel_setLodMaxClamp</c>.
    /// </summary>
    private static readonly Selector sel_setLodMaxClamp = "setLodMaxClamp:";

    /// <summary>
    /// Stores the value associated with <c>sel_lodAverage</c>.
    /// </summary>
    private static readonly Selector sel_lodAverage = "lodAverage";

    /// <summary>
    /// Stores the value associated with <c>sel_setLodAverage</c>.
    /// </summary>
    private static readonly Selector sel_setLodAverage = "setLodAverage:";

    /// <summary>
    /// Stores the value associated with <c>sel_maxAnisotropy</c>.
    /// </summary>
    private static readonly Selector sel_maxAnisotropy = "maxAnisotropy";

    /// <summary>
    /// Stores the value associated with <c>sel_setMaAnisotropy</c>.
    /// </summary>
    private static readonly Selector sel_setMaAnisotropy = "setMaxAnisotropy:";

    /// <summary>
    /// Stores the value associated with <c>sel_compareFunction</c>.
    /// </summary>
    private static readonly Selector sel_compareFunction = "compareFunction";

    /// <summary>
    /// Stores the value associated with <c>sel_setCompareFunction</c>.
    /// </summary>
    private static readonly Selector sel_setCompareFunction = "setCompareFunction:";

    /// <summary>
    /// Stores the value associated with <c>sel_borderColor</c>.
    /// </summary>
    private static readonly Selector sel_borderColor = "borderColor";

    /// <summary>
    /// Stores the value associated with <c>sel_setBorderColor</c>.
    /// </summary>
    private static readonly Selector sel_setBorderColor = "setBorderColor:";
}
