using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the ObjectiveCRuntime type used by the graphics runtime.
/// </summary>
public static unsafe class ObjectiveCRuntime {

    /// <summary>
    /// Stores the obj clibrary state used by this instance.
    /// </summary>
    private const string _objCLibrary = "/usr/lib/libobjc.A.dylib";

    /// <summary>
    /// Stores the sel retain state used by this instance.
    /// </summary>
    private static readonly Selector _selRetain = "retain";

    /// <summary>
    /// Stores the sel release state used by this instance.
    /// </summary>
    private static readonly Selector _selRelease = "release";

    /// <summary>
    /// Stores the sel retain count value used during command execution.
    /// </summary>
    private static readonly Selector _selRetainCount = "retainCount";

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, float a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, double a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, CGRect a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, uint b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, NSRange b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLSize a, MTLSize b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr c, UIntPtr d, MTLSize e);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLClearColor a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, CGSize a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b, UIntPtr c);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, UIntPtr b, UIntPtr c);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, UIntPtr c);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d, UIntPtr e);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, NSRange a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLCommandBufferHandler a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLViewport a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLScissorRect a);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, uint b, UIntPtr c);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    /// <param name="f">The f value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, MTLIndexType c, IntPtr d, UIntPtr e, UIntPtr f);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, MTLBuffer b, UIntPtr c);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    /// <param name="f">The f value used by this operation.</param>
    /// <param name="g">The g value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, MTLIndexType c, IntPtr d, UIntPtr e, UIntPtr f, IntPtr g, UIntPtr h);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    /// <param name="f">The f value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, MTLIndexType b, MTLBuffer c, UIntPtr d, MTLBuffer e, UIntPtr f);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLBuffer a, UIntPtr b, MTLBuffer c, UIntPtr d, UIntPtr e);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    /// <param name="f">The f value used by this operation.</param>
    /// <param name="g">The g value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
    /// <param name="i">The i value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b, UIntPtr c, UIntPtr d, MTLSize e, IntPtr f, UIntPtr g, UIntPtr h, MTLOrigin i);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    /// <param name="f">The f value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLRegion a, UIntPtr b, UIntPtr c, IntPtr d, UIntPtr e, UIntPtr f);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <param name="e">The e value used by this operation.</param>
    /// <param name="f">The f value used by this operation.</param>
    /// <param name="g">The g value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
    /// <param name="i">The i value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLTexture a, UIntPtr b, UIntPtr c, MTLOrigin d, MTLSize e, MTLBuffer f, UIntPtr g, UIntPtr h, UIntPtr i);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="sourceTexture">The source texture value used by this operation.</param>
    /// <param name="sourceSlice">The source slice value used by this operation.</param>
    /// <param name="sourceLevel">The source level value used by this operation.</param>
    /// <param name="sourceOrigin">The source origin value used by this operation.</param>
    /// <param name="sourceSize">The source size value used by this operation.</param>
    /// <param name="destinationTexture">The destination texture value used by this operation.</param>
    /// <param name="destinationSlice">The destination slice value used by this operation.</param>
    /// <param name="destinationLevel">The destination level value used by this operation.</param>
    /// <param name="destinationOrigin">The destination origin value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLTexture destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, MTLOrigin destinationOrigin);

    /// <summary>
    /// Executes the byte ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern byte* bytePtr_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the cgsize objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern CGSize CGSize_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the byte objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern byte byte_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the bool8 objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the bool8 objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    /// <summary>
    /// Executes the bool8 objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);

    /// <summary>
    /// Executes the bool8 objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a, IntPtr b);

    /// <summary>
    /// Executes the bool8 objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, uint a);

    /// <summary>
    /// Executes the uint objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern uint uint_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the float objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern float float_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the cgfloat objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern CGFloat CGFloat_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the double objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern double double_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="error">The error value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, out NSError error);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a, uint b, NSRange c, NSRange d);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="error">The error value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, MTLComputePipelineDescriptor a, uint b, IntPtr c, out NSError error);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="error">The error value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, IntPtr b, out NSError error);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr b, MTLResourceOptions c);

    /// <summary>
    /// Executes the int ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, MTLResourceOptions c);

    /// <summary>
    /// Executes the uint ptr objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern UIntPtr UIntPtr_objc_msgSend(IntPtr receiver, Selector selector);

    public static T objc_msgSend<T>(IntPtr receiver, Selector selector) where T : struct {
        IntPtr value = IntPtr_objc_msgSend(receiver, selector);
        return Unsafe.AsRef<T>(&value);
    }

    public static T objc_msgSend<T>(IntPtr receiver, Selector selector, IntPtr a) where T : struct {
        IntPtr value = IntPtr_objc_msgSend(receiver, selector, a);
        return Unsafe.AsRef<T>(&value);
    }

    /// <summary>
    /// Executes the string objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static string string_objc_msgSend(IntPtr receiver, Selector selector) {
        return objc_msgSend<NSString>(receiver, selector).GetValue();
    }

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, byte b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, Bool8 b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, uint b);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="c">The c value used by this operation.</param>
    /// <param name="d">The d value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, float a, float b, float c, float d);

    /// <summary>
    /// Executes the objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr b);

    /// <summary>
    /// Executes the objc msg send stret logic for this backend.
    /// </summary>
    /// <param name="retPtr">The ret ptr value used by this operation.</param>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend_stret")]
    public static extern void objc_msgSend_stret(void* retPtr, IntPtr receiver, Selector selector);

    public static T objc_msgSend_stret<T>(IntPtr receiver, Selector selector) where T : struct {
        T ret = default;
        objc_msgSend_stret(Unsafe.AsPointer(ref ret), receiver, selector);
        return ret;
    }

    /// <summary>
    /// Executes the mtlclear color objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern MTLClearColor MTLClearColor_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the mtlsize objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern MTLSize MTLSize_objc_msgSend(IntPtr receiver, Selector selector);

    /// <summary>
    /// Executes the cgrect objc msg send logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <param name="selector">The selector value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary, EntryPoint = "objc_msgSend")]
    public static extern CGRect CGRect_objc_msgSend(IntPtr receiver, Selector selector);

    // TODO: This should check the current processor type, struct size, etc.
    // At the moment there is no need because all existing occurences of
    // this can safely use the non-stret versions everywhere.
    /// <summary>
    /// Determines whether the architecture-specific stret messaging path should be used for <typeparamref name="T" />.
    /// </summary>
    public static bool UseStret<T>() {
        return false;
    }

    /// <summary>
    /// Executes the sel register name logic for this backend.
    /// </summary>
    /// <param name="namePtr">The name ptr value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary)]
    public static extern IntPtr sel_registerName(byte* namePtr);

    /// <summary>
    /// Executes the sel get name logic for this backend.
    /// </summary>
    /// <param name="selector">The selector value used by this operation.</param>
    [DllImport(_objCLibrary)]
    public static extern byte* sel_getName(IntPtr selector);

    /// <summary>
    /// Executes the objc get class logic for this backend.
    /// </summary>
    /// <param name="namePtr">The name ptr value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary)]
    public static extern IntPtr objc_getClass(byte* namePtr);

    /// <summary>
    /// Executes the object get class logic for this backend.
    /// </summary>
    /// <param name="obj">The object instance to evaluate.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary)]
    public static extern ObjCClass object_getClass(IntPtr obj);

    /// <summary>
    /// Executes the class get property logic for this backend.
    /// </summary>
    /// <param name="cls">The cls value used by this operation.</param>
    /// <param name="namePtr">The name ptr value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary)]
    public static extern IntPtr class_getProperty(ObjCClass cls, byte* namePtr);

    /// <summary>
    /// Executes the class get name logic for this backend.
    /// </summary>
    /// <param name="cls">The cls value used by this operation.</param>
    [DllImport(_objCLibrary)]
    public static extern byte* class_getName(ObjCClass cls);

    /// <summary>
    /// Executes the property copy attribute value logic for this backend.
    /// </summary>
    /// <param name="property">The property value used by this operation.</param>
    /// <param name="attributeNamePtr">The attribute name ptr value used by this operation.</param>
    [DllImport(_objCLibrary)]
    public static extern byte* property_copyAttributeValue(IntPtr property, byte* attributeNamePtr);

    /// <summary>
    /// Executes the method get name logic for this backend.
    /// </summary>
    /// <param name="method">The method value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(_objCLibrary)]
    public static extern Selector method_getName(ObjectiveCMethod method);

    /// <summary>
    /// Executes the class copy method list logic for this backend.
    /// </summary>
    /// <param name="cls">The cls value used by this operation.</param>
    /// <param name="outCount">The out count value used by this operation.</param>
    [DllImport(_objCLibrary)]
    public static extern ObjectiveCMethod* class_copyMethodList(ObjCClass cls, out uint outCount);

    /// <summary>
    /// Executes the free logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    [DllImport(_objCLibrary)]
    public static extern void free(IntPtr receiver);

    /// <summary>
    /// Executes the retain logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    public static void Retain(IntPtr receiver) {
        objc_msgSend(receiver, _selRetain);
    }

    /// <summary>
    /// Executes the release logic for this backend.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    public static void Release(IntPtr receiver) {
        objc_msgSend(receiver, _selRelease);
    }

    /// <summary>
    /// Gets the retain count value.
    /// </summary>
    /// <param name="receiver">The receiver value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static ulong GetRetainCount(IntPtr receiver) {
        return UIntPtr_objc_msgSend(receiver, _selRetainCount);
    }
}