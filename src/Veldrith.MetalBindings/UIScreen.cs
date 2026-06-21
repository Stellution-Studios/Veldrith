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

    public CGFloat NativeScale => CGFloat_objc_msgSend(this.NativePtr, _selNativeScale);

    /// <summary>
    /// Gets the primary screen object from UIKit.
    /// </summary>

    public static UIScreen MainScreen => ObjcMsgSend<UIScreen>(new ObjCClass(nameof(UIScreen)), _selMainScreen);

    /// <summary>
    /// Stores the sel native scale state used by this instance.
    /// </summary>
    private static readonly Selector _selNativeScale = "nativeScale";

    /// <summary>
    /// Stores the sel main screen state used by this instance.
    /// </summary>
    private static readonly Selector _selMainScreen = "mainScreen";
}
