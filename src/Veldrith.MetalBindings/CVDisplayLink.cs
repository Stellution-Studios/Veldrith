using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CVDisplayLink struct.
/// </summary>
public struct CVDisplayLink {

    /// <summary>
    /// Represents the CVFramework field.
    /// </summary>
    private const string CVFramework = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";

    /// <summary>
    /// Represents the CGFramework field.
    /// </summary>
    private const string CGFramework = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Performs the operator IntPtr operation.
    /// </summary>
    /// <param name="c">The value of c.</param>
    /// <returns>The result of the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(CVDisplayLink c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CVDisplayLink" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public CVDisplayLink(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the CreateWithActiveCGDisplays operation.
    /// </summary>
    /// <returns>The result of the CreateWithActiveCGDisplays operation.</returns>
    public static CVDisplayLink CreateWithActiveCGDisplays() {
        CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink link);
        return link;
    }

    /// <summary>
    /// Performs the SetOutputCallback operation.
    /// </summary>
    /// <param name="callback">The value of callback.</param>
    /// <param name="userData">The value of userData.</param>
    public void SetOutputCallback(CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData) {
        CVDisplayLinkSetOutputCallback(this, callback, userData);
    }

    /// <summary>
    /// Performs the Start operation.
    /// </summary>
    public void Start() {
        CVDisplayLinkStart(this);
    }

    /// <summary>
    /// Performs the UpdateActiveMonitor operation.
    /// </summary>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="w">The value of w.</param>
    /// <param name="h">The value of h.</param>
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
    /// Performs the CGGetDisplaysWithRect operation.
    /// </summary>
    /// <param name="rect">The value of rect.</param>
    /// <param name="maxDisplays">The value of maxDisplays.</param>
    /// <param name="displays">The value of displays.</param>
    /// <param name="displayCount">The value of displayCount.</param>
    /// <returns>The result of the CGGetDisplaysWithRect operation.</returns>
    private static extern int CGGetDisplaysWithRect(CGRect rect, int maxDisplays, uint[] displays, ref uint displayCount);

    /// <summary>
    /// Performs the GetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <returns>The result of the GetActualOutputVideoRefreshPeriod operation.</returns>
    public double GetActualOutputVideoRefreshPeriod() {
        return CVDisplayLinkGetActualOutputVideoRefreshPeriod(this);
    }

    /// <summary>
    /// Performs the Stop operation.
    /// </summary>
    public void Stop() {
        CVDisplayLinkStop(this);
    }

    /// <summary>
    /// Performs the Release operation.
    /// </summary>
    public void Release() {
        CVDisplayLinkRelease(this);
    }

    [DllImport(CVFramework)]

    /// <summary>
    /// Performs the CVDisplayLinkCreateWithActiveCGDisplays operation.
    /// </summary>
    /// <param name="displayLink">The value of displayLink.</param>
    /// <returns>The result of the CVDisplayLinkCreateWithActiveCGDisplays operation.</returns>
    private static extern int CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Performs the CVDisplayLinkGetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <param name="displayLink">The value of displayLink.</param>
    /// <returns>The result of the CVDisplayLinkGetActualOutputVideoRefreshPeriod operation.</returns>
    private static extern double CVDisplayLinkGetActualOutputVideoRefreshPeriod(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Performs the CVDisplayLinkSetOutputCallback operation.
    /// </summary>
    /// <param name="displayLink">The value of displayLink.</param>
    /// <param name="callback">The value of callback.</param>
    /// <param name="userData">The value of userData.</param>
    /// <returns>The result of the CVDisplayLinkSetOutputCallback operation.</returns>
    private static extern int CVDisplayLinkSetOutputCallback(CVDisplayLink displayLink, CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData);

    [DllImport(CVFramework)]

    /// <summary>
    /// Performs the CVDisplayLinkSetCurrentCGDisplay operation.
    /// </summary>
    /// <param name="displayLink">The value of displayLink.</param>
    /// <param name="displayId">The value of displayId.</param>
    /// <returns>The result of the CVDisplayLinkSetCurrentCGDisplay operation.</returns>
    private static extern int CVDisplayLinkSetCurrentCGDisplay(CVDisplayLink displayLink, uint displayId);

    [DllImport(CVFramework)]

    /// <summary>
    /// Performs the CVDisplayLinkStart operation.
    /// </summary>
    /// <param name="displayLink">The value of displayLink.</param>
    /// <returns>The result of the CVDisplayLinkStart operation.</returns>
    private static extern int CVDisplayLinkStart(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Performs the CVDisplayLinkStop operation.
    /// </summary>
    /// <param name="displayLink">The value of displayLink.</param>
    /// <returns>The result of the CVDisplayLinkStop operation.</returns>
    private static extern int CVDisplayLinkStop(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Performs the CVDisplayLinkRelease operation.
    /// </summary>
    /// <param name="displayLink">The value of displayLink.</param>
    /// <returns>The result of the CVDisplayLinkRelease operation.</returns>
    private static extern int CVDisplayLinkRelease(CVDisplayLink displayLink);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate int CVDisplayLinkOutputCallbackDelegate(CVDisplayLink displayLink, CVTimeStamp* inNow, CVTimeStamp* inOutputTime, long flagsIn, long flagsOut, IntPtr userData);