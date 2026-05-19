using System;

namespace Veldrith.MetalBindings
{
    public struct MTLOrigin
    {
        public UIntPtr x;
        public UIntPtr y;
        public UIntPtr z;

        public MTLOrigin(uint x, uint y, uint z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}