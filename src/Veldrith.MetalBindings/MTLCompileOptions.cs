using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLCompileOptions data structure used by the graphics runtime.
/// </summary>
public struct MTLCompileOptions {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="mco">The mco value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(MTLCompileOptions mco) {
        return mco.NativePtr;
    }

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static MTLCompileOptions New() {
        return s_class.AllocInit<MTLCompileOptions>();
    }

    /// <summary>
    /// Gets or sets fastMathEnabled.
    /// </summary>
    public Bool8 fastMathEnabled {
        get => bool8_objc_msgSend(this.NativePtr, sel_fastMathEnabled);
        set => objc_msgSend(this.NativePtr, sel_setFastMathEnabled, value);
    }

    /// <summary>
    /// Gets or sets languageVersion.
    /// </summary>
    public MTLLanguageVersion languageVersion {
        get => (MTLLanguageVersion)uint_objc_msgSend(this.NativePtr, sel_languageVersion);
        set => objc_msgSend(this.NativePtr, sel_setLanguageVersion, (uint)value);
    }

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(MTLCompileOptions));

    /// <summary>
    /// Stores the sel fast math enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_fastMathEnabled = "fastMathEnabled";

    /// <summary>
    /// Stores the sel set fast math enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFastMathEnabled = "setFastMathEnabled:";

    /// <summary>
    /// Stores the sel language version state used by this instance.
    /// </summary>
    private static readonly Selector sel_languageVersion = "languageVersion";

    /// <summary>
    /// Stores the sel set language version state used by this instance.
    /// </summary>
    private static readonly Selector sel_setLanguageVersion = "setLanguageVersion:";
}