using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Represents the BoundResourceSetInfo data structure used by the graphics runtime.
/// </summary>
internal struct BoundResourceSetInfo : IEquatable<BoundResourceSetInfo> {

    /// <summary>
    /// Stores the set state used by this instance.
    /// </summary>
    public ResourceSet Set;

    /// <summary>
    /// Stores the offsets value used during command execution.
    /// </summary>
    public SmallFixedOrDynamicArray Offsets;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundResourceSetInfo" /> type.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="offsetsCount">The offsets count value used by this operation.</param>
    /// <param name="offsets">The offsets value used by this operation.</param>
    public BoundResourceSetInfo(ResourceSet set, uint offsetsCount, ref uint offsets) {
        this.Set = set;
        this.Offsets = new SmallFixedOrDynamicArray(offsetsCount, ref offsets);
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="offsetsCount">The offsets count value used by this operation.</param>
    /// <param name="offsets">The offsets value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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