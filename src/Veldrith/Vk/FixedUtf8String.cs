using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the FixedUtf8String class.
/// </summary>
internal unsafe class FixedUtf8String : IDisposable {

    /// <summary>
    /// Stores the value associated with <c>_numBytes</c>.
    /// </summary>
    private readonly uint _numBytes;

    /// <summary>
    /// Stores the value associated with <c>_handle</c>.
    /// </summary>
    private GCHandle _handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> type.
    /// </summary>
    /// <param name="s">Specifies the value of <paramref name="s" />.</param>
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
    /// Executes the AddrOfPinnedObject operation.
    /// </summary>
    /// <returns>Returns the result produced by the AddrOfPinnedObject operation.</returns>
    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    #region Disposal

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose() {
        this._handle.Free();
    }

    #endregion

    /// <summary>
    /// Executes the operator byte* operation.
    /// </summary>
    /// <param name="utf8String">Specifies the value of <paramref name="utf8String" />.</param>
    /// <returns>Returns the result produced by the operator byte* operation.</returns>
    public static implicit operator byte*(FixedUtf8String utf8String) {
        return utf8String.StringPtr;
    }

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="utf8String">Specifies the value of <paramref name="utf8String" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(FixedUtf8String utf8String) {
        return new IntPtr(utf8String.StringPtr);
    }

    /// <summary>
    /// Executes the operator FixedUtf8String operation.
    /// </summary>
    /// <param name="s">Specifies the value of <paramref name="s" />.</param>
    /// <returns>Returns the result produced by the operator FixedUtf8String operation.</returns>
    public static implicit operator FixedUtf8String(string s) {
        return new FixedUtf8String(s);
    }

    /// <summary>
    /// Executes the operator string operation.
    /// </summary>
    /// <param name="utf8String">Specifies the value of <paramref name="utf8String" />.</param>
    /// <returns>Returns the result produced by the operator string operation.</returns>
    public static implicit operator string(FixedUtf8String utf8String) {
        return utf8String.GetString();
    }

    /// <summary>
    /// Executes the ToString operation.
    /// </summary>
    /// <returns>Returns the result produced by the ToString operation.</returns>
    public override string ToString() {
        return this.GetString();
    }

    /// <summary>
    /// Executes the GetString operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetString operation.</returns>
    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }
}