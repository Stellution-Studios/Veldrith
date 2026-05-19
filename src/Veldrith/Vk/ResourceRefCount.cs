using System;
using System.Threading;

namespace Veldrith.Vk
{
    internal class ResourceRefCount
    {
        private readonly Action disposeAction;
        private int _refCount;

        public ResourceRefCount(Action disposeAction)
        {
            this.disposeAction = disposeAction;
            _refCount = 1;
        }

        public int Increment()
        {
            int ret = Interlocked.Increment(ref _refCount);
#if VALIDATE_USAGE
            if (ret == 0) throw new VeldridException("An attempt was made to reference a disposed resource.");
#endif
            return ret;
        }

        public int Decrement()
        {
            int ret = Interlocked.Decrement(ref _refCount);
            if (ret == 0) disposeAction();

            return ret;
        }
    }
}
