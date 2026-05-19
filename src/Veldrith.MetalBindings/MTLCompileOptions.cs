using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLCompileOptions struct.
/// </summary>
public struct MTLCompileOptions {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="mco">Specifies the value of <paramref name="mco" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(MTLCompileOptions mco) {
        return mco.NativePtr;
    }

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>Returns the result produced by the New operation.</returns>
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
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="MTLCompileOptions">Specifies the value of <paramref name="MTLCompileOptions" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(MTLCompileOptions));

    /// <summary>
    /// Stores the value associated with <c>sel_fastMathEnabled</c>.
    /// </summary>
    private static readonly Selector sel_fastMathEnabled = "fastMathEnabled";

    /// <summary>
    /// Stores the value associated with <c>sel_setFastMathEnabled</c>.
    /// </summary>
    private static readonly Selector sel_setFastMathEnabled = "setFastMathEnabled:";

    /// <summary>
    /// Stores the value associated with <c>sel_languageVersion</c>.
    /// </summary>
    private static readonly Selector sel_languageVersion = "languageVersion";

    /// <summary>
    /// Stores the value associated with <c>sel_setLanguageVersion</c>.
    /// </summary>
    private static readonly Selector sel_setLanguageVersion = "setLanguageVersion:";
}
