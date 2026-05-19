using System;

namespace Veldrith;

/// <summary>
/// A <see cref="IBindableResource" /> that represents a section of a <see cref="DeviceBuffer" />. This can be used in
/// </summary>
public struct DeviceBufferRange : IBindableResource, IEquatable<DeviceBufferRange> {

    /// <summary>
    /// The underlying <see cref="DeviceBuffer" /> that this range refers to.
    /// </summary>
    public DeviceBuffer Buffer;

    /// <summary>
    /// The offset, in bytes, from the beginning of the buffer that this range starts at.
    /// </summary>
    public uint Offset;

    /// <summary>
    /// The total number of bytes that this range encompasses.
    /// </summary>
    public uint SizeInBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceBufferRange" /> type.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    public DeviceBufferRange(DeviceBuffer buffer, uint offset, uint sizeInBytes) {
        this.Buffer = buffer;
        this.Offset = offset;
        this.SizeInBytes = sizeInBytes;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(DeviceBufferRange other) {
        return this.Buffer == other.Buffer && this.Offset.Equals(other.Offset) && this.SizeInBytes.Equals(other.SizeInBytes);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        int bufferHash = this.Buffer?.GetHashCode() ?? 0;
        return HashHelper.Combine(bufferHash, this.Offset.GetHashCode(), this.SizeInBytes.GetHashCode());
    }
}