using System;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Selector struct.
/// </summary>
public unsafe struct Selector {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="Selector" /> class.
    /// </summary>
    public Selector(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Selector" /> class.
    /// </summary>
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
    /// Executes Selector.
    /// </summary>
    public static implicit operator Selector(string s) {
        return new Selector(s);
    }
}