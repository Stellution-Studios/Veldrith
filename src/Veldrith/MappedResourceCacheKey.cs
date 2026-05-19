using System;

namespace Veldrith;

/// <summary>
/// Defines the data layout and behavior of the MappedResourceCacheKey struct.
/// </summary>
internal struct MappedResourceCacheKey : IEquatable<MappedResourceCacheKey> {

    /// <summary>
    /// Stores the value associated with <c>Resource</c>.
    /// </summary>
    public readonly IMappableResource Resource;

    /// <summary>
    /// Stores the value associated with <c>Subresource</c>.
    /// </summary>
    public readonly uint Subresource;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResourceCacheKey" /> type.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    public MappedResourceCacheKey(IMappableResource resource, uint subresource) {
        this.Resource = resource;
        this.Subresource = subresource;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(MappedResourceCacheKey other) {
        return this.Resource.Equals(other.Resource)
               && this.Subresource.Equals(other.Subresource);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Resource.GetHashCode(), this.Subresource.GetHashCode());
    }
}