using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSString struct.
/// </summary>
public unsafe struct NSString {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSString" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public NSString(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="nss">Specifies the value of <paramref name="nss" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(NSString nss) {
        return nss.NativePtr;
    }

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="s">Specifies the value of <paramref name="s" />.</param>
    /// <returns>Returns the result produced by the New operation.</returns>
    public static NSString New(string s) {
        NSString nss = s_class.Alloc<NSString>();

        fixed (char* utf16Ptr = s) {
            UIntPtr length = (UIntPtr)s.Length;
            IntPtr newString = IntPtr_objc_msgSend(nss, sel_initWithCharacters, (IntPtr)utf16Ptr, length);
            return new NSString(newString);
        }
    }

    /// <summary>
    /// Executes the GetValue operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetValue operation.</returns>
    public string GetValue() {
        byte* utf8Ptr = bytePtr_objc_msgSend(this.NativePtr, sel_utf8String);
        return MTLUtil.GetUtf8String(utf8Ptr);
    }

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="NSString">Specifies the value of <paramref name="NSString" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(NSString));

    /// <summary>
    /// Stores the value associated with <c>sel_initWithCharacters</c>.
    /// </summary>
    private static readonly Selector sel_initWithCharacters = "initWithCharacters:length:";

    /// <summary>
    /// Stores the value associated with <c>sel_utf8String</c>.
    /// </summary>
    private static readonly Selector sel_utf8String = "UTF8String";
}
