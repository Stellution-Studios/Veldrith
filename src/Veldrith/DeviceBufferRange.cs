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
    /// Initializes a new instance of the <see cref="DeviceBufferRange" /> type.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    public DeviceBufferRange(DeviceBuffer buffer, uint offset, uint sizeInBytes) {
        this.Buffer = buffer;
        this.Offset = offset;
        this.SizeInBytes = sizeInBytes;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(DeviceBufferRange other) {
        return this.Buffer == other.Buffer && this.Offset.Equals(other.Offset) && this.SizeInBytes.Equals(other.SizeInBytes);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        int bufferHash = this.Buffer?.GetHashCode() ?? 0;
        return HashHelper.Combine(bufferHash, this.Offset.GetHashCode(), this.SizeInBytes.GetHashCode());
    }
}