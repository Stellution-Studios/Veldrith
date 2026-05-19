using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.MetalBindings;

internal unsafe class FixedUtf8String : IDisposable {

    /// <summary>
    /// Represents the _handle field.
    /// </summary>
    private GCHandle _handle;

    /// <summary>
    /// Represents the _numBytes field.
    /// </summary>
    private uint _numBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> class.
    /// </summary>
    public FixedUtf8String(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        byte[] text = Encoding.UTF8.GetBytes(s);
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length;
    }

    /// <summary>
    /// Represents the StringPtr field.
    /// </summary>
    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public void Dispose() {
        this._handle.Free();
    }

    /// <summary>
    /// Executes SetText.
    /// </summary>
    public void SetText(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        this._handle.Free();
        byte[] text = Encoding.UTF8.GetBytes(s);
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length;
    }

    /// <summary>
    /// Executes GetString.
    /// </summary>
    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }

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
}