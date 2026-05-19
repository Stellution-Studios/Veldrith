using System;
using System.Threading;

namespace Veldrith.MTL
{
    internal class MtlFence : Fence
    {
        public ManualResetEvent ResetEvent { get; }

        public override bool Signaled => ResetEvent.WaitOne(0);
        public override bool IsDisposed => _disposed;

        public override string Name { get; set; }
        private bool _disposed;

        public MtlFence(bool signaled)
        {
            ResetEvent = new ManualResetEvent(signaled);
        }

        #region Disposal

        public override void Dispose()
        {
            if (!_disposed)
            {
                ResetEvent.Dispose();
                _disposed = true;
            }
        }

        #endregion

        public void Set()
        {
            ResetEvent.Set();
        }

        public override void Reset()
        {
            ResetEvent.Reset();
        }

        internal bool Wait(ulong nanosecondTimeout)
        {
            ulong timeout = Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000);
            return ResetEvent.WaitOne((int)timeout);
        }
    }
}
