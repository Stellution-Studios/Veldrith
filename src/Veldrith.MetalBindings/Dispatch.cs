using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Dispatch class.
/// </summary>
public static unsafe class Dispatch {

    /// <summary>
    /// Represents the LibdispatchLocation field.
    /// </summary>
    private const string LibdispatchLocation = @"/usr/lib/system/libdispatch.dylib";

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Performs the dispatch_get_global_queue operation.
    /// </summary>
    /// <param name="identifier">The value of identifier.</param>
    /// <param name="flags">The value of flags.</param>
    /// <returns>The result of the dispatch_get_global_queue operation.</returns>
    public static extern DispatchQueue dispatch_get_global_queue(QualityOfServiceLevel identifier, ulong flags);

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Performs the dispatch_data_create operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="size">The value of size.</param>
    /// <param name="queue">The value of queue.</param>
    /// <param name="destructorBlock">The value of destructorBlock.</param>
    /// <returns>The result of the dispatch_data_create operation.</returns>
    public static extern DispatchData dispatch_data_create(void* buffer, UIntPtr size, DispatchQueue queue, IntPtr destructorBlock);

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Performs the dispatch_release operation.
    /// </summary>
    /// <param name="nativePtr">The value of nativePtr.</param>
    public static extern void dispatch_release(IntPtr nativePtr);
}

/// <summary>
/// Represents the QualityOfServiceLevel enum.
/// </summary>
public enum QualityOfServiceLevel : long {

    /// <summary>
    /// Represents the QOS_CLASS_USER_INTERACTIVE field.
    /// </summary>
    QOS_CLASS_USER_INTERACTIVE = 0x21, QOS_CLASS_USER_INITIATED = 0x19, QOS_CLASS_DEFAULT = 0x15, QOS_CLASS_UTILITY = 0x11, QOS_CLASS_BACKGROUND = 0x9, QOS_CLASS_UNSPECIFIED = 0
}

/// <summary>
/// Represents the DispatchQueue struct.
/// </summary>
public struct DispatchQueue {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;
}

/// <summary>
/// Represents the DispatchData struct.
/// </summary>
public struct DispatchData {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;
}