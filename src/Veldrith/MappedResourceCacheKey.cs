using System;

namespace Veldrith;

/// <summary>
/// Represents the MappedResourceCacheKey struct.
/// </summary>
internal struct MappedResourceCacheKey : IEquatable<MappedResourceCacheKey> {

    /// <summary>
    /// Represents the Resource field.
    /// </summary>
    public readonly IMappableResource Resource;

    /// <summary>
    /// Represents the Subresource field.
    /// </summary>
    public readonly uint Subresource;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResourceCacheKey" /> type.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="subresource">The value of subresource.</param>
    public MappedResourceCacheKey(IMappableResource resource, uint subresource) {
        this.Resource = resource;
        this.Subresource = subresource;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(MappedResourceCacheKey other) {
        return this.Resource.Equals(other.Resource)
               && this.Subresource.Equals(other.Subresource);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Resource.GetHashCode(), this.Subresource.GetHashCode());
    }
}