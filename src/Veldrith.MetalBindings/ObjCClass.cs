using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the ObjCClass struct.
/// </summary>
public unsafe struct ObjCClass {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(ObjCClass c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjCClass" /> type.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    public ObjCClass(string name) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(name.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8BytesPtr, byteCount);
        }

        this.NativePtr = ObjectiveCRuntime.objc_getClass(utf8BytesPtr);
    }

    /// <summary>
    /// Executes the GetProperty operation.
    /// </summary>
    /// <param name="propertyName">Specifies the value of <paramref name="propertyName" />.</param>
    /// <returns>Returns the result produced by the GetProperty operation.</returns>
    public IntPtr GetProperty(string propertyName) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(propertyName.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = propertyName) {
            Encoding.UTF8.GetBytes(namePtr, propertyName.Length, utf8BytesPtr, byteCount);
        }

        return ObjectiveCRuntime.class_getProperty(this, utf8BytesPtr);
    }

    /// <summary>
    /// Executes the GetUtf8String operation.
    /// </summary>
    /// <param name="this">Specifies the value of <paramref name="this" />.</param>
    /// <returns>Returns the result produced by the GetUtf8String operation.</returns>
    public string Name => MTLUtil.GetUtf8String(ObjectiveCRuntime.class_getName(this));

    public T Alloc<T>() where T : struct {
        IntPtr value = ObjectiveCRuntime.IntPtr_objc_msgSend(this.NativePtr, Selectors.alloc);
        return Unsafe.AsRef<T>(&value);
    }

    public T AllocInit<T>() where T : struct {
        IntPtr value = ObjectiveCRuntime.IntPtr_objc_msgSend(this.NativePtr, Selectors.alloc);
        ObjectiveCRuntime.objc_msgSend(value, Selectors.init);
        return Unsafe.AsRef<T>(&value);
    }

    /// <summary>
    /// Executes the class_copyMethodList operation.
    /// </summary>
    /// <param name="count">Specifies the value of <paramref name="count" />.</param>
    /// <returns>Returns the result produced by the class_copyMethodList operation.</returns>
    public ObjectiveCMethod* class_copyMethodList(out uint count) {
        return ObjectiveCRuntime.class_copyMethodList(this, out count);
    }
}