using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the BlockLiteral data structure used by the graphics runtime.
/// </summary>
public unsafe struct BlockLiteral {

    /// <summary>
    /// Stores the isa state used by this instance.
    /// </summary>
    public IntPtr isa;

    /// <summary>
    /// Stores the flags state used by this instance.
    /// </summary>
    public int flags;

    /// <summary>
    /// Stores the reserved state used by this instance.
    /// </summary>
    public int reserved;

    /// <summary>
    /// Stores the invoke state used by this instance.
    /// </summary>
    public IntPtr invoke;

    /// <summary>
    /// Stores the descriptor state used by this instance.
    /// </summary>
    public BlockDescriptor* descriptor;
}

/// <summary>
/// Represents the BlockDescriptor data structure used by the graphics runtime.
/// </summary>
public struct BlockDescriptor {

    /// <summary>
    /// Stores the reserved state used by this instance.
    /// </summary>
    public ulong reserved;

    /// <summary>
    /// Stores the block size value used during command execution.
    /// </summary>
    public ulong Block_size;
}