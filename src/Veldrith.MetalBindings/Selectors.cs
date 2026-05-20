namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Selectors type used by the graphics runtime.
/// </summary>
internal static class Selectors {

    /// <summary>
    /// Stores the texture state used by this instance.
    /// </summary>
    internal static readonly Selector Texture = "texture";

    /// <summary>
    /// Stores the set texture state used by this instance.
    /// </summary>
    internal static readonly Selector SetTexture = "setTexture:";

    /// <summary>
    /// Stores the load action state used by this instance.
    /// </summary>
    internal static readonly Selector LoadAction = "loadAction";

    /// <summary>
    /// Stores the set load action state used by this instance.
    /// </summary>
    internal static readonly Selector SetLoadAction = "setLoadAction:";

    /// <summary>
    /// Stores the store action state used by this instance.
    /// </summary>
    internal static readonly Selector StoreAction = "storeAction";

    /// <summary>
    /// Stores the set store action state used by this instance.
    /// </summary>
    internal static readonly Selector SetStoreAction = "setStoreAction:";

    /// <summary>
    /// Stores the resolve texture state used by this instance.
    /// </summary>
    internal static readonly Selector ResolveTexture = "resolveTexture";

    /// <summary>
    /// Stores the set resolve texture state used by this instance.
    /// </summary>
    internal static readonly Selector SetResolveTexture = "setResolveTexture:";

    /// <summary>
    /// Stores the slice state used by this instance.
    /// </summary>
    internal static readonly Selector Slice = "slice";

    /// <summary>
    /// Stores the set slice state used by this instance.
    /// </summary>
    internal static readonly Selector SetSlice = "setSlice:";

    /// <summary>
    /// Stores the level state used by this instance.
    /// </summary>
    internal static readonly Selector Level = "level";

    /// <summary>
    /// Stores the set level state used by this instance.
    /// </summary>
    internal static readonly Selector SetLevel = "setLevel:";

    /// <summary>
    /// Stores the object at indexed subscript value used during command execution.
    /// </summary>
    internal static readonly Selector ObjectAtIndexedSubscript = "objectAtIndexedSubscript:";

    /// <summary>
    /// Stores the set object at indexed subscript value used during command execution.
    /// </summary>
    internal static readonly Selector SetObjectAtIndexedSubscript = "setObject:atIndexedSubscript:";

    /// <summary>
    /// Stores the pixel format state used by this instance.
    /// </summary>
    internal static readonly Selector PixelFormat = "pixelFormat";

    /// <summary>
    /// Stores the set pixel format state used by this instance.
    /// </summary>
    internal static readonly Selector SetPixelFormat = "setPixelFormat:";

    /// <summary>
    /// Stores the alloc state used by this instance.
    /// </summary>
    internal static readonly Selector Alloc = "alloc";

    /// <summary>
    /// Stores the init state used by this instance.
    /// </summary>
    internal static readonly Selector Init = "init";

    /// <summary>
    /// Stores the push debug group state used by this instance.
    /// </summary>
    internal static readonly Selector PushDebugGroup = "pushDebugGroup:";

    /// <summary>
    /// Stores the pop debug group state used by this instance.
    /// </summary>
    internal static readonly Selector PopDebugGroup = "popDebugGroup";

    /// <summary>
    /// Stores the insert debug signpost state used by this instance.
    /// </summary>
    internal static readonly Selector InsertDebugSignpost = "insertDebugSignpost:";
}