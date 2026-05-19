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
    /// <param name="layout">Specifies the value of <paramref name="layout" />.</param>
    /// <param name="boundResources">Specifies the value of <paramref name="boundResources" />.</param>
    public ResourceSetDescription(ResourceLayout layout, params IBindableResource[] boundResources) {
        this.Layout = layout;
        this.BoundResources = boundResources;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(ResourceSetDescription other) {
        return this.Layout.Equals(other.Layout) && Util.ArrayEquals(this.BoundResources, other.BoundResources);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Layout.GetHashCode(), HashHelper.Array(this.BoundResources));
    }
}