using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLFunction data structure used by the graphics runtime.
/// </summary>
public struct MTLFunction {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLFunction" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLFunction(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets functionConstantsDictionary.
    /// </summary>

    public NSDictionary functionConstantsDictionary => objc_msgSend<NSDictionary>(this.NativePtr, sel_functionConstantsDictionary);

    /// <summary>
    /// Stores the sel function constants dictionary state used by this instance.
    /// </summary>
    private static readonly Selector sel_functionConstantsDictionary = "functionConstantsDictionary";
}