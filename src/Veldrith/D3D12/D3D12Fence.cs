using System;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrith.D3D12
{
    internal sealed class D3D12Fence : Fence
    {
        private readonly ID3D12Fence _nativeFence;
        private readonly AutoResetEvent _waitEvent;
        private ulong _fenceValue;
        private bool _disposed;
        private string _name;

        public D3D12Fence(D3D12GraphicsDevice gd, bool signaled)
        {
            ulong initialValue = signaled ? 1UL : 0UL;
            _nativeFence = gd.Device.CreateFence(initialValue, FenceFlags.None);
            _waitEvent = new AutoResetEvent(false);
            _fenceValue = initialValue;
        }

        public override bool Signaled => _nativeFence.CompletedValue >= _fenceValue;
        public override bool IsDisposed => _disposed;

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _waitEvent.Dispose();
            _nativeFence.Dispose();
        }

        public override void Reset()
        {
            _fenceValue = _nativeFence.CompletedValue + 1;
        }

        internal void Signal(ID3D12CommandQueue commandQueue)
        {
            if (_fenceValue <= _nativeFence.CompletedValue)
            {
                _fenceValue = _nativeFence.CompletedValue + 1;
            }

            commandQueue.Signal(_nativeFence, _fenceValue).CheckError();
        }

        internal bool Wait(ulong nanosecondTimeout)
        {
            if (Signaled)
            {
                return true;
            }

            _nativeFence.SetEventOnCompletion(_fenceValue, _waitEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();

            if (nanosecondTimeout == ulong.MaxValue)
            {
                _waitEvent.WaitOne();
                return true;
            }

            int milliseconds = nanosecondTimeout > 0
                ? (int)Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000)
                : 0;
            return _waitEvent.WaitOne(milliseconds);
        }
    }
}
