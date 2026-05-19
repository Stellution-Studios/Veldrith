using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Represents the BoundResourceSetInfo struct.
/// </summary>
internal struct BoundResourceSetInfo : IEquatable<BoundResourceSetInfo> {

    /// <summary>
    /// Represents the Set field.
    /// </summary>
    public ResourceSet Set;

    /// <summary>
    /// Represents the Offsets field.
    /// </summary>
    public SmallFixedOrDynamicArray Offsets;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundResourceSetInfo" /> class.
    /// </summary>
    public BoundResourceSetInfo(ResourceSet set, uint offsetsCount, ref uint offsets) {
        this.Set = set;
        this.Offsets = new SmallFixedOrDynamicArray(offsetsCount, ref offsets);
    }

    /// <summary>
    /// Executes Equals.
    /// </summary>
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

    /// <summary>
    /// Executes Equals.
    /// </summary>
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