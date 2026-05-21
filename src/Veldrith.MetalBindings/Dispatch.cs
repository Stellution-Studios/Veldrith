using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Dispatch type used by the graphics runtime.
/// </summary>
public static unsafe class Dispatch {

    /// <summary>
    /// Stores the libdispatch location state used by this instance.
    /// </summary>
    private const string LibdispatchLocation = @"/usr/lib/system/libdispatch.dylib";

    [DllImport(LibdispatchLocation, EntryPoint = "dispatch_get_global_queue")]

    /// <summary>
    /// Executes the dispatch get global queue logic for this backend.
    /// </summary>
    /// <param name="identifier">The identifier value used by this operation.</param>
    /// <param name="flags">The flags value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static extern DispatchQueue DispatchGetGlobalQueue(QualityOfServiceLevel identifier, ulong flags);

    [DllImport(LibdispatchLocation, EntryPoint = "dispatch_data_create")]

    /// <summary>
    /// Executes the dispatch data create logic for this backend.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <param name="queue">The queue value used by this operation.</param>
    /// <param name="destructorBlock">The destructor block value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static extern DispatchData DispatchDataCreate(void* buffer, UIntPtr size, DispatchQueue queue, IntPtr destructorBlock);

    [DllImport(LibdispatchLocation, EntryPoint = "dispatch_release")]

    /// <summary>
    /// Executes the dispatch release logic for this backend.
    /// </summary>
    /// <param name="nativePtr">The native ptr value used by this operation.</param>
    public static extern void DispatchRelease(IntPtr nativePtr);
}

/// <summary>
/// Defines the available values of the QualityOfServiceLevel enumeration.
/// </summary>
public enum QualityOfServiceLevel : long {

    /// <summary>
    /// Defines the predefined value for qos class user interactive.
    /// </summary>
    QOS_CLASS_USER_INTERACTIVE = 0x21, QOS_CLASS_USER_INITIATED = 0x19, QOS_CLASS_DEFAULT = 0x15, QOS_CLASS_UTILITY = 0x11, QOS_CLASS_BACKGROUND = 0x9, QOS_CLASS_UNSPECIFIED = 0
}

/// <summary>
/// Represents the DispatchQueue data structure used by the graphics runtime.
/// </summary>
public struct DispatchQueue {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;
}

/// <summary>
/// Represents the DispatchData data structure used by the graphics runtime.
/// </summary>
public struct DispatchData {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;
}
