using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct MTLFunction {
    public readonly IntPtr NativePtr;

    public MTLFunction(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public NSDictionary functionConstantsDictionary => objc_msgSend<NSDictionary>(this.NativePtr, sel_functionConstantsDictionary);

    private static readonly Selector sel_functionConstantsDictionary = "functionConstantsDictionary";
}