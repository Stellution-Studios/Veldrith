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
    /// The number and type of resources must match those specified in the <see cref="ResourceLayout" />.
    /// </summary>
    public IBindableResource[] BoundResources;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSetDescription" /> type.
    /// </summary>
    /// <param name="layout">The value of layout.</param>
    /// <param name="boundResources">The value of boundResources.</param>
    public ResourceSetDescription(ResourceLayout layout, params IBindableResource[] boundResources) {
        this.Layout = layout;
        this.BoundResources = boundResources;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(ResourceSetDescription other) {
        return this.Layout.Equals(other.Layout) && Util.ArrayEquals(this.BoundResources, other.BoundResources);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Layout.GetHashCode(), HashHelper.Array(this.BoundResources));
    }
}