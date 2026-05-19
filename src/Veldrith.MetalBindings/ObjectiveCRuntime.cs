using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the behavior and responsibilities of the ObjectiveCRuntime class.
/// </summary>
public static unsafe class ObjectiveCRuntime {

    /// <summary>
    /// Stores the value associated with <c>ObjCLibrary</c>.
    /// </summary>
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    /// <summary>
    /// Stores the value associated with <c>sel_retain</c>.
    /// </summary>
    private static readonly Selector sel_retain = "retain";

    /// <summary>
    /// Stores the value associated with <c>sel_release</c>.
    /// </summary>
    private static readonly Selector sel_release = "release";

    /// <summary>
    /// Stores the value associated with <c>sel_retainCount</c>.
    /// </summary>
    private static readonly Selector sel_retainCount = "retainCount";

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, float a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, double a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, CGRect a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, uint b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, NSRange b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLSize a, MTLSize b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr c, UIntPtr d, MTLSize e);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLClearColor a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, CGSize a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, UIntPtr b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d, UIntPtr e);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, NSRange a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLCommandBufferHandler a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLViewport a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLScissorRect a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, uint b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, MTLIndexType c, IntPtr d, UIntPtr e, UIntPtr f);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, MTLBuffer b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="g">Specifies the value of <paramref name="g" />.</param>
    /// <param name="h">Specifies the value of <paramref name="h" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, MTLIndexType c, IntPtr d, UIntPtr e, UIntPtr f, IntPtr g, UIntPtr h);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, MTLIndexType b, MTLBuffer c, UIntPtr d, MTLBuffer e, UIntPtr f);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLBuffer a, UIntPtr b, MTLBuffer c, UIntPtr d, UIntPtr e);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="g">Specifies the value of <paramref name="g" />.</param>
    /// <param name="h">Specifies the value of <paramref name="h" />.</param>
    /// <param name="i">Specifies the value of <paramref name="i" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b, UIntPtr c, UIntPtr d, MTLSize e, IntPtr f, UIntPtr g, UIntPtr h, MTLOrigin i);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLRegion a, UIntPtr b, UIntPtr c, IntPtr d, UIntPtr e, UIntPtr f);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <param name="e">Specifies the value of <paramref name="e" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="g">Specifies the value of <paramref name="g" />.</param>
    /// <param name="h">Specifies the value of <paramref name="h" />.</param>
    /// <param name="i">Specifies the value of <paramref name="i" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLTexture a, UIntPtr b, UIntPtr c, MTLOrigin d, MTLSize e, MTLBuffer f, UIntPtr g, UIntPtr h, UIntPtr i);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="sourceTexture">Specifies the value of <paramref name="sourceTexture" />.</param>
    /// <param name="sourceSlice">Specifies the value of <paramref name="sourceSlice" />.</param>
    /// <param name="sourceLevel">Specifies the value of <paramref name="sourceLevel" />.</param>
    /// <param name="sourceOrigin">Specifies the value of <paramref name="sourceOrigin" />.</param>
    /// <param name="sourceSize">Specifies the value of <paramref name="sourceSize" />.</param>
    /// <param name="destinationTexture">Specifies the value of <paramref name="destinationTexture" />.</param>
    /// <param name="destinationSlice">Specifies the value of <paramref name="destinationSlice" />.</param>
    /// <param name="destinationLevel">Specifies the value of <paramref name="destinationLevel" />.</param>
    /// <param name="destinationOrigin">Specifies the value of <paramref name="destinationOrigin" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLTexture destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, MTLOrigin destinationOrigin);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the bytePtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the bytePtr_objc_msgSend operation.</returns>
    public static extern byte* bytePtr_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the CGSize_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the CGSize_objc_msgSend operation.</returns>
    public static extern CGSize CGSize_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the byte_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the byte_objc_msgSend operation.</returns>
    public static extern byte byte_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <returns>Returns the result produced by the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <returns>Returns the result produced by the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <returns>Returns the result produced by the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a, IntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <returns>Returns the result produced by the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, uint a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the uint_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the uint_objc_msgSend operation.</returns>
    public static extern uint uint_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the float_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the float_objc_msgSend operation.</returns>
    public static extern float float_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the CGFloat_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the CGFloat_objc_msgSend operation.</returns>
    public static extern CGFloat CGFloat_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the double_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the double_objc_msgSend operation.</returns>
    public static extern double double_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="error">Specifies the value of <paramref name="error" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, out NSError error);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a, uint b, NSRange c, NSRange d);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="error">Specifies the value of <paramref name="error" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, MTLComputePipelineDescriptor a, uint b, IntPtr c, out NSError error);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="error">Specifies the value of <paramref name="error" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, IntPtr b, out NSError error);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr b, MTLResourceOptions c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <returns>Returns the result produced by the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, MTLResourceOptions c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the UIntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the UIntPtr_objc_msgSend operation.</returns>
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
    /// Executes the string_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the string_objc_msgSend operation.</returns>
    public static string string_objc_msgSend(IntPtr receiver, Selector selector) {
        return objc_msgSend<NSString>(receiver, selector).GetValue();
    }

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, byte b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, Bool8 b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, uint b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <param name="d">Specifies the value of <paramref name="d" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, float a, float b, float c, float d);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend_stret")]

    /// <summary>
    /// Executes the objc_msgSend_stret operation.
    /// </summary>
    /// <param name="retPtr">Specifies the value of <paramref name="retPtr" />.</param>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    public static extern void objc_msgSend_stret(void* retPtr, IntPtr receiver, Selector selector);

    public static T objc_msgSend_stret<T>(IntPtr receiver, Selector selector) where T : struct {
        T ret = default;
        objc_msgSend_stret(Unsafe.AsPointer(ref ret), receiver, selector);
        return ret;
    }

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the MTLClearColor_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the MTLClearColor_objc_msgSend operation.</returns>
    public static extern MTLClearColor MTLClearColor_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the MTLSize_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the MTLSize_objc_msgSend operation.</returns>
    public static extern MTLSize MTLSize_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Executes the CGRect_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the CGRect_objc_msgSend operation.</returns>
    public static extern CGRect CGRect_objc_msgSend(IntPtr receiver, Selector selector);

    // TODO: This should check the current processor type, struct size, etc.
    // At the moment there is no need because all existing occurences of
    // this can safely use the non-stret versions everywhere.
    /// <summary>
    /// Determines whether the architecture-specific stret messaging path should be used for <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The return struct type being marshaled.</typeparam>
    /// <returns><see langword="true" /> when stret dispatch is required; otherwise, <see langword="false" />.</returns>
    public static bool UseStret<T>() {
        return false;
    }

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the sel_registerName operation.
    /// </summary>
    /// <param name="namePtr">Specifies the value of <paramref name="namePtr" />.</param>
    /// <returns>Returns the result produced by the sel_registerName operation.</returns>
    public static extern IntPtr sel_registerName(byte* namePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the sel_getName operation.
    /// </summary>
    /// <param name="selector">Specifies the value of <paramref name="selector" />.</param>
    /// <returns>Returns the result produced by the sel_getName operation.</returns>
    public static extern byte* sel_getName(IntPtr selector);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the objc_getClass operation.
    /// </summary>
    /// <param name="namePtr">Specifies the value of <paramref name="namePtr" />.</param>
    /// <returns>Returns the result produced by the objc_getClass operation.</returns>
    public static extern IntPtr objc_getClass(byte* namePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the object_getClass operation.
    /// </summary>
    /// <param name="obj">Specifies the value of <paramref name="obj" />.</param>
    /// <returns>Returns the result produced by the object_getClass operation.</returns>
    public static extern ObjCClass object_getClass(IntPtr obj);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the class_getProperty operation.
    /// </summary>
    /// <param name="cls">Specifies the value of <paramref name="cls" />.</param>
    /// <param name="namePtr">Specifies the value of <paramref name="namePtr" />.</param>
    /// <returns>Returns the result produced by the class_getProperty operation.</returns>
    public static extern IntPtr class_getProperty(ObjCClass cls, byte* namePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the class_getName operation.
    /// </summary>
    /// <param name="cls">Specifies the value of <paramref name="cls" />.</param>
    /// <returns>Returns the result produced by the class_getName operation.</returns>
    public static extern byte* class_getName(ObjCClass cls);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the property_copyAttributeValue operation.
    /// </summary>
    /// <param name="property">Specifies the value of <paramref name="property" />.</param>
    /// <param name="attributeNamePtr">Specifies the value of <paramref name="attributeNamePtr" />.</param>
    /// <returns>Returns the result produced by the property_copyAttributeValue operation.</returns>
    public static extern byte* property_copyAttributeValue(IntPtr property, byte* attributeNamePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the method_getName operation.
    /// </summary>
    /// <param name="method">Specifies the value of <paramref name="method" />.</param>
    /// <returns>Returns the result produced by the method_getName operation.</returns>
    public static extern Selector method_getName(ObjectiveCMethod method);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the class_copyMethodList operation.
    /// </summary>
    /// <param name="cls">Specifies the value of <paramref name="cls" />.</param>
    /// <param name="outCount">Specifies the value of <paramref name="outCount" />.</param>
    /// <returns>Returns the result produced by the class_copyMethodList operation.</returns>
    public static extern ObjectiveCMethod* class_copyMethodList(ObjCClass cls, out uint outCount);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Executes the free operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    public static extern void free(IntPtr receiver);

    /// <summary>
    /// Executes the retain operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    public static void retain(IntPtr receiver) {
        objc_msgSend(receiver, sel_retain);
    }

    /// <summary>
    /// Executes the release operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    public static void release(IntPtr receiver) {
        objc_msgSend(receiver, sel_release);
    }

    /// <summary>
    /// Executes the GetRetainCount operation.
    /// </summary>
    /// <param name="receiver">Specifies the value of <paramref name="receiver" />.</param>
    /// <returns>Returns the result produced by the GetRetainCount operation.</returns>
    public static ulong GetRetainCount(IntPtr receiver) {
        return UIntPtr_objc_msgSend(receiver, sel_retainCount);
    }
}