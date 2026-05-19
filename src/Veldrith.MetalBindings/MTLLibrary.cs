using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLLibrary struct.
/// </summary>
public struct MTLLibrary {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLLibrary" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLLibrary(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the newFunctionWithName operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    /// <returns>Returns the result produced by the newFunctionWithName operation.</returns>
    public MTLFunction newFunctionWithName(string name) {
        NSString nameNSS = NSString.New(name);
        IntPtr function = IntPtr_objc_msgSend(this.NativePtr, sel_newFunctionWithName, nameNSS);
        release(nameNSS.NativePtr);
        return new MTLFunction(function);
    }

    /// <summary>
    /// Executes the newFunctionWithNameConstantValues operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    /// <param name="constantValues">Specifies the value of <paramref name="constantValues" />.</param>
    /// <returns>Returns the result produced by the newFunctionWithNameConstantValues operation.</returns>
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
    /// Stores the value associated with <c>sel_newFunctionWithName</c>.
    /// </summary>
    private static readonly Selector sel_newFunctionWithName = "newFunctionWithName:";

    /// <summary>
    /// Stores the value associated with <c>sel_newFunctionWithNameConstantValues</c>.
    /// </summary>
    private static readonly Selector sel_newFunctionWithNameConstantValues = "newFunctionWithName:constantValues:error:";
}