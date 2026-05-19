using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSError data structure used by the graphics runtime.
/// </summary>
public struct NSError {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the string objc msg send logic for this backend.
    /// </summary>

    public string domain => string_objc_msgSend(this.NativePtr, sel_domain);

    /// <summary>
    /// Executes the string objc msg send logic for this backend.
    /// </summary>

    public string localizedDescription => string_objc_msgSend(this.NativePtr, sel_localizedDescription);

    /// <summary>
    /// Stores the sel domain state used by this instance.
    /// </summary>
    private static readonly Selector sel_domain = "domain";

    /// <summary>
    /// Stores the sel localized description state used by this instance.
    /// </summary>
    private static readonly Selector sel_localizedDescription = "localizedDescription";
}