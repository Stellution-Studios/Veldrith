namespace Veldrith;

/// <summary>
/// The kind of a <see cref="IBindableResource" /> object.
/// </summary>
public enum ResourceKind : byte {

    /// <summary>
    /// A <see cref="DeviceBuffer" /> accessed as a uniform buffer. A subset of a buffer can be bound using a
    /// </summary>
    UniformBuffer,

    /// <summary>
    /// A <see cref="DeviceBuffer" /> accessed as a read-only storage buffer. A subset of a buffer can be bound using a
    /// </summary>
    StructuredBufferReadOnly,

    /// <summary>
    /// A <see cref="DeviceBuffer" /> accessed as a read-write storage buffer. A subset of a buffer can be bound using a
    /// </summary>
    StructuredBufferReadWrite,

    /// <summary>
    /// A read-only <see cref="Texture" />, accessed through a Texture or <see cref="TextureView" />.
    /// </summary>
    TextureReadOnly,

    /// <summary>
    /// A read-write <see cref="Texture" />, accessed through a Texture or <see cref="TextureView" />.
    /// </summary>
    TextureReadWrite,

    /// <summary>
    /// A <see cref="Veldrith.Sampler" />.
    /// </summary>
    Sampler
}