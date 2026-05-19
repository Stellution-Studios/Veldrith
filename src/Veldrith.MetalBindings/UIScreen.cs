using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct UIScreen {
    public readonly IntPtr NativePtr;

    public UIScreen(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public CGFloat nativeScale => CGFloat_objc_msgSend(this.NativePtr, sel_nativeScale);

    public static UIScreen mainScreen
        => objc_msgSend<UIScreen>(new ObjCClass(nameof(UIScreen)), sel_mainScreen);

    private static readonly Selector sel_nativeScale = "nativeScale";
    private static readonly Selector sel_mainScreen = "mainScreen";
}