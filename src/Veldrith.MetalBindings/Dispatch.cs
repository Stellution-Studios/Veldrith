using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

public static unsafe class Dispatch {

    /// <summary>
    /// Represents the LibdispatchLocation field.
    /// </summary>
    private const string LibdispatchLocation = @"/usr/lib/system/libdispatch.dylib";

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Executes dispatch_get_global_queue.
    /// </summary>
    public static extern DispatchQueue dispatch_get_global_queue(QualityOfServiceLevel identifier, ulong flags);

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Executes dispatch_data_create.
    /// </summary>
    public static extern DispatchData dispatch_data_create(void* buffer, UIntPtr size, DispatchQueue queue, IntPtr destructorBlock);

    [DllImport(LibdispatchLocation)]

    /// <summary>
    /// Executes dispatch_release.
    /// </summary>
    public static extern void dispatch_release(IntPtr nativePtr);
}

public enum QualityOfServiceLevel : long {
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