using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the ObjectiveCRuntime class.
/// </summary>
public static unsafe class ObjectiveCRuntime {

    /// <summary>
    /// Represents the ObjCLibrary field.
    /// </summary>
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    /// <summary>
    /// Represents the sel_retain field.
    /// </summary>
    private static readonly Selector sel_retain = "retain";

    /// <summary>
    /// Represents the sel_release field.
    /// </summary>
    private static readonly Selector sel_release = "release";

    /// <summary>
    /// Represents the sel_retainCount field.
    /// </summary>
    private static readonly Selector sel_retainCount = "retainCount";

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, float a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, double a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, CGRect a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, uint b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, NSRange b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLSize a, MTLSize b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr c, UIntPtr d, MTLSize e);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLClearColor a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, CGSize a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, UIntPtr b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d, UIntPtr e);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, NSRange a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLCommandBufferHandler a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLViewport a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLScissorRect a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, uint b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    /// <param name="f">The value of f.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, MTLIndexType c, IntPtr d, UIntPtr e, UIntPtr f);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, MTLBuffer b, UIntPtr c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="g">The value of g.</param>
    /// <param name="h">The value of h.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, MTLIndexType c, IntPtr d, UIntPtr e, UIntPtr f, IntPtr g, UIntPtr h);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    /// <param name="f">The value of f.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, MTLIndexType b, MTLBuffer c, UIntPtr d, MTLBuffer e, UIntPtr f);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLBuffer a, UIntPtr b, MTLBuffer c, UIntPtr d, UIntPtr e);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="g">The value of g.</param>
    /// <param name="h">The value of h.</param>
    /// <param name="i">The value of i.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b, UIntPtr c, UIntPtr d, MTLSize e, IntPtr f, UIntPtr g, UIntPtr h, MTLOrigin i);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    /// <param name="f">The value of f.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLRegion a, UIntPtr b, UIntPtr c, IntPtr d, UIntPtr e, UIntPtr f);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <param name="e">The value of e.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="g">The value of g.</param>
    /// <param name="h">The value of h.</param>
    /// <param name="i">The value of i.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLTexture a, UIntPtr b, UIntPtr c, MTLOrigin d, MTLSize e, MTLBuffer f, UIntPtr g, UIntPtr h, UIntPtr i);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="sourceTexture">The value of sourceTexture.</param>
    /// <param name="sourceSlice">The value of sourceSlice.</param>
    /// <param name="sourceLevel">The value of sourceLevel.</param>
    /// <param name="sourceOrigin">The value of sourceOrigin.</param>
    /// <param name="sourceSize">The value of sourceSize.</param>
    /// <param name="destinationTexture">The value of destinationTexture.</param>
    /// <param name="destinationSlice">The value of destinationSlice.</param>
    /// <param name="destinationLevel">The value of destinationLevel.</param>
    /// <param name="destinationOrigin">The value of destinationOrigin.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, MTLTexture sourceTexture, UIntPtr sourceSlice, UIntPtr sourceLevel, MTLOrigin sourceOrigin, MTLSize sourceSize, MTLTexture destinationTexture, UIntPtr destinationSlice, UIntPtr destinationLevel, MTLOrigin destinationOrigin);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the bytePtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the bytePtr_objc_msgSend operation.</returns>
    public static extern byte* bytePtr_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the CGSize_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the CGSize_objc_msgSend operation.</returns>
    public static extern CGSize CGSize_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the byte_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the byte_objc_msgSend operation.</returns>
    public static extern byte byte_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <returns>The result of the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <returns>The result of the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <returns>The result of the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a, IntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the bool8_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <returns>The result of the bool8_objc_msgSend operation.</returns>
    public static extern Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, uint a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the uint_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the uint_objc_msgSend operation.</returns>
    public static extern uint uint_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the float_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the float_objc_msgSend operation.</returns>
    public static extern float float_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the CGFloat_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the CGFloat_objc_msgSend operation.</returns>
    public static extern CGFloat CGFloat_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the double_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the double_objc_msgSend operation.</returns>
    public static extern double double_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="error">The value of error.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, out NSError error);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a, uint b, NSRange c, NSRange d);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="error">The value of error.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, MTLComputePipelineDescriptor a, uint b, IntPtr c, out NSError error);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="error">The value of error.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, IntPtr b, out NSError error);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr b, MTLResourceOptions c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the IntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <returns>The result of the IntPtr_objc_msgSend operation.</returns>
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, MTLResourceOptions c);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the UIntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the UIntPtr_objc_msgSend operation.</returns>
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
    /// Performs the string_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the string_objc_msgSend operation.</returns>
    public static string string_objc_msgSend(IntPtr receiver, Selector selector) {
        return objc_msgSend<NSString>(receiver, selector).GetValue();
    }

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, byte b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, Bool8 b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, uint b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="a">The value of a.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="c">The value of c.</param>
    /// <param name="d">The value of d.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, float a, float b, float c, float d);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <param name="b">The value of b.</param>
    public static extern void objc_msgSend(IntPtr receiver, Selector selector, IntPtr b);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend_stret")]

    /// <summary>
    /// Performs the objc_msgSend_stret operation.
    /// </summary>
    /// <param name="retPtr">The value of retPtr.</param>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    public static extern void objc_msgSend_stret(void* retPtr, IntPtr receiver, Selector selector);

    public static T objc_msgSend_stret<T>(IntPtr receiver, Selector selector) where T : struct {
        T ret = default;
        objc_msgSend_stret(Unsafe.AsPointer(ref ret), receiver, selector);
        return ret;
    }

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the MTLClearColor_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the MTLClearColor_objc_msgSend operation.</returns>
    public static extern MTLClearColor MTLClearColor_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the MTLSize_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the MTLSize_objc_msgSend operation.</returns>
    public static extern MTLSize MTLSize_objc_msgSend(IntPtr receiver, Selector selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]

    /// <summary>
    /// Performs the CGRect_objc_msgSend operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the CGRect_objc_msgSend operation.</returns>
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
    /// Performs the sel_registerName operation.
    /// </summary>
    /// <param name="namePtr">The value of namePtr.</param>
    /// <returns>The result of the sel_registerName operation.</returns>
    public static extern IntPtr sel_registerName(byte* namePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the sel_getName operation.
    /// </summary>
    /// <param name="selector">The value of selector.</param>
    /// <returns>The result of the sel_getName operation.</returns>
    public static extern byte* sel_getName(IntPtr selector);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the objc_getClass operation.
    /// </summary>
    /// <param name="namePtr">The value of namePtr.</param>
    /// <returns>The result of the objc_getClass operation.</returns>
    public static extern IntPtr objc_getClass(byte* namePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the object_getClass operation.
    /// </summary>
    /// <param name="obj">The value of obj.</param>
    /// <returns>The result of the object_getClass operation.</returns>
    public static extern ObjCClass object_getClass(IntPtr obj);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the class_getProperty operation.
    /// </summary>
    /// <param name="cls">The value of cls.</param>
    /// <param name="namePtr">The value of namePtr.</param>
    /// <returns>The result of the class_getProperty operation.</returns>
    public static extern IntPtr class_getProperty(ObjCClass cls, byte* namePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the class_getName operation.
    /// </summary>
    /// <param name="cls">The value of cls.</param>
    /// <returns>The result of the class_getName operation.</returns>
    public static extern byte* class_getName(ObjCClass cls);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the property_copyAttributeValue operation.
    /// </summary>
    /// <param name="property">The value of property.</param>
    /// <param name="attributeNamePtr">The value of attributeNamePtr.</param>
    /// <returns>The result of the property_copyAttributeValue operation.</returns>
    public static extern byte* property_copyAttributeValue(IntPtr property, byte* attributeNamePtr);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the method_getName operation.
    /// </summary>
    /// <param name="method">The value of method.</param>
    /// <returns>The result of the method_getName operation.</returns>
    public static extern Selector method_getName(ObjectiveCMethod method);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the class_copyMethodList operation.
    /// </summary>
    /// <param name="cls">The value of cls.</param>
    /// <param name="outCount">The value of outCount.</param>
    /// <returns>The result of the class_copyMethodList operation.</returns>
    public static extern ObjectiveCMethod* class_copyMethodList(ObjCClass cls, out uint outCount);

    [DllImport(ObjCLibrary)]

    /// <summary>
    /// Performs the free operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    public static extern void free(IntPtr receiver);

    /// <summary>
    /// Performs the retain operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    public static void retain(IntPtr receiver) {
        objc_msgSend(receiver, sel_retain);
    }

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    public static void release(IntPtr receiver) {
        objc_msgSend(receiver, sel_release);
    }

    /// <summary>
    /// Performs the GetRetainCount operation.
    /// </summary>
    /// <param name="receiver">The value of receiver.</param>
    /// <returns>The result of the GetRetainCount operation.</returns>
    public static ulong GetRetainCount(IntPtr receiver) {
        return UIntPtr_objc_msgSend(receiver, sel_retainCount);
    }
}