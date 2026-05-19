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
    /// Initializes a new instance of the <see cref="UIScreen" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public UIScreen(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the CGFloat_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">The value of NativePtr.</param>
    /// <param name="sel_nativeScale">The value of sel_nativeScale.</param>
    /// <returns>The result of the CGFloat_objc_msgSend operation.</returns>
    public CGFloat nativeScale => CGFloat_objc_msgSend(this.NativePtr, sel_nativeScale);

    /// <summary>
    /// Gets the primary screen object from UIKit.
    /// </summary>
    public static UIScreen mainScreen => objc_msgSend<UIScreen>(new ObjCClass(nameof(UIScreen)), sel_mainScreen);

    /// <summary>
    /// Represents the sel_nativeScale field.
    /// </summary>
    private static readonly Selector sel_nativeScale = "nativeScale";

    /// <summary>
    /// Represents the sel_mainScreen field.
    /// </summary>
    private static readonly Selector sel_mainScreen = "mainScreen";
}