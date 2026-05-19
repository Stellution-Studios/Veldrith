using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Provides Objective-C interop bindings for ObjCClass.
/// </summary>
public unsafe struct ObjCClass {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="c">The c value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(ObjCClass c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjCClass" /> type.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    public ObjCClass(string name) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(name.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8BytesPtr, byteCount);
        }

        this.NativePtr = ObjectiveCRuntime.objc_getClass(utf8BytesPtr);
    }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    /// <param name="propertyName">The property name value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public IntPtr GetProperty(string propertyName) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(propertyName.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = propertyName) {
            Encoding.UTF8.GetBytes(namePtr, propertyName.Length, utf8BytesPtr, byteCount);
        }

        return ObjectiveCRuntime.class_getProperty(this, utf8BytesPtr);
    }

    /// <summary>
    /// Gets the utf8 string value.
    /// </summary>
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
    /// Executes the class copy method list logic for this backend.
    /// </summary>
    /// <param name="count">The number of items involved in this operation.</param>
    public ObjectiveCMethod* class_copyMethodList(out uint count) {
        return ObjectiveCRuntime.class_copyMethodList(this, out count);
    }
}