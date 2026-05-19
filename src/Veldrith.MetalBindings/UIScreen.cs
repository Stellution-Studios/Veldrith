using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the UIScreen struct.
/// </summary>
public struct UIScreen {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIScreen" /> class.
    /// </summary>
    public UIScreen(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets nativeScale.
    /// </summary>
    public CGFloat nativeScale => CGFloat_objc_msgSend(this.NativePtr, sel_nativeScale);

    public static UIScreen mainScreen
        => objc_msgSend<UIScreen>(new ObjCClass(nameof(UIScreen)), sel_mainScreen);

    /// <summary>
    /// Represents the sel_nativeScale field.
    /// </summary>
    private static readonly Selector sel_nativeScale = "nativeScale";

    /// <summary>
    /// Represents the sel_mainScreen field.
    /// </summary>
    private static readonly Selector sel_mainScreen = "mainScreen";
}