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
    /// <param name="elements">Specifies the value of <paramref name="elements" />.</param>
    public ResourceLayoutDescription(params ResourceLayoutElementDescription[] elements) {
        this.Elements = elements;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(ResourceLayoutDescription other) {
        return Util.ArrayEqualsEquatable(this.Elements, other.Elements);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Array(this.Elements);
    }
}