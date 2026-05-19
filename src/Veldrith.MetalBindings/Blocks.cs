using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the BlockLiteral struct.
/// </summary>
public unsafe struct BlockLiteral {

    /// <summary>
    /// Represents the isa field.
    /// </summary>
    public IntPtr isa;

    /// <summary>
    /// Represents the flags field.
    /// </summary>
    public int flags;

    /// <summary>
    /// Represents the reserved field.
    /// </summary>
    public int reserved;

    /// <summary>
    /// Represents the invoke field.
    /// </summary>
    public IntPtr invoke;

    /// <summary>
    /// Represents the descriptor field.
    /// </summary>
    public BlockDescriptor* descriptor;
}

/// <summary>
/// Represents the BlockDescriptor struct.
/// </summary>
public struct BlockDescriptor {

    /// <summary>
    /// Represents the reserved field.
    /// </summary>
    public ulong reserved;

    /// <summary>
    /// Represents the Block_size field.
    /// </summary>
    public ulong Block_size;
}