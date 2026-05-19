using System;

namespace Veldrith.MetalBindings
{
    public struct MTLScissorRect : IEquatable<MTLScissorRect>
    {
        public UIntPtr x;
        public UIntPtr y;
        public UIntPtr width;
        public UIntPtr height;

        public MTLScissorRect(uint x, uint y, uint width, uint height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool Equals(MTLScissorRect other)
            => x == other.x
               && y == other.y
               && width == other.width
               && height == other.height;
    }
}
