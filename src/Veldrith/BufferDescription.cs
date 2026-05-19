using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="DeviceBuffer" />, used in the creation of <see cref="DeviceBuffer" /> objects by a
/// <see cref="ResourceFactory" />.
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
    /// For all other buffer types, this value must be zero.
    /// </summary>
    public uint StructureByteStride;

    /// <summary>
    /// Indicates that this is a raw buffer. This should be combined with
    /// <see cref="BufferUsage.StructuredBufferReadWrite" />.
    /// </summary>
    public bool RawBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferDescription" /> type.
    /// </summary>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="usage">The value of usage.</param>
    public BufferDescription(uint sizeInBytes, BufferUsage usage) {
        this.SizeInBytes = sizeInBytes;
        this.Usage = usage;
        this.StructureByteStride = 0;
        this.RawBuffer = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferDescription" /> type.
    /// </summary>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="structureByteStride">The value of structureByteStride.</param>
    public BufferDescription(uint sizeInBytes, BufferUsage usage, uint structureByteStride) {
        this.SizeInBytes = sizeInBytes;
        this.Usage = usage;
        this.StructureByteStride = structureByteStride;
        this.RawBuffer = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferDescription" /> type.
    /// </summary>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="structureByteStride">The value of structureByteStride.</param>
    /// <param name="rawBuffer">The value of rawBuffer.</param>
    public BufferDescription(uint sizeInBytes, BufferUsage usage, uint structureByteStride, bool rawBuffer) {
        this.SizeInBytes = sizeInBytes;
        this.Usage = usage;
        this.StructureByteStride = structureByteStride;
        this.RawBuffer = rawBuffer;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(BufferDescription other) {
        return this.SizeInBytes.Equals(other.SizeInBytes)
               && this.Usage == other.Usage
               && this.StructureByteStride.Equals(other.StructureByteStride)
               && this.RawBuffer.Equals(other.RawBuffer);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.SizeInBytes.GetHashCode(), (int)this.Usage, this.StructureByteStride.GetHashCode(), this.RawBuffer.GetHashCode());
    }
}