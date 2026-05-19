using System;

namespace Veldrith;

/// <summary>
/// Represents the MappedResourceCacheKey data structure used by the graphics runtime.
/// </summary>
internal struct MappedResourceCacheKey : IEquatable<MappedResourceCacheKey> {

    /// <summary>
    /// Stores the resource state used by this instance.
    /// </summary>
    public readonly IMappableResource Resource;

    /// <summary>
    /// Stores the subresource state used by this instance.
    /// </summary>
    public readonly uint Subresource;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResourceCacheKey" /> type.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    public MappedResourceCacheKey(IMappableResource resource, uint subresource) {
        this.Resource = resource;
        this.Subresource = subresource;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(MappedResourceCacheKey other) {
        return this.Resource.Equals(other.Resource)
               && this.Subresource.Equals(other.Subresource);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Resource.GetHashCode(), this.Subresource.GetHashCode());
    }
}