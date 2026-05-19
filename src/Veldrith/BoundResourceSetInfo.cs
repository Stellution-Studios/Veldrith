using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

internal struct BoundResourceSetInfo : IEquatable<BoundResourceSetInfo> {
    public ResourceSet Set;
    public SmallFixedOrDynamicArray Offsets;

    public BoundResourceSetInfo(ResourceSet set, uint offsetsCount, ref uint offsets) {
        this.Set = set;
        this.Offsets = new SmallFixedOrDynamicArray(offsetsCount, ref offsets);
    }

    public bool Equals(ResourceSet set, uint offsetsCount, ref uint offsets) {
        if (set != this.Set || offsetsCount != this.Offsets.Count) {
            return false;
        }

        for (uint i = 0; i < this.Offsets.Count; i++) {
            if (Unsafe.Add(ref offsets, (int)i) != this.Offsets.Get(i)) {
                return false;
            }
        }

        return true;
    }

    public bool Equals(BoundResourceSetInfo other) {
        if (this.Set != other.Set || this.Offsets.Count != other.Offsets.Count) {
            return false;
        }

        for (uint i = 0; i < this.Offsets.Count; i++) {
            if (this.Offsets.Get(i) != other.Offsets.Get(i)) {
                return false;
            }
        }

        return true;
    }
}