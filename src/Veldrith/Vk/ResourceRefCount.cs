using System;
using System.Threading;

namespace Veldrith.Vk
{
    internal class ResourceRefCount
    {
        private readonly Action disposeAction;
        private int refCount;

        public ResourceRefCount(Action disposeAction)
        {
            this.disposeAction = disposeAction;
            refCount = 1;
        }

        public int Increment()
        {
            int ret = Interlocked.Increment(ref refCount);
#if VALIDATE_USAGE
            if (ret == 0) throw new VeldridException("An attempt was made to reference a disposed resource.");
#endif
            return ret;
        }

        public int Decrement()
        {
            int ret = Interlocked.Decrement(ref refCount);
            if (ret == 0) disposeAction();

            return ret;
        }
    }
}
