namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the behavior and responsibilities of the Selectors class.
/// </summary>
internal static class Selectors {

    /// <summary>
    /// Stores the value associated with <c>texture</c>.
    /// </summary>
    internal static readonly Selector texture = "texture";

    /// <summary>
    /// Stores the value associated with <c>setTexture</c>.
    /// </summary>
    internal static readonly Selector setTexture = "setTexture:";

    /// <summary>
    /// Stores the value associated with <c>loadAction</c>.
    /// </summary>
    internal static readonly Selector loadAction = "loadAction";

    /// <summary>
    /// Stores the value associated with <c>setLoadAction</c>.
    /// </summary>
    internal static readonly Selector setLoadAction = "setLoadAction:";

    /// <summary>
    /// Stores the value associated with <c>storeAction</c>.
    /// </summary>
    internal static readonly Selector storeAction = "storeAction";

    /// <summary>
    /// Stores the value associated with <c>setStoreAction</c>.
    /// </summary>
    internal static readonly Selector setStoreAction = "setStoreAction:";

    /// <summary>
    /// Stores the value associated with <c>resolveTexture</c>.
    /// </summary>
    internal static readonly Selector resolveTexture = "resolveTexture";

    /// <summary>
    /// Stores the value associated with <c>setResolveTexture</c>.
    /// </summary>
    internal static readonly Selector setResolveTexture = "setResolveTexture:";

    /// <summary>
    /// Stores the value associated with <c>slice</c>.
    /// </summary>
    internal static readonly Selector slice = "slice";

    /// <summary>
    /// Stores the value associated with <c>setSlice</c>.
    /// </summary>
    internal static readonly Selector setSlice = "setSlice:";

    /// <summary>
    /// Stores the value associated with <c>level</c>.
    /// </summary>
    internal static readonly Selector level = "level";

    /// <summary>
    /// Stores the value associated with <c>setLevel</c>.
    /// </summary>
    internal static readonly Selector setLevel = "setLevel:";

    /// <summary>
    /// Stores the value associated with <c>objectAtIndexedSubscript</c>.
    /// </summary>
    internal static readonly Selector objectAtIndexedSubscript = "objectAtIndexedSubscript:";

    /// <summary>
    /// Stores the value associated with <c>setObjectAtIndexedSubscript</c>.
    /// </summary>
    internal static readonly Selector setObjectAtIndexedSubscript = "setObject:atIndexedSubscript:";

    /// <summary>
    /// Stores the value associated with <c>pixelFormat</c>.
    /// </summary>
    internal static readonly Selector pixelFormat = "pixelFormat";

    /// <summary>
    /// Stores the value associated with <c>setPixelFormat</c>.
    /// </summary>
    internal static readonly Selector setPixelFormat = "setPixelFormat:";

    /// <summary>
    /// Stores the value associated with <c>alloc</c>.
    /// </summary>
    internal static readonly Selector alloc = "alloc";

    /// <summary>
    /// Stores the value associated with <c>init</c>.
    /// </summary>
    internal static readonly Selector init = "init";

    /// <summary>
    /// Stores the value associated with <c>pushDebugGroup</c>.
    /// </summary>
    internal static readonly Selector pushDebugGroup = "pushDebugGroup:";

    /// <summary>
    /// Stores the value associated with <c>popDebugGroup</c>.
    /// </summary>
    internal static readonly Selector popDebugGroup = "popDebugGroup";

    /// <summary>
    /// Stores the value associated with <c>insertDebugSignpost</c>.
    /// </summary>
    internal static readonly Selector insertDebugSignpost = "insertDebugSignpost:";
}