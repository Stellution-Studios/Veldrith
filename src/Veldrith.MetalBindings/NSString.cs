using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSString struct.
/// </summary>
public unsafe struct NSString {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSString" /> class.
    /// </summary>
    public NSString(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(NSString nss) {
        return nss.NativePtr;
    }

    /// <summary>
    /// Executes New.
    /// </summary>
    public static NSString New(string s) {
        NSString nss = s_class.Alloc<NSString>();

        fixed (char* utf16Ptr = s) {
            UIntPtr length = (UIntPtr)s.Length;
            IntPtr newString = IntPtr_objc_msgSend(nss, sel_initWithCharacters, (IntPtr)utf16Ptr, length);
            return new NSString(newString);
        }
    }

    /// <summary>
    /// Executes GetValue.
    /// </summary>
    public string GetValue() {
        byte* utf8Ptr = bytePtr_objc_msgSend(this.NativePtr, sel_utf8String);
        return MTLUtil.GetUtf8String(utf8Ptr);
    }

    /// <summary>
    /// Represents the s_class field.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(NSString));

    /// <summary>
    /// Represents the sel_initWithCharacters field.
    /// </summary>
    private static readonly Selector sel_initWithCharacters = "initWithCharacters:length:";

    /// <summary>
    /// Represents the sel_utf8String field.
    /// </summary>
    private static readonly Selector sel_utf8String = "UTF8String";
}