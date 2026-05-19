using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSError struct.
/// </summary>
public struct NSError {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets domain.
    /// </summary>
    public string domain => string_objc_msgSend(this.NativePtr, sel_domain);

    /// <summary>
    /// Gets or sets localizedDescription.
    /// </summary>
    public string localizedDescription => string_objc_msgSend(this.NativePtr, sel_localizedDescription);

    /// <summary>
    /// Represents the sel_domain field.
    /// </summary>
    private static readonly Selector sel_domain = "domain";

    /// <summary>
    /// Represents the sel_localizedDescription field.
    /// </summary>
    private static readonly Selector sel_localizedDescription = "localizedDescription";
}