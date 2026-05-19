using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Veldrith.Vk;

/// <summary>
/// A super-dangerous stack-only list which can hold up to 256 bytes of blittable data.
/// </summary>
/// <typeparam name="T">The type of element held in the list. Must be blittable.</typeparam>
internal unsafe struct StackList<T> where T : struct {

    /// <summary>
    /// Represents the CAPACITY_IN_BYTES field.
    /// </summary>
    public const int CAPACITY_IN_BYTES = 256;

    /// <summary>
    /// Represents the _s_sizeof_t field.
    /// </summary>
    private static readonly int _s_sizeof_t = Unsafe.SizeOf<T>();

    private fixed byte _storage[CAPACITY_IN_BYTES];

    /// <summary>
    /// Gets or sets Count.
    /// </summary>
    public uint Count { get; private set; }

    /// <summary>
    /// Performs the AsPointer operation.
    /// </summary>
    /// <param name="this">The value of this.</param>
    /// <returns>The result of the AsPointer operation.</returns>
    public void* Data => Unsafe.AsPointer(ref this);

    /// <summary>
    /// Performs the Add operation.
    /// </summary>
    /// <param name="item">The value of item.</param>
    public void Add(T item) {
        byte* basePtr = (byte*)this.Data;
        int offset = (int)(this.Count * _s_sizeof_t);
#if DEBUG
        Debug.Assert(offset + _s_sizeof_t <= CAPACITY_IN_BYTES);
#endif
        Unsafe.Write(basePtr + offset, item);

        this.Count += 1;
    }

    /// <summary>
    /// Gets or sets this[uint index].
    /// </summary>
    public ref T this[uint index] {
        get {
            byte* basePtr = (byte*)Unsafe.AsPointer(ref this);
            int offset = (int)(index * _s_sizeof_t);
            return ref Unsafe.AsRef<T>(basePtr + offset);
        }
    }

    /// <summary>
    /// Gets or sets this[int index].
    /// </summary>
    public ref T this[int index] {
        get {
            byte* basePtr = (byte*)Unsafe.AsPointer(ref this);
            int offset = index * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(basePtr + offset);
        }
    }
}

/// <summary>
/// A super-dangerous stack-only list which can hold a number of bytes determined by the second type parameter.
/// </summary>
/// <typeparam name="T">The type of element held in the list. Must be blittable.</typeparam>
/// <typeparam name="TSize">A type parameter dictating the capacity of the list.</typeparam>
internal unsafe struct StackList<T, TSize> where T : struct where TSize : struct {

    /// <summary>
    /// Represents the _s_sizeof_t field.
    /// </summary>
    private static readonly int _s_sizeof_t = Unsafe.SizeOf<T>();

#pragma warning disable 0169 // Unused field. This is used implicity because it controls the size of the structure on the stack.

    /// <summary>
    /// Represents the _storage field.
    /// </summary>
    private TSize _storage;
#pragma warning restore 0169

    /// <summary>
    /// Gets or sets Count.
    /// </summary>
    public uint Count { get; private set; }

    /// <summary>
    /// Performs the AsPointer operation.
    /// </summary>
    /// <param name="this">The value of this.</param>
    /// <returns>The result of the AsPointer operation.</returns>
    public void* Data => Unsafe.AsPointer(ref this);

    /// <summary>
    /// Performs the Add operation.
    /// </summary>
    /// <param name="item">The value of item.</param>
    public void Add(T item) {
        ref T dest = ref Unsafe.Add(ref Unsafe.As<TSize, T>(ref this._storage), (int)this.Count);
#if DEBUG
        int offset = (int)(this.Count * _s_sizeof_t);
        Debug.Assert(offset + _s_sizeof_t <= Unsafe.SizeOf<TSize>());
#endif
        dest = item;

        this.Count += 1;
    }

    /// <summary>
    /// Performs the Add operation.
    /// </summary>
    /// <param name="Data">The value of Data.</param>
    /// <param name="index">The value of index.</param>
    /// <returns>The result of the Add operation.</returns>
    public ref T this[int index] => ref Unsafe.Add(ref Unsafe.AsRef<T>(this.Data), index);

    /// <summary>
    /// Performs the Add operation.
    /// </summary>
    /// <param name="Data">The value of Data.</param>
    /// <param name="int">The value of int.</param>
    /// <returns>The result of the Add operation.</returns>
    public ref T this[uint index] => ref Unsafe.Add(ref Unsafe.AsRef<T>(this.Data), (int)index);
}

/// <summary>
/// Represents the Size16Bytes struct.
/// </summary>
internal unsafe struct Size16Bytes {

    public fixed byte Data[16];
}

/// <summary>
/// Represents the Size64Bytes struct.
/// </summary>
internal unsafe struct Size64Bytes {

    public fixed byte Data[64];
}

/// <summary>
/// Represents the Size128Bytes struct.
/// </summary>
internal unsafe struct Size128Bytes {

    public fixed byte Data[64];
}

/// <summary>
/// Represents the Size512Bytes struct.
/// </summary>
internal unsafe struct Size512Bytes {

    public fixed byte Data[1024];
}

/// <summary>
/// Represents the Size1024Bytes struct.
/// </summary>
internal unsafe struct Size1024Bytes {

    public fixed byte Data[1024];
}

/// <summary>
/// Represents the Size2048Bytes struct.
/// </summary>
internal unsafe struct Size2048Bytes {

    public fixed byte Data[2048];
}
#pragma warning disable 0649 // Fields are not assigned directly -- expected.

/// <summary>
/// Represents the Size2IntPtr struct.
/// </summary>
internal struct Size2IntPtr {

    /// <summary>
    /// Represents the First field.
    /// </summary>
    public IntPtr First;

    /// <summary>
    /// Represents the Second field.
    /// </summary>
    public IntPtr Second;
}

/// <summary>
/// Represents the Size6IntPtr struct.
/// </summary>
internal struct Size6IntPtr {

    /// <summary>
    /// Represents the First field.
    /// </summary>
    public IntPtr First;

    /// <summary>
    /// Represents the Second field.
    /// </summary>
    public IntPtr Second;

    /// <summary>
    /// Represents the Third field.
    /// </summary>
    public IntPtr Third;

    /// <summary>
    /// Represents the Fourth field.
    /// </summary>
    public IntPtr Fourth;

    /// <summary>
    /// Represents the Fifth field.
    /// </summary>
    public IntPtr Fifth;

    /// <summary>
    /// Represents the Sixth field.
    /// </summary>
    public IntPtr Sixth;
}
#pragma warning restore 0649