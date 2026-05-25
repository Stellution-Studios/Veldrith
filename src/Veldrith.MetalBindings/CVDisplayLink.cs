using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CVDisplayLink data structure used by the graphics runtime.
/// </summary>
public struct CVDisplayLink {

    /// <summary>
    /// Stores the cvframework state used by this instance.
    /// </summary>
    private const string CVFramework = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";

    /// <summary>
    /// Stores the cgframework state used by this instance.
    /// </summary>
    private const string CGFramework = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="c">The c value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(CVDisplayLink c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CVDisplayLink" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public CVDisplayLink(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Creates the with active cgdisplays instance used by this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static CVDisplayLink CreateWithActiveCGDisplays() {
        CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink link);
        return link;
    }

    /// <summary>
    /// Sets the output callback value.
    /// </summary>
    /// <param name="callback">The callback value used by this operation.</param>
    /// <param name="userData">The user data value used by this operation.</param>
    public void SetOutputCallback(CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData) {
        CVDisplayLinkSetOutputCallback(this, callback, userData);
    }

    /// <summary>
    /// Executes the start logic for this backend.
    /// </summary>
    public void Start() {
        CVDisplayLinkStart(this);
    }

    /// <summary>
    /// Updates the active monitor state for this command sequence.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="w">The w value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
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
    
    /// <summary>
    /// Executes the cgget displays with rect logic for this backend.
    /// </summary>
    /// <param name="rect">The rect value used by this operation.</param>
    /// <param name="maxDisplays">The max displays value used by this operation.</param>
    /// <param name="displays">The displays value used by this operation.</param>
    /// <param name="displayCount">The display count value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CGFramework)]
    private static extern int CGGetDisplaysWithRect(CGRect rect, int maxDisplays, uint[] displays, ref uint displayCount);

    /// <summary>
    /// Gets the actual output video refresh period value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public double GetActualOutputVideoRefreshPeriod() {
        return CVDisplayLinkGetActualOutputVideoRefreshPeriod(this);
    }

    /// <summary>
    /// Executes the stop logic for this backend.
    /// </summary>
    public void Stop() {
        CVDisplayLinkStop(this);
    }

    /// <summary>
    /// Executes the release logic for this backend.
    /// </summary>
    public void Release() {
        CVDisplayLinkRelease(this);
    }

    /// <summary>
    /// Executes the cvdisplay link create with active cgdisplays logic for this backend.
    /// </summary>
    /// <param name="displayLink">The display link value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CVFramework)]
    private static extern int CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink displayLink);

    /// <summary>
    /// Executes the cvdisplay link get actual output video refresh period logic for this backend.
    /// </summary>
    /// <param name="displayLink">The display link value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CVFramework)]
    private static extern double CVDisplayLinkGetActualOutputVideoRefreshPeriod(CVDisplayLink displayLink);

    /// <summary>
    /// Executes the cvdisplay link set output callback logic for this backend.
    /// </summary>
    /// <param name="displayLink">The display link value used by this operation.</param>
    /// <param name="callback">The callback value used by this operation.</param>
    /// <param name="userData">The user data value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CVFramework)]
    private static extern int CVDisplayLinkSetOutputCallback(CVDisplayLink displayLink, CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData);

    /// <summary>
    /// Executes the cvdisplay link set current cgdisplay logic for this backend.
    /// </summary>
    /// <param name="displayLink">The display link value used by this operation.</param>
    /// <param name="displayId">The display id value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CVFramework)]
    private static extern int CVDisplayLinkSetCurrentCGDisplay(CVDisplayLink displayLink, uint displayId);

    /// <summary>
    /// Executes the cvdisplay link start logic for this backend.
    /// </summary>
    /// <param name="displayLink">The display link value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CVFramework)]
    private static extern int CVDisplayLinkStart(CVDisplayLink displayLink);

    /// <summary>
    /// Executes the cvdisplay link stop logic for this backend.
    /// </summary>
    /// <param name="displayLink">The display link value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CVFramework)]
    private static extern int CVDisplayLinkStop(CVDisplayLink displayLink);

    /// <summary>
    /// Executes the cvdisplay link release logic for this backend.
    /// </summary>
    /// <param name="displayLink">The display link value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(CVFramework)]
    private static extern int CVDisplayLinkRelease(CVDisplayLink displayLink);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate int CVDisplayLinkOutputCallbackDelegate(CVDisplayLink displayLink, CVTimeStamp* inNow, CVTimeStamp* inOutputTime, long flagsIn, long flagsOut, IntPtr userData);