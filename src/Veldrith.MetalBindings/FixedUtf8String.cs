using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the FixedUtf8String class.
/// </summary>
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
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> type.
    /// </summary>
    /// <param name="s">The value of s.</param>
    public FixedUtf8String(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        byte[] text = Encoding.UTF8.GetBytes(s);
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length;
    }

    /// <summary>
    /// Performs the AddrOfPinnedObject operation.
    /// </summary>
    /// <returns>The result of the AddrOfPinnedObject operation.</returns>
    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public void Dispose() {
        this._handle.Free();
    }

    /// <summary>
    /// Performs the SetText operation.
    /// </summary>
    /// <param name="s">The value of s.</param>
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
    /// Performs the GetString operation.
    /// </summary>
    /// <returns>The result of the GetString operation.</returns>
    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }

    /// <summary>
    /// Performs the operator byte* operation.
    /// </summary>
    /// <param name="utf8String">The value of utf8String.</param>
    /// <returns>The result of the operator byte* operation.</returns>
    public static implicit operator byte*(FixedUtf8String utf8String) {
        return utf8String.StringPtr;
    }

    /// <summary>
    /// Performs the operator IntPtr operation.
    /// </summary>
    /// <param name="utf8String">The value of utf8String.</param>
    /// <returns>The result of the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(FixedUtf8String utf8String) {
        return new IntPtr(utf8String.StringPtr);
    }

    /// <summary>
    /// Performs the operator FixedUtf8String operation.
    /// </summary>
    /// <param name="s">The value of s.</param>
    /// <returns>The result of the operator FixedUtf8String operation.</returns>
    public static implicit operator FixedUtf8String(string s) {
        return new FixedUtf8String(s);
    }

    /// <summary>
    /// Performs the operator string operation.
    /// </summary>
    /// <param name="utf8String">The value of utf8String.</param>
    /// <returns>The result of the operator string operation.</returns>
    public static implicit operator string(FixedUtf8String utf8String) {
        return utf8String.GetString();
    }
}