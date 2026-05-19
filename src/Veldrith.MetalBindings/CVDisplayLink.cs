using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the CVDisplayLink struct.
/// </summary>
public struct CVDisplayLink {

    /// <summary>
    /// Stores the value associated with <c>CVFramework</c>.
    /// </summary>
    private const string CVFramework = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";

    /// <summary>
    /// Stores the value associated with <c>CGFramework</c>.
    /// </summary>
    private const string CGFramework = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(CVDisplayLink c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CVDisplayLink" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public CVDisplayLink(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the CreateWithActiveCGDisplays operation.
    /// </summary>
    /// <returns>Returns the result produced by the CreateWithActiveCGDisplays operation.</returns>
    public static CVDisplayLink CreateWithActiveCGDisplays() {
        CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink link);
        return link;
    }

    /// <summary>
    /// Executes the SetOutputCallback operation.
    /// </summary>
    /// <param name="callback">Specifies the value of <paramref name="callback" />.</param>
    /// <param name="userData">Specifies the value of <paramref name="userData" />.</param>
    public void SetOutputCallback(CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData) {
        CVDisplayLinkSetOutputCallback(this, callback, userData);
    }

    /// <summary>
    /// Executes the Start operation.
    /// </summary>
    public void Start() {
        CVDisplayLinkStart(this);
    }

    /// <summary>
    /// Executes the UpdateActiveMonitor operation.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="w">Specifies the value of <paramref name="w" />.</param>
    /// <param name="h">Specifies the value of <paramref name="h" />.</param>
    public void UpdateActiveMonitor(int x, int y, int w, int h) {
        uint[] displays = new uint[1];
        uint displayCount = 0;
        CGRect rect = new(new CGPoint(x, y), new CGSize(w, h));
        int err = CGGetDisplaysWithRect(rect, 1, displays, ref displayCount);
        if (err != 0) {
            return;
        }

        if (displayCount > 0) {
            CVDisplayLinkSetCurrentCGDisplay(this, displays[0]);
        }
    }

    [DllImport(CGFramework)]

    /// <summary>
    /// Executes the CGGetDisplaysWithRect operation.
    /// </summary>
    /// <param name="rect">Specifies the value of <paramref name="rect" />.</param>
    /// <param name="maxDisplays">Specifies the value of <paramref name="maxDisplays" />.</param>
    /// <param name="displays">Specifies the value of <paramref name="displays" />.</param>
    /// <param name="displayCount">Specifies the value of <paramref name="displayCount" />.</param>
    /// <returns>Returns the result produced by the CGGetDisplaysWithRect operation.</returns>
    private static extern int CGGetDisplaysWithRect(CGRect rect, int maxDisplays, uint[] displays, ref uint displayCount);

    /// <summary>
    /// Executes the GetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetActualOutputVideoRefreshPeriod operation.</returns>
    public double GetActualOutputVideoRefreshPeriod() {
        return CVDisplayLinkGetActualOutputVideoRefreshPeriod(this);
    }

    /// <summary>
    /// Executes the Stop operation.
    /// </summary>
    public void Stop() {
        CVDisplayLinkStop(this);
    }

    /// <summary>
    /// Executes the Release operation.
    /// </summary>
    public void Release() {
        CVDisplayLinkRelease(this);
    }

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes the CVDisplayLinkCreateWithActiveCGDisplays operation.
    /// </summary>
    /// <param name="displayLink">Specifies the value of <paramref name="displayLink" />.</param>
    /// <returns>Returns the result produced by the CVDisplayLinkCreateWithActiveCGDisplays operation.</returns>
    private static extern int CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes the CVDisplayLinkGetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <param name="displayLink">Specifies the value of <paramref name="displayLink" />.</param>
    /// <returns>Returns the result produced by the CVDisplayLinkGetActualOutputVideoRefreshPeriod operation.</returns>
    private static extern double CVDisplayLinkGetActualOutputVideoRefreshPeriod(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes the CVDisplayLinkSetOutputCallback operation.
    /// </summary>
    /// <param name="displayLink">Specifies the value of <paramref name="displayLink" />.</param>
    /// <param name="callback">Specifies the value of <paramref name="callback" />.</param>
    /// <param name="userData">Specifies the value of <paramref name="userData" />.</param>
    /// <returns>Returns the result produced by the CVDisplayLinkSetOutputCallback operation.</returns>
    private static extern int CVDisplayLinkSetOutputCallback(CVDisplayLink displayLink, CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes the CVDisplayLinkSetCurrentCGDisplay operation.
    /// </summary>
    /// <param name="displayLink">Specifies the value of <paramref name="displayLink" />.</param>
    /// <param name="displayId">Specifies the value of <paramref name="displayId" />.</param>
    /// <returns>Returns the result produced by the CVDisplayLinkSetCurrentCGDisplay operation.</returns>
    private static extern int CVDisplayLinkSetCurrentCGDisplay(CVDisplayLink displayLink, uint displayId);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes the CVDisplayLinkStart operation.
    /// </summary>
    /// <param name="displayLink">Specifies the value of <paramref name="displayLink" />.</param>
    /// <returns>Returns the result produced by the CVDisplayLinkStart operation.</returns>
    private static extern int CVDisplayLinkStart(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes the CVDisplayLinkStop operation.
    /// </summary>
    /// <param name="displayLink">Specifies the value of <paramref name="displayLink" />.</param>
    /// <returns>Returns the result produced by the CVDisplayLinkStop operation.</returns>
    private static extern int CVDisplayLinkStop(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes the CVDisplayLinkRelease operation.
    /// </summary>
    /// <param name="displayLink">Specifies the value of <paramref name="displayLink" />.</param>
    /// <returns>Returns the result produced by the CVDisplayLinkRelease operation.</returns>
    private static extern int CVDisplayLinkRelease(CVDisplayLink displayLink);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate int CVDisplayLinkOutputCallbackDelegate(CVDisplayLink displayLink, CVTimeStamp* inNow, CVTimeStamp* inOutputTime, long flagsIn, long flagsOut, IntPtr userData);