using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the BlockLiteral data structure used by the graphics runtime.
/// </summary>
public unsafe struct BlockLiteral {

    /// <summary>
    /// Stores the isa state used by this instance.
    /// </summary>
    public IntPtr Isa;

    /// <summary>
    /// Stores the flags state used by this instance.
    /// </summary>
    public int Flags;

    /// <summary>
    /// Stores the reserved state used by this instance.
    /// </summary>
    public int Reserved;

    /// <summary>
    /// Stores the invoke state used by this instance.
    /// </summary>
    public IntPtr Invoke;

    /// <summary>
    /// Stores the descriptor state used by this instance.
    /// </summary>
    public BlockDescriptor* Descriptor;
}

/// <summary>
/// Represents the BlockDescriptor data structure used by the graphics runtime.
/// </summary>
public struct BlockDescriptor {

    /// <summary>
    /// Stores the reserved state used by this instance.
    /// </summary>
    public ulong Reserved;

    /// <summary>
    /// Stores the block size value used during command execution.
    /// </summary>
    public ulong BlockSize;
}