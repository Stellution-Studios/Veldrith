using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.MetalBindings;

internal unsafe class FixedUtf8String : IDisposable {
    private GCHandle _handle;
    private uint _numBytes;

    public FixedUtf8String(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        byte[] text = Encoding.UTF8.GetBytes(s);
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length;
    }

    public byte* StringPtr => (byte*)this._handle.AddrOfPinnedObject().ToPointer();

    public void Dispose() {
        this._handle.Free();
    }

    public void SetText(string s) {
        if (s == null) {
            throw new ArgumentNullException(nameof(s));
        }

        this._handle.Free();
        byte[] text = Encoding.UTF8.GetBytes(s);
        this._handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        this._numBytes = (uint)text.Length;
    }

    private string GetString() {
        return Encoding.UTF8.GetString(this.StringPtr, (int)this._numBytes);
    }

    public static implicit operator byte*(FixedUtf8String utf8String) {
        return utf8String.StringPtr;
    }

    public static implicit operator IntPtr(FixedUtf8String utf8String) {
        return new IntPtr(utf8String.StringPtr);
    }

    public static implicit operator FixedUtf8String(string s) {
        return new FixedUtf8String(s);
    }

    public static implicit operator string(FixedUtf8String utf8String) {
        return utf8String.GetString();
    }
}