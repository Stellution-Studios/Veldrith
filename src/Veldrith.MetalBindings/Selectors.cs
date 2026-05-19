namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Selectors type used by the graphics runtime.
/// </summary>
internal static class Selectors {

    /// <summary>
    /// Stores the texture state used by this instance.
    /// </summary>
    internal static readonly Selector texture = "texture";

    /// <summary>
    /// Stores the set texture state used by this instance.
    /// </summary>
    internal static readonly Selector setTexture = "setTexture:";

    /// <summary>
    /// Stores the load action state used by this instance.
    /// </summary>
    internal static readonly Selector loadAction = "loadAction";

    /// <summary>
    /// Stores the set load action state used by this instance.
    /// </summary>
    internal static readonly Selector setLoadAction = "setLoadAction:";

    /// <summary>
    /// Stores the store action state used by this instance.
    /// </summary>
    internal static readonly Selector storeAction = "storeAction";

    /// <summary>
    /// Stores the set store action state used by this instance.
    /// </summary>
    internal static readonly Selector setStoreAction = "setStoreAction:";

    /// <summary>
    /// Stores the resolve texture state used by this instance.
    /// </summary>
    internal static readonly Selector resolveTexture = "resolveTexture";

    /// <summary>
    /// Stores the set resolve texture state used by this instance.
    /// </summary>
    internal static readonly Selector setResolveTexture = "setResolveTexture:";

    /// <summary>
    /// Stores the slice state used by this instance.
    /// </summary>
    internal static readonly Selector slice = "slice";

    /// <summary>
    /// Stores the set slice state used by this instance.
    /// </summary>
    internal static readonly Selector setSlice = "setSlice:";

    /// <summary>
    /// Stores the level state used by this instance.
    /// </summary>
    internal static readonly Selector level = "level";

    /// <summary>
    /// Stores the set level state used by this instance.
    /// </summary>
    internal static readonly Selector setLevel = "setLevel:";

    /// <summary>
    /// Stores the object at indexed subscript value used during command execution.
    /// </summary>
    internal static readonly Selector objectAtIndexedSubscript = "objectAtIndexedSubscript:";

    /// <summary>
    /// Stores the set object at indexed subscript value used during command execution.
    /// </summary>
    internal static readonly Selector setObjectAtIndexedSubscript = "setObject:atIndexedSubscript:";

    /// <summary>
    /// Stores the pixel format state used by this instance.
    /// </summary>
    internal static readonly Selector pixelFormat = "pixelFormat";

    /// <summary>
    /// Stores the set pixel format state used by this instance.
    /// </summary>
    internal static readonly Selector setPixelFormat = "setPixelFormat:";

    /// <summary>
    /// Stores the alloc state used by this instance.
    /// </summary>
    internal static readonly Selector alloc = "alloc";

    /// <summary>
    /// Stores the init state used by this instance.
    /// </summary>
    internal static readonly Selector init = "init";

    /// <summary>
    /// Stores the push debug group state used by this instance.
    /// </summary>
    internal static readonly Selector pushDebugGroup = "pushDebugGroup:";

    /// <summary>
    /// Stores the pop debug group state used by this instance.
    /// </summary>
    internal static readonly Selector popDebugGroup = "popDebugGroup";

    /// <summary>
    /// Stores the insert debug signpost state used by this instance.
    /// </summary>
    internal static readonly Selector insertDebugSignpost = "insertDebugSignpost:";
}