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
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(CVDisplayLink c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CVDisplayLink" /> class.
    /// </summary>
    public CVDisplayLink(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes CreateWithActiveCGDisplays.
    /// </summary>
    public static CVDisplayLink CreateWithActiveCGDisplays() {
        CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink link);
        return link;
    }

    /// <summary>
    /// Executes SetOutputCallback.
    /// </summary>
    public void SetOutputCallback(CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData) {
        CVDisplayLinkSetOutputCallback(this, callback, userData);
    }

    /// <summary>
    /// Executes Start.
    /// </summary>
    public void Start() {
        CVDisplayLinkStart(this);
    }

    /// <summary>
    /// Executes UpdateActiveMonitor.
    /// </summary>
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
    /// Executes CGGetDisplaysWithRect.
    /// </summary>
    private static extern int CGGetDisplaysWithRect(CGRect rect, int maxDisplays, uint[] displays, ref uint displayCount);

    /// <summary>
    /// Executes GetActualOutputVideoRefreshPeriod.
    /// </summary>
    public double GetActualOutputVideoRefreshPeriod() {
        return CVDisplayLinkGetActualOutputVideoRefreshPeriod(this);
    }

    /// <summary>
    /// Executes Stop.
    /// </summary>
    public void Stop() {
        CVDisplayLinkStop(this);
    }

    /// <summary>
    /// Executes Release.
    /// </summary>
    public void Release() {
        CVDisplayLinkRelease(this);
    }

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes CVDisplayLinkCreateWithActiveCGDisplays.
    /// </summary>
    private static extern int CVDisplayLinkCreateWithActiveCGDisplays(out CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes CVDisplayLinkGetActualOutputVideoRefreshPeriod.
    /// </summary>
    private static extern double CVDisplayLinkGetActualOutputVideoRefreshPeriod(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes CVDisplayLinkSetOutputCallback.
    /// </summary>
    private static extern int CVDisplayLinkSetOutputCallback(CVDisplayLink displayLink, CVDisplayLinkOutputCallbackDelegate callback, IntPtr userData);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes CVDisplayLinkSetCurrentCGDisplay.
    /// </summary>
    private static extern int CVDisplayLinkSetCurrentCGDisplay(CVDisplayLink displayLink, uint displayId);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes CVDisplayLinkStart.
    /// </summary>
    private static extern int CVDisplayLinkStart(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes CVDisplayLinkStop.
    /// </summary>
    private static extern int CVDisplayLinkStop(CVDisplayLink displayLink);

    [DllImport(CVFramework)]

    /// <summary>
    /// Executes CVDisplayLinkRelease.
    /// </summary>
    private static extern int CVDisplayLinkRelease(CVDisplayLink displayLink);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate int CVDisplayLinkOutputCallbackDelegate(CVDisplayLink displayLink, CVTimeStamp* inNow, CVTimeStamp* inOutputTime, long flagsIn, long flagsOut, IntPtr userData);