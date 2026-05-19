using System;

namespace Veldrith;

/// <summary>
///     Describes a <see cref="ResourceSet" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct ResourceSetDescription : IEquatable<ResourceSetDescription> {
    /// <summary>
    ///     The <see cref="ResourceLayout" /> describing the number and kind of resources used.
    /// </summary>
    public ResourceLayout Layout;

    /// <summary>
    ///     An array of <see cref="IBindableResource" /> objects.
    ///     The number and type of resources must match those specified in the <see cref="ResourceLayout" />.
    /// </summary>
    public IBindableResource[] BoundResources;

    /// <summary>
    ///     Constructs a new ResourceSetDescription.
    /// </summary>
    /// <param name="layout">The <see cref="ResourceLayout" /> describing the number and kind of resources used.</param>
    /// <param name="boundResources">
    ///     An array of <see cref="IBindableResource" /> objects.
    ///     The number and type of resources must match those specified in the <see cref="ResourceLayout" />.
    /// </param>
    public ResourceSetDescription(ResourceLayout layout, params IBindableResource[] boundResources) {
        this.Layout = layout;
        this.BoundResources = boundResources;
    }

    /// <summary>
    ///     Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements and all array elements are equal; false otherswise.</returns>
    public bool Equals(ResourceSetDescription other) {
        return this.Layout.Equals(other.Layout) && Util.ArrayEquals(this.BoundResources, other.BoundResources);
    }

    /// <summary>
    ///     Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Layout.GetHashCode(), HashHelper.Array(this.BoundResources));
    }
}