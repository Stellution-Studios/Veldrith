using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the FixedUtf8String type used by the graphics runtime.
/// </summary>
internal unsafe class FixedUtf8String : IDisposable {

    /// <summary>
    /// Stores the handle state used by this instance.
    /// </summary>
    private GCHandle _handle;

    /// <summary>
    /// Stores the num bytes state used by this instance.
    /// </summary>
    private uint _numBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> type.
    /// </summary>
    /// <param name="s">The s value used by this operation.</param>
    public FixedUtf8String(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        byte[] text = Encoding.UTF8.GetBytes(s);
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length;
    }

    /// <summary>
    /// Executes the addr of pinned object logic for this backend.
    /// </summary>
    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public void Dispose() {
        this._handle.Free();
    }

    /// <summary>
    /// Sets the text value.
    /// </summary>
    /// <param name="s">The s value used by this operation.</param>
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
    /// Gets the string value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }

    /// <summary>
    /// Executes the value logic for this backend.
    /// </summary>
    /// <param name="utf8String">The utf8 string value used by this operation.</param>
    public static implicit operator byte*(FixedUtf8String utf8String) {
        return utf8String.StringPtr;
    }

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="utf8String">The utf8 string value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(FixedUtf8String utf8String) {
        return new IntPtr(utf8String.StringPtr);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> class.
    /// </summary>
    /// <param name="s">The s value used by this operation.</param>
    public static implicit operator FixedUtf8String(string s) {
        return new FixedUtf8String(s);
    }

    /// <summary>
    /// Executes the string logic for this backend.
    /// </summary>
    /// <param name="utf8String">The utf8 string value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator string(FixedUtf8String utf8String) {
        return utf8String.GetString();
    }
}