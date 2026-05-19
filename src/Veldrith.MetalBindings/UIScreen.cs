using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the UIScreen data structure used by the graphics runtime.
/// </summary>
public struct UIScreen {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIScreen" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public UIScreen(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the cgfloat objc msg send logic for this backend.
    /// </summary>

    public CGFloat nativeScale => CGFloat_objc_msgSend(this.NativePtr, sel_nativeScale);

    /// <summary>
    /// Gets the primary screen object from UIKit.
    /// </summary>

    public static UIScreen mainScreen => objc_msgSend<UIScreen>(new ObjCClass(nameof(UIScreen)), sel_mainScreen);

    /// <summary>
    /// Stores the sel native scale state used by this instance.
    /// </summary>
    private static readonly Selector sel_nativeScale = "nativeScale";

    /// <summary>
    /// Stores the sel main screen state used by this instance.
    /// </summary>
    private static readonly Selector sel_mainScreen = "mainScreen";
}