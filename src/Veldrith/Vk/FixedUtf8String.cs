using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.Vk;

/// <summary>
/// Represents the FixedUtf8String class.
/// </summary>
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
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> type.
    /// </summary>
    /// <param name="s">The value of s.</param>
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
    /// Performs the AddrOfPinnedObject operation.
    /// </summary>
    /// <returns>The result of the AddrOfPinnedObject operation.</returns>
    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    #region Disposal

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public void Dispose() {
        this._handle.Free();
    }

    #endregion

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

    /// <summary>
    /// Performs the ToString operation.
    /// </summary>
    /// <returns>The result of the ToString operation.</returns>
    public override string ToString() {
        return this.GetString();
    }

    /// <summary>
    /// Performs the GetString operation.
    /// </summary>
    /// <returns>The result of the GetString operation.</returns>
    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }
}