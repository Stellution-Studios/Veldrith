using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the UIScreen struct.
/// </summary>
public struct UIScreen {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIScreen" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public UIScreen(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the CGFloat_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">Specifies the value of <paramref name="NativePtr" />.</param>
    /// <param name="sel_nativeScale">Specifies the value of <paramref name="sel_nativeScale" />.</param>
    /// <returns>Returns the result produced by the CGFloat_objc_msgSend operation.</returns>
    public CGFloat nativeScale => CGFloat_objc_msgSend(this.NativePtr, sel_nativeScale);

    /// <summary>
    /// Gets the primary screen object from UIKit.
    /// </summary>
    public static UIScreen mainScreen => objc_msgSend<UIScreen>(new ObjCClass(nameof(UIScreen)), sel_mainScreen);

    /// <summary>
    /// Stores the value associated with <c>sel_nativeScale</c>.
    /// </summary>
    private static readonly Selector sel_nativeScale = "nativeScale";

    /// <summary>
    /// Stores the value associated with <c>sel_mainScreen</c>.
    /// </summary>
    private static readonly Selector sel_mainScreen = "mainScreen";
}