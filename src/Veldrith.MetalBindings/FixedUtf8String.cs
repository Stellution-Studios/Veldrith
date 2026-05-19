using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the behavior and responsibilities of the FixedUtf8String class.
/// </summary>
internal unsafe class FixedUtf8String : IDisposable {

    /// <summary>
    /// Stores the value associated with <c>_handle</c>.
    /// </summary>
    private GCHandle _handle;

    /// <summary>
    /// Stores the value associated with <c>_numBytes</c>.
    /// </summary>
    private uint _numBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedUtf8String" /> type.
    /// </summary>
    /// <param name="s">Specifies the value of <paramref name="s" />.</param>
    public FixedUtf8String(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        byte[] text = Encoding.UTF8.GetBytes(s);
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length;
    }

    /// <summary>
    /// Executes the AddrOfPinnedObject operation.
    /// </summary>
    /// <returns>Returns the result produced by the AddrOfPinnedObject operation.</returns>
    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose() {
        this._handle.Free();
    }

    /// <summary>
    /// Executes the SetText operation.
    /// </summary>
    /// <param name="s">Specifies the value of <paramref name="s" />.</param>
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
    /// Executes the GetString operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetString operation.</returns>
    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }

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
}