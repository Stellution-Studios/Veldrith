using System;

namespace Veldrith;

/// <summary>
/// A bitmask describing the permitted uses of a <see cref="DeviceBuffer" /> object.
/// </summary>
[Flags]
public enum BufferUsage : byte {

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> can be used as the source of vertex data for drawing commands.
    /// </summary>
    VertexBuffer = 1 << 0,

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> can be used as the source of index data for drawing commands.
    /// </summary>
    IndexBuffer = 1 << 1,

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> can be used as a uniform Buffer.
    /// </summary>
    UniformBuffer = 1 << 2,

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> can be used as a read-only structured Buffer.
    /// May be combined with <see cref="VertexBuffer" />, <see cref="IndexBuffer" />, and <see cref="IndirectBuffer" />.
    /// </summary>
    StructuredBufferReadOnly = 1 << 3,

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> can be used as a read-write structured Buffer.
    /// May be combined with <see cref="VertexBuffer" />, <see cref="IndexBuffer" />, and <see cref="IndirectBuffer" />.
    /// </summary>
    StructuredBufferReadWrite = 1 << 4,

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> can be used as the source of indirect drawing information.
    /// </summary>
    IndirectBuffer = 1 << 5,

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> will be updated with new data very frequently. Dynamic Buffers can be
    /// combined with most usages except <see cref="StructuredBufferReadWrite" /> and <see cref="IndirectBuffer" />.
    /// </summary>
    Dynamic = 1 << 6,

    /// <summary>
    /// Indicates that a <see cref="DeviceBuffer" /> will be used as a staging Buffer. Staging Buffers can be used to
    /// </summary>
    Staging = 1 << 7
}
