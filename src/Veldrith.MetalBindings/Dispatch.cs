using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the behavior and responsibilities of the Dispatch class.
/// </summary>
public static unsafe class Dispatch {

    /// <summary>
    /// Stores the value associated with <c>LibdispatchLocation</c>.
    /// </summary>
    private const string LibdispatchLocation = @"/usr/lib/system/libdispatch.dylib";

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Executes the dispatch_get_global_queue operation.
    /// </summary>
    /// <param name="identifier">Specifies the value of <paramref name="identifier" />.</param>
    /// <param name="flags">Specifies the value of <paramref name="flags" />.</param>
    /// <returns>Returns the result produced by the dispatch_get_global_queue operation.</returns>
    public static extern DispatchQueue dispatch_get_global_queue(QualityOfServiceLevel identifier, ulong flags);

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Executes the dispatch_data_create operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
    /// <param name="queue">Specifies the value of <paramref name="queue" />.</param>
    /// <param name="destructorBlock">Specifies the value of <paramref name="destructorBlock" />.</param>
    /// <returns>Returns the result produced by the dispatch_data_create operation.</returns>
    public static extern DispatchData dispatch_data_create(void* buffer, UIntPtr size, DispatchQueue queue, IntPtr destructorBlock);

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Executes the dispatch_release operation.
    /// </summary>
    /// <param name="nativePtr">Specifies the value of <paramref name="nativePtr" />.</param>
    public static extern void dispatch_release(IntPtr nativePtr);
}

/// <summary>
/// Defines the available values of the QualityOfServiceLevel enumeration.
/// </summary>
public enum QualityOfServiceLevel : long {

    /// <summary>
    /// Stores the value associated with <c>QOS_CLASS_USER_INTERACTIVE</c>.
    /// </summary>
    QOS_CLASS_USER_INTERACTIVE = 0x21, QOS_CLASS_USER_INITIATED = 0x19, QOS_CLASS_DEFAULT = 0x15, QOS_CLASS_UTILITY = 0x11, QOS_CLASS_BACKGROUND = 0x9, QOS_CLASS_UNSPECIFIED = 0
}

/// <summary>
/// Defines the data layout and behavior of the DispatchQueue struct.
/// </summary>
public struct DispatchQueue {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;
}

/// <summary>
/// Defines the data layout and behavior of the DispatchData struct.
/// </summary>
public struct DispatchData {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;
}