using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Defines the data layout and behavior of the BoundResourceSetInfo struct.
/// </summary>
internal struct BoundResourceSetInfo : IEquatable<BoundResourceSetInfo> {

    /// <summary>
    /// Stores the value associated with <c>Set</c>.
    /// </summary>
    public ResourceSet Set;

    /// <summary>
    /// Stores the value associated with <c>Offsets</c>.
    /// </summary>
    public SmallFixedOrDynamicArray Offsets;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundResourceSetInfo" /> type.
    /// </summary>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="offsetsCount">Specifies the value of <paramref name="offsetsCount" />.</param>
    /// <param name="offsets">Specifies the value of <paramref name="offsets" />.</param>
    public BoundResourceSetInfo(ResourceSet set, uint offsetsCount, ref uint offsets) {
        this.Set = set;
        this.Offsets = new SmallFixedOrDynamicArray(offsetsCount, ref offsets);
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="offsetsCount">Specifies the value of <paramref name="offsetsCount" />.</param>
    /// <param name="offsets">Specifies the value of <paramref name="offsets" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
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
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
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