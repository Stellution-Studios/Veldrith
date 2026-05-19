using System;

namespace Veldrith;

/// <summary>
/// A <see cref="IBindableResource" /> that represents a section of a <see cref="DeviceBuffer" />. This can be used in
/// place of
/// a <see cref="DeviceBuffer" /> when creating a <see cref="ResourceSet" /> to make only a subset of the Buffer
/// available to
/// shaders.
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
    /// Constructs a new <see cref="DeviceBufferRange" />.
    /// </summary>
    /// <param name="buffer">The underlying <see cref="DeviceBuffer" /> that this range will refer to.</param>
    /// <param name="offset">The offset, in bytes, from the beginning of the buffer that this range will start at.</param>
    /// <param name="sizeInBytes">The total number of bytes that this range will encompass.</param>
    public DeviceBufferRange(DeviceBuffer buffer, uint offset, uint sizeInBytes) {
        this.Buffer = buffer;
        this.Offset = offset;
        this.SizeInBytes = sizeInBytes;
    }

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements are equal; false otherswise.</returns>
    public bool Equals(DeviceBufferRange other) {
        return this.Buffer == other.Buffer && this.Offset.Equals(other.Offset) && this.SizeInBytes.Equals(other.SizeInBytes);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() {
        int bufferHash = this.Buffer?.GetHashCode() ?? 0;
        return HashHelper.Combine(bufferHash, this.Offset.GetHashCode(), this.SizeInBytes.GetHashCode());
    }
}