using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="DeviceBuffer" />, used in the creation of <see cref="DeviceBuffer" /> objects by a
/// </summary>
public struct BufferDescription : IEquatable<BufferDescription> {

    /// <summary>
    /// The desired capacity, in bytes, of the <see cref="DeviceBuffer" />.
    /// </summary>
    public uint SizeInBytes;

    /// <summary>
    /// Indicates how the <see cref="DeviceBuffer" /> will be used.
    /// </summary>
    public BufferUsage Usage;

    /// <summary>
    /// For structured buffers, this value indicates the size in bytes of a single structure element, and must be non-zero.
    /// </summary>
    public uint StructureByteStride;

    /// <summary>
    /// Indicates that this is a raw buffer. This should be combined with
    /// </summary>
    public bool RawBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferDescription" /> type.
    /// </summary>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    public BufferDescription(uint sizeInBytes, BufferUsage usage) {
        this.SizeInBytes = sizeInBytes;
        this.Usage = usage;
        this.StructureByteStride = 0;
        this.RawBuffer = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferDescription" /> type.
    /// </summary>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="structureByteStride">The structure byte stride value used by this operation.</param>
    public BufferDescription(uint sizeInBytes, BufferUsage usage, uint structureByteStride) {
        this.SizeInBytes = sizeInBytes;
        this.Usage = usage;
        this.StructureByteStride = structureByteStride;
        this.RawBuffer = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferDescription" /> type.
    /// </summary>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="structureByteStride">The structure byte stride value used by this operation.</param>
    /// <param name="rawBuffer">The raw buffer value used by this operation.</param>
    public BufferDescription(uint sizeInBytes, BufferUsage usage, uint structureByteStride, bool rawBuffer) {
        this.SizeInBytes = sizeInBytes;
        this.Usage = usage;
        this.StructureByteStride = structureByteStride;
        this.RawBuffer = rawBuffer;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(BufferDescription other) {
        return this.SizeInBytes.Equals(other.SizeInBytes)
               && this.Usage == other.Usage
               && this.StructureByteStride.Equals(other.StructureByteStride)
               && this.RawBuffer.Equals(other.RawBuffer);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.SizeInBytes.GetHashCode(), (int)this.Usage, this.StructureByteStride.GetHashCode(), this.RawBuffer.GetHashCode());
    }
}