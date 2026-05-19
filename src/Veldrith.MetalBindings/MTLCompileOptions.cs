using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLCompileOptions struct.
/// </summary>
public struct MTLCompileOptions {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Performs the operator IntPtr operation.
    /// </summary>
    /// <param name="mco">The value of mco.</param>
    /// <returns>The result of the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(MTLCompileOptions mco) {
        return mco.NativePtr;
    }

    /// <summary>
    /// Performs the New operation.
    /// </summary>
    /// <returns>The result of the New operation.</returns>
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
    /// Performs the new operation.
    /// </summary>
    /// <param name="MTLCompileOptions">The value of MTLCompileOptions.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(MTLCompileOptions));

    /// <summary>
    /// Represents the sel_fastMathEnabled field.
    /// </summary>
    private static readonly Selector sel_fastMathEnabled = "fastMathEnabled";

    /// <summary>
    /// Represents the sel_setFastMathEnabled field.
    /// </summary>
    private static readonly Selector sel_setFastMathEnabled = "setFastMathEnabled:";

    /// <summary>
    /// Represents the sel_languageVersion field.
    /// </summary>
    private static readonly Selector sel_languageVersion = "languageVersion";

    /// <summary>
    /// Represents the sel_setLanguageVersion field.
    /// </summary>
    private static readonly Selector sel_setLanguageVersion = "setLanguageVersion:";
}