using System;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    internal sealed class D3D12Fence : Fence
    {
        private readonly ID3D12Fence nativeFence;
        private readonly AutoResetEvent waitEvent;
        private ulong fenceValue;
        private bool disposed;
        private string name;

        public D3D12Fence(D3D12GraphicsDevice gd, bool signaled)
        {
            ulong initialValue = signaled ? 1UL : 0UL;
            nativeFence = gd.Device.CreateFence(initialValue, FenceFlags.None);
            waitEvent = new AutoResetEvent(false);
            fenceValue = initialValue;
        }

        public override bool Signaled => nativeFence.CompletedValue >= fenceValue;
        public override bool IsDisposed => disposed;

        public override string Name
        {
            get => name;
            set => name = value;
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            waitEvent.Dispose();
            nativeFence.Dispose();
        }

        public override void Reset()
        {
            fenceValue = nativeFence.CompletedValue + 1;
        }

        internal void Signal(ID3D12CommandQueue commandQueue)
        {
            if (fenceValue <= nativeFence.CompletedValue)
            {
                fenceValue = nativeFence.CompletedValue + 1;
            }

            commandQueue.Signal(nativeFence, fenceValue).CheckError();
        }

        internal bool Wait(ulong nanosecondTimeout)
        {
            if (Signaled)
            {
                return true;
            }

            nativeFence.SetEventOnCompletion(fenceValue, waitEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();

            if (nanosecondTimeout == ulong.MaxValue)
            {
                waitEvent.WaitOne();
                return true;
            }

            int milliseconds = nanosecondTimeout > 0
                ? (int)Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000)
                : 0;
            return waitEvent.WaitOne(milliseconds);
        }
    }
}
