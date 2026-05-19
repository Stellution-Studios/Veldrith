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
    /// Performs the string_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">The value of NativePtr.</param>
    /// <param name="sel_domain">The value of sel_domain.</param>
    /// <returns>The result of the string_objc_msgSend operation.</returns>
    public string domain => string_objc_msgSend(this.NativePtr, sel_domain);

    /// <summary>
    /// Performs the string_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">The value of NativePtr.</param>
    /// <param name="sel_localizedDescription">The value of sel_localizedDescription.</param>
    /// <returns>The result of the string_objc_msgSend operation.</returns>
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