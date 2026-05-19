using System;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the Selector struct.
/// </summary>
public unsafe struct Selector {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="Selector" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public Selector(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Selector" /> type.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
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
    /// Executes the operator Selector operation.
    /// </summary>
    /// <param name="s">Specifies the value of <paramref name="s" />.</param>
    /// <returns>Returns the result produced by the operator Selector operation.</returns>
    public static implicit operator Selector(string s) {
        return new Selector(s);
    }
}