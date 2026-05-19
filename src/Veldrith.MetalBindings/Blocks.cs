using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the BlockLiteral struct.
/// </summary>
public unsafe struct BlockLiteral {

    /// <summary>
    /// Stores the value associated with <c>isa</c>.
    /// </summary>
    public IntPtr isa;

    /// <summary>
    /// Stores the value associated with <c>flags</c>.
    /// </summary>
    public int flags;

    /// <summary>
    /// Stores the value associated with <c>reserved</c>.
    /// </summary>
    public int reserved;

    /// <summary>
    /// Stores the value associated with <c>invoke</c>.
    /// </summary>
    public IntPtr invoke;

    /// <summary>
    /// Stores the value associated with <c>descriptor</c>.
    /// </summary>
    public BlockDescriptor* descriptor;
}

/// <summary>
/// Defines the data layout and behavior of the BlockDescriptor struct.
/// </summary>
public struct BlockDescriptor {

    /// <summary>
    /// Stores the value associated with <c>reserved</c>.
    /// </summary>
    public ulong reserved;

    /// <summary>
    /// Stores the value associated with <c>Block_size</c>.
    /// </summary>
    public ulong Block_size;
}