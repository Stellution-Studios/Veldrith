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
    /// Initializes a new instance of the <see cref="MappedResourceCacheKey" /> class.
    /// </summary>
    public MappedResourceCacheKey(IMappableResource resource, uint subresource) {
        this.Resource = resource;
        this.Subresource = subresource;
    }

    /// <summary>
    /// Executes Equals.
    /// </summary>
    public bool Equals(MappedResourceCacheKey other) {
        return this.Resource.Equals(other.Resource)
               && this.Subresource.Equals(other.Subresource);
    }

    /// <summary>
    /// Executes GetHashCode.
    /// </summary>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Resource.GetHashCode(), this.Subresource.GetHashCode());
    }
}