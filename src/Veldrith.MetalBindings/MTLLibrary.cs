using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLLibrary struct.
/// </summary>
public struct MTLLibrary {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLLibrary" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLLibrary(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the newFunctionWithName operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <returns>The result of the newFunctionWithName operation.</returns>
    public MTLFunction newFunctionWithName(string name) {
        NSString nameNSS = NSString.New(name);
        IntPtr function = IntPtr_objc_msgSend(this.NativePtr, sel_newFunctionWithName, nameNSS);
        release(nameNSS.NativePtr);
        return new MTLFunction(function);
    }

    /// <summary>
    /// Performs the newFunctionWithNameConstantValues operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <param name="constantValues">The value of constantValues.</param>
    /// <returns>The result of the newFunctionWithNameConstantValues operation.</returns>
    public MTLFunction newFunctionWithNameConstantValues(string name, MTLFunctionConstantValues constantValues) {
        NSString nameNSS = NSString.New(name);
        IntPtr function = IntPtr_objc_msgSend(this.NativePtr, sel_newFunctionWithNameConstantValues, nameNSS.NativePtr, constantValues.NativePtr, out NSError error);
        release(nameNSS.NativePtr);

        if (function == IntPtr.Zero) {
            throw new Exception($"Failed to create MTLFunction: {error.localizedDescription}");
        }

        return new MTLFunction(function);
    }

    /// <summary>
    /// Represents the sel_newFunctionWithName field.
    /// </summary>
    private static readonly Selector sel_newFunctionWithName = "newFunctionWithName:";

    /// <summary>
    /// Represents the sel_newFunctionWithNameConstantValues field.
    /// </summary>
    private static readonly Selector sel_newFunctionWithNameConstantValues = "newFunctionWithName:constantValues:error:";
}