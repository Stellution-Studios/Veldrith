using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="ResourceSet" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct ResourceSetDescription : IEquatable<ResourceSetDescription> {

    /// <summary>
    /// The <see cref="ResourceLayout" /> describing the number and kind of resources used.
    /// </summary>
    public ResourceLayout Layout;

    /// <summary>
    /// An array of <see cref="IBindableResource" /> objects.
    /// </summary>
    public IBindableResource[] BoundResources;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSetDescription" /> type.
    /// </summary>
    /// <param name="layout">The resource layout used by this operation.</param>
    /// <param name="boundResources">The resource involved in this operation.</param>
    public ResourceSetDescription(ResourceLayout layout, params IBindableResource[] boundResources) {
        this.Layout = layout;
        this.BoundResources = boundResources;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(ResourceSetDescription other) {
        return this.Layout.Equals(other.Layout) && Util.ArrayEquals(this.BoundResources, other.BoundResources);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Layout.GetHashCode(), HashHelper.Array(this.BoundResources));
    }
}