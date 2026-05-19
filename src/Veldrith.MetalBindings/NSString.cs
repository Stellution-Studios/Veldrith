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
    /// Initializes a new instance of the <see cref="NSString" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public NSString(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the operator IntPtr operation.
    /// </summary>
    /// <param name="nss">The value of nss.</param>
    /// <returns>The result of the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(NSString nss) {
        return nss.NativePtr;
    }

    /// <summary>
    /// Performs the New operation.
    /// </summary>
    /// <param name="s">The value of s.</param>
    /// <returns>The result of the New operation.</returns>
    public static NSString New(string s) {
        NSString nss = s_class.Alloc<NSString>();

        fixed (char* utf16Ptr = s) {
            UIntPtr length = (UIntPtr)s.Length;
            IntPtr newString = IntPtr_objc_msgSend(nss, sel_initWithCharacters, (IntPtr)utf16Ptr, length);
            return new NSString(newString);
        }
    }

    /// <summary>
    /// Performs the GetValue operation.
    /// </summary>
    /// <returns>The result of the GetValue operation.</returns>
    public string GetValue() {
        byte* utf8Ptr = bytePtr_objc_msgSend(this.NativePtr, sel_utf8String);
        return MTLUtil.GetUtf8String(utf8Ptr);
    }

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="NSString">The value of NSString.</param>
    /// <returns>The result of the new operation.</returns>
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