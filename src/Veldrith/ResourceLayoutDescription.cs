using System;

namespace Veldrith;

/// <summary>
/// Describes the layout of <see cref="IBindableResource" /> objects for a <see cref="Pipeline" />.
/// </summary>
public struct ResourceLayoutDescription : IEquatable<ResourceLayoutDescription> {

    /// <summary>
    /// An array of <see cref="ResourceLayoutElementDescription" /> objects, describing the properties of each resource
    /// </summary>
    public ResourceLayoutElementDescription[] Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLayoutDescription" /> type.
    /// </summary>
    /// <param name="elements">The elements value used by this operation.</param>
    public ResourceLayoutDescription(params ResourceLayoutElementDescription[] elements) {
        this.Elements = elements;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(ResourceLayoutDescription other) {
        return Util.ArrayEqualsEquatable(this.Elements, other.Elements);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Array(this.Elements);
    }
}