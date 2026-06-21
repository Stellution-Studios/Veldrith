using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLLibrary data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLLibrary {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLLibrary" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLLibrary(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the new function with name logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLFunction NewFunctionWithName(string name) {
        NSString nameNSS = NSString.New(name);
        IntPtr function = IntPtr_objc_msgSend(this.NativePtr, sel_newFunctionWithName, nameNSS);
        Release(nameNSS.NativePtr);
        return new MTLFunction(function);
    }

    /// <summary>
    /// Executes the new function with name constant values logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <param name="constantValues">The constant values value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLFunction NewFunctionWithNameConstantValues(string name, MTLFunctionConstantValues constantValues) {
        NSString nameNss = NSString.New(name);
        IntPtr function = IntPtr_objc_msgSend(this.NativePtr, sel_newFunctionWithNameConstantValues, nameNss.NativePtr, constantValues.NativePtr, out NSError error);
        Release(nameNss.NativePtr);

        if (function == IntPtr.Zero) {
            throw new Exception($"Failed to create MTLFunction: {error.LocalizedDescription}");
        }

        return new MTLFunction(function);
    }

    /// <summary>
    /// Stores the sel new function with name state used by this instance.
    /// </summary>
    private static readonly Selector sel_newFunctionWithName = "newFunctionWithName:";

    /// <summary>
    /// Stores the sel new function with name constant values state used by this instance.
    /// </summary>
    private static readonly Selector sel_newFunctionWithNameConstantValues = "newFunctionWithName:constantValues:error:";
}