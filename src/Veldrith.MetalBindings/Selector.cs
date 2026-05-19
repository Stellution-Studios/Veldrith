using System;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Selector data structure used by the graphics runtime.
/// </summary>
public unsafe struct Selector {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="Selector" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public Selector(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Selector" /> type.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    public Selector(string name) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(name.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8BytesPtr, byteCount);
        }

        this.NativePtr = ObjectiveCRuntime.sel_registerName(utf8BytesPtr);
    }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public string Name {
        get {
            byte* name = ObjectiveCRuntime.sel_getName(this.NativePtr);
            return MTLUtil.GetUtf8String(name);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Selector" /> class.
    /// </summary>
    /// <param name="s">The s value used by this operation.</param>
    public static implicit operator Selector(string s) {
        return new Selector(s);
    }
}