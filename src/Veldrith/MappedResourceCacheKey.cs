using System;

namespace Veldrith;

internal struct MappedResourceCacheKey : IEquatable<MappedResourceCacheKey> {
    public readonly IMappableResource Resource;
    public readonly uint Subresource;

    public MappedResourceCacheKey(IMappableResource resource, uint subresource) {
        this.Resource = resource;
        this.Subresource = subresource;
    }

    public bool Equals(MappedResourceCacheKey other) {
        return this.Resource.Equals(other.Resource)
               && this.Subresource.Equals(other.Subresource);
    }

    public override int GetHashCode() {
        return HashHelper.Combine(this.Resource.GetHashCode(), this.Subresource.GetHashCode());
    }
}