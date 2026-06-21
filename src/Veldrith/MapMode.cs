namespace Veldrith;

/// <summary>
/// Identifies how a <see cref="IMappableResource" /> will be mapped into CPU address space.
/// </summary>
public enum MapMode : byte {

    /// <summary>
    /// A read-only resource mapping. The mapped data region is not writable, and cannot be used to transfer data into the
    /// </summary>
    Read,

    /// <summary>
    /// A write-only resource mapping. The mapped data region is writable, and will be transferred into the graphics
    /// </summary>
    Write,

    /// <summary>
    /// A read-write resource mapping. The mapped data region is both readable and writable. NOTE: this mode can only be
    /// </summary>
    ReadWrite
}