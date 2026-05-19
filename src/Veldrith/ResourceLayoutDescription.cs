using System;

namespace Veldrith;

/// <summary>
/// Describes the layout of <see cref="IBindableResource" /> objects for a <see cref="Pipeline" />.
/// </summary>
public struct ResourceLayoutDescription : IEquatable<ResourceLayoutDescription> {

    /// <summary>
    /// An array of <see cref="ResourceLayoutElementDescription" /> objects, describing the properties of each resource
    /// element in the <see cref="ResourceLayout" />.
    /// </summary>
    public ResourceLayoutElementDescription[] Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLayoutDescription" /> type.
    /// </summary>
    /// <param name="elements">The value of elements.</param>
    public ResourceLayoutDescription(params ResourceLayoutElementDescription[] elements) {
        this.Elements = elements;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(ResourceLayoutDescription other) {
        return Util.ArrayEqualsEquatable(this.Elements, other.Elements);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Array(this.Elements);
    }
}