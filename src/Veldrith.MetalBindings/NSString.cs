using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public unsafe struct NSString {
    public readonly IntPtr NativePtr;

    public NSString(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public static implicit operator IntPtr(NSString nss) {
        return nss.NativePtr;
    }

    public static NSString New(string s) {
        NSString nss = s_class.Alloc<NSString>();

        fixed (char* utf16Ptr = s) {
            UIntPtr length = (UIntPtr)s.Length;
            IntPtr newString = IntPtr_objc_msgSend(nss, sel_initWithCharacters, (IntPtr)utf16Ptr, length);
            return new NSString(newString);
        }
    }

    public string GetValue() {
        byte* utf8Ptr = bytePtr_objc_msgSend(this.NativePtr, sel_utf8String);
        return MTLUtil.GetUtf8String(utf8Ptr);
    }

    private static readonly ObjCClass s_class = new(nameof(NSString));
    private static readonly Selector sel_initWithCharacters = "initWithCharacters:length:";
    private static readonly Selector sel_utf8String = "UTF8String";
}