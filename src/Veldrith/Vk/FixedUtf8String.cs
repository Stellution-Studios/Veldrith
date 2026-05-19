using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.Vk
{
    internal unsafe class FixedUtf8String : IDisposable
    {
        public byte* StringPtr => (byte*)_handle.AddrOfPinnedObject().ToPointer();
        private GCHandle _handle;
        private readonly uint _numBytes;

        public FixedUtf8String(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            int byteCount = Encoding.UTF8.GetByteCount(s);
            byte[] text = new byte[byteCount + 1];
            _handle = GCHandle.Alloc(text, GCHandleType.Pinned);
            _numBytes = (uint)text.Length - 1; // Includes null terminator
            int encodedCount = Encoding.UTF8.GetBytes(s, 0, s.Length, text, 0);
            Debug.Assert(encodedCount == byteCount);
        }

        #region Disposal

        public void Dispose()
        {
            _handle.Free();
        }

        #endregion

        public static implicit operator byte*(FixedUtf8String utf8String)
        {
            return utf8String.StringPtr;
        }

        public static implicit operator IntPtr(FixedUtf8String utf8String)
        {
            return new IntPtr(utf8String.StringPtr);
        }

        public static implicit operator FixedUtf8String(string s)
        {
            return new FixedUtf8String(s);
        }

        public static implicit operator string(FixedUtf8String utf8String)
        {
            return utf8String.getString();
        }

        public override string ToString()
        {
            return getString();
        }

        private string getString()
        {
            return Encoding.UTF8.GetString(StringPtr, (int)_numBytes);
        }
    }
}
