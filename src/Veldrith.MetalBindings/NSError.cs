using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSError struct.
/// </summary>
public struct NSError {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the string_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">Specifies the value of <paramref name="NativePtr" />.</param>
    /// <param name="sel_domain">Specifies the value of <paramref name="sel_domain" />.</param>
    /// <returns>Returns the result produced by the string_objc_msgSend operation.</returns>
    public string domain => string_objc_msgSend(this.NativePtr, sel_domain);

    /// <summary>
    /// Executes the string_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">Specifies the value of <paramref name="NativePtr" />.</param>
    /// <param name="sel_localizedDescription">Specifies the value of <paramref name="sel_localizedDescription" />.</param>
    /// <returns>Returns the result produced by the string_objc_msgSend operation.</returns>
    public string localizedDescription => string_objc_msgSend(this.NativePtr, sel_localizedDescription);

    /// <summary>
    /// Stores the value associated with <c>sel_domain</c>.
    /// </summary>
    private static readonly Selector sel_domain = "domain";

    /// <summary>
    /// Stores the value associated with <c>sel_localizedDescription</c>.
    /// </summary>
    private static readonly Selector sel_localizedDescription = "localizedDescription";
}