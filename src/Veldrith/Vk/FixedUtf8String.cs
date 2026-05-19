using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.Vk;

internal unsafe class FixedUtf8String : IDisposable {

    /// <summary>
    /// Represents the _numBytes field.
    /// </summary>
    private readonly uint _numBytes;

    /// <summary>
    /// Represents the _handle field.
    /// </summary>
    private GCHandle _handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> class.
    /// </summary>
    public FixedUtf8String(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        int byteCount = Encoding.UTF8.GetByteCount(s);
        byte[] text = new byte[byteCount + 1];
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length - 1; // Includes null terminator
        int encodedCount = Encoding.UTF8.GetBytes(s, 0, s.Length, text, 0);
        Debug.Assert(encodedCount == byteCount);
    }

    /// <summary>
    /// Represents the StringPtr field.
    /// </summary>
    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    #region Disposal

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public void Dispose() {
        this._handle.Free();
    }

    #endregion

    public static implicit operator byte*(FixedUtf8String utf8String) {
        return utf8String.StringPtr;
    }

    /// <summary>
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(FixedUtf8String utf8String) {
        return new IntPtr(utf8String.StringPtr);
    }

    /// <summary>
    /// Executes FixedUtf8String.
    /// </summary>
    public static implicit operator FixedUtf8String(string s) {
        return new FixedUtf8String(s);
    }

    /// <summary>
    /// Executes string.
    /// </summary>
    public static implicit operator string(FixedUtf8String utf8String) {
        return utf8String.GetString();
    }

    /// <summary>
    /// Executes ToString.
    /// </summary>
    public override string ToString() {
        return this.GetString();
    }

    /// <summary>
    /// Executes GetString.
    /// </summary>
    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }
}