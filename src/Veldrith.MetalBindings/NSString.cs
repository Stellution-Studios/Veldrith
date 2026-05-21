using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Provides Objective-C interop bindings for NSString.
/// </summary>
public unsafe struct NSString {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSString" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public NSString(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="nss">The nss value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(NSString nss) {
        return nss.NativePtr;
    }

    /// <summary>
    /// Creates a new Objective-C <see cref="NSString" /> from a managed <see cref="string" /> value.
    /// </summary>
    /// <param name="s">The s value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static NSString New(string s) {
        NSString nss = _sClass.Alloc<NSString>();

        fixed (char* utf16Ptr = s) {
            UIntPtr length = (UIntPtr)s.Length;
            IntPtr newString = IntPtr_objc_msgSend(nss, _selInitWithCharacters, (IntPtr)utf16Ptr, length);
            return new NSString(newString);
        }
    }

    /// <summary>
    /// Gets the value value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public string GetValue() {
        byte* utf8Ptr = BytePtrObjcMsgSend(this.NativePtr, _selUtf8String);
        return MTLUtil.GetUtf8String(utf8Ptr);
    }

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass _sClass = new(nameof(NSString));

    /// <summary>
    /// Stores the sel init with characters state used by this instance.
    /// </summary>
    private static readonly Selector _selInitWithCharacters = "initWithCharacters:length:";

    /// <summary>
    /// Stores the sel utf8 string state used by this instance.
    /// </summary>
    private static readonly Selector _selUtf8String = "UTF8String";
}
