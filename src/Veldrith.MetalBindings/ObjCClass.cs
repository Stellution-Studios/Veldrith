using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the ObjCClass struct.
/// </summary>
public unsafe struct ObjCClass {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(ObjCClass c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjCClass" /> class.
    /// </summary>
    public ObjCClass(string name) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(name.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8BytesPtr, byteCount);
        }

        this.NativePtr = ObjectiveCRuntime.objc_getClass(utf8BytesPtr);
    }

    /// <summary>
    /// Executes GetProperty.
    /// </summary>
    public IntPtr GetProperty(string propertyName) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(propertyName.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = propertyName) {
            Encoding.UTF8.GetBytes(namePtr, propertyName.Length, utf8BytesPtr, byteCount);
        }

        return ObjectiveCRuntime.class_getProperty(this, utf8BytesPtr);
    }

    /// <summary>
    /// Gets or sets Name.
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
    /// Executes class_copyMethodList.
    /// </summary>
    public ObjectiveCMethod* class_copyMethodList(out uint count) {
        return ObjectiveCRuntime.class_copyMethodList(this, out count);
    }
}