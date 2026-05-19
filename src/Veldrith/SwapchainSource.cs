using System;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the SwapchainSource class.
/// </summary>
public abstract class SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapchainSource" /> type.
    /// </summary>
    internal SwapchainSource() { }

    /// <summary>
    /// Executes the CreateWin32 operation.
    /// </summary>
    /// <param name="hwnd">Specifies the value of <paramref name="hwnd" />.</param>
    /// <param name="hinstance">Specifies the value of <paramref name="hinstance" />.</param>
    /// <returns>Returns the result produced by the CreateWin32 operation.</returns>
    public static SwapchainSource CreateWin32(IntPtr hwnd, IntPtr hinstance) {
        return new Win32SwapchainSource(hwnd, hinstance);
    }

    /// <summary>
    /// Executes the CreateUwp operation.
    /// </summary>
    /// <param name="swapChainPanel">Specifies the value of <paramref name="swapChainPanel" />.</param>
    /// <param name="logicalDpi">Specifies the value of <paramref name="logicalDpi" />.</param>
    /// <returns>Returns the result produced by the CreateUwp operation.</returns>
    public static SwapchainSource CreateUwp(object swapChainPanel, float logicalDpi) {
        return new UwpSwapchainSource(swapChainPanel, logicalDpi);
    }

    /// <summary>
    /// Executes the CreateXlib operation.
    /// </summary>
    /// <param name="display">Specifies the value of <paramref name="display" />.</param>
    /// <param name="window">Specifies the value of <paramref name="window" />.</param>
    /// <returns>Returns the result produced by the CreateXlib operation.</returns>
    public static SwapchainSource CreateXlib(IntPtr display, IntPtr window) {
        return new XlibSwapchainSource(display, window);
    }

    /// <summary>
    /// Executes the CreateWayland operation.
    /// </summary>
    /// <param name="display">Specifies the value of <paramref name="display" />.</param>
    /// <param name="surface">Specifies the value of <paramref name="surface" />.</param>
    /// <returns>Returns the result produced by the CreateWayland operation.</returns>
    public static SwapchainSource CreateWayland(IntPtr display, IntPtr surface) {
        return new WaylandSwapchainSource(display, surface);
    }

    /// <summary>
    /// Executes the CreateNSWindow operation.
    /// </summary>
    /// <param name="nsWindow">Specifies the value of <paramref name="nsWindow" />.</param>
    /// <returns>Returns the result produced by the CreateNSWindow operation.</returns>
    public static SwapchainSource CreateNSWindow(IntPtr nsWindow) {
        return new NSWindowSwapchainSource(nsWindow);
    }

    /// <summary>
    /// Executes the CreateUIView operation.
    /// </summary>
    /// <param name="uiView">Specifies the value of <paramref name="uiView" />.</param>
    /// <returns>Returns the result produced by the CreateUIView operation.</returns>
    public static SwapchainSource CreateUIView(IntPtr uiView) {
        return new UIViewSwapchainSource(uiView);
    }

    /// <summary>
    /// Executes the CreateAndroidSurface operation.
    /// </summary>
    /// <param name="surfaceHandle">Specifies the value of <paramref name="surfaceHandle" />.</param>
    /// <param name="jniEnv">Specifies the value of <paramref name="jniEnv" />.</param>
    /// <returns>Returns the result produced by the CreateAndroidSurface operation.</returns>
    public static SwapchainSource CreateAndroidSurface(IntPtr surfaceHandle, IntPtr jniEnv) {
        return new AndroidSurfaceSwapchainSource(surfaceHandle, jniEnv);
    }

    /// <summary>
    /// Executes the CreateNSView operation.
    /// </summary>
    /// <param name="nsView">Specifies the value of <paramref name="nsView" />.</param>
    /// <returns>Returns the result produced by the CreateNSView operation.</returns>
    public static SwapchainSource CreateNSView(IntPtr nsView) {
        return new NSViewSwapchainSource(nsView);
    }
}

/// <summary>
/// Defines the behavior and responsibilities of the Win32SwapchainSource class.
/// </summary>
internal class Win32SwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32SwapchainSource" /> type.
    /// </summary>
    /// <param name="hwnd">Specifies the value of <paramref name="hwnd" />.</param>
    /// <param name="hinstance">Specifies the value of <paramref name="hinstance" />.</param>
    public Win32SwapchainSource(IntPtr hwnd, IntPtr hinstance) {
        this.Hwnd = hwnd;
        this.Hinstance = hinstance;
    }

    /// <summary>
    /// Gets or sets Hwnd.
    /// </summary>
    public IntPtr Hwnd { get; }

    /// <summary>
    /// Gets or sets Hinstance.
    /// </summary>
    public IntPtr Hinstance { get; }
}

/// <summary>
/// Defines the behavior and responsibilities of the UwpSwapchainSource class.
/// </summary>
internal class UwpSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UwpSwapchainSource" /> type.
    /// </summary>
    /// <param name="swapChainPanelNative">Specifies the value of <paramref name="swapChainPanelNative" />.</param>
    /// <param name="logicalDpi">Specifies the value of <paramref name="logicalDpi" />.</param>
    public UwpSwapchainSource(object swapChainPanelNative, float logicalDpi) {
        this.SwapChainPanelNative = swapChainPanelNative;
        this.LogicalDpi = logicalDpi;
    }

    /// <summary>
    /// Gets or sets SwapChainPanelNative.
    /// </summary>
    public object SwapChainPanelNative { get; }

    /// <summary>
    /// Gets or sets LogicalDpi.
    /// </summary>
    public float LogicalDpi { get; }
}

/// <summary>
/// Defines the behavior and responsibilities of the XlibSwapchainSource class.
/// </summary>
internal class XlibSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibSwapchainSource" /> type.
    /// </summary>
    /// <param name="display">Specifies the value of <paramref name="display" />.</param>
    /// <param name="window">Specifies the value of <paramref name="window" />.</param>
    public XlibSwapchainSource(IntPtr display, IntPtr window) {
        this.Display = display;
        this.Window = window;
    }

    /// <summary>
    /// Gets or sets Display.
    /// </summary>
    public IntPtr Display { get; }

    /// <summary>
    /// Gets or sets Window.
    /// </summary>
    public IntPtr Window { get; }
}

/// <summary>
/// Defines the behavior and responsibilities of the WaylandSwapchainSource class.
/// </summary>
internal class WaylandSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="WaylandSwapchainSource" /> type.
    /// </summary>
    /// <param name="display">Specifies the value of <paramref name="display" />.</param>
    /// <param name="surface">Specifies the value of <paramref name="surface" />.</param>
    public WaylandSwapchainSource(IntPtr display, IntPtr surface) {
        this.Display = display;
        this.Surface = surface;
    }

    /// <summary>
    /// Gets or sets Display.
    /// </summary>
    public IntPtr Display { get; }

    /// <summary>
    /// Gets or sets Surface.
    /// </summary>
    public IntPtr Surface { get; }
}

/// <summary>
/// Defines the behavior and responsibilities of the NSWindowSwapchainSource class.
/// </summary>
internal class NSWindowSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSWindowSwapchainSource" /> type.
    /// </summary>
    /// <param name="nsWindow">Specifies the value of <paramref name="nsWindow" />.</param>
    public NSWindowSwapchainSource(IntPtr nsWindow) {
        this.NSWindow = nsWindow;
    }

    /// <summary>
    /// Gets or sets NSWindow.
    /// </summary>
    public IntPtr NSWindow { get; }
}

/// <summary>
/// Defines the behavior and responsibilities of the UIViewSwapchainSource class.
/// </summary>
internal class UIViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UIViewSwapchainSource" /> type.
    /// </summary>
    /// <param name="uiView">Specifies the value of <paramref name="uiView" />.</param>
    public UIViewSwapchainSource(IntPtr uiView) {
        this.UIView = uiView;
    }

    /// <summary>
    /// Gets or sets UIView.
    /// </summary>
    public IntPtr UIView { get; }
}

/// <summary>
/// Defines the behavior and responsibilities of the AndroidSurfaceSwapchainSource class.
/// </summary>
internal class AndroidSurfaceSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidSurfaceSwapchainSource" /> type.
    /// </summary>
    /// <param name="surfaceHandle">Specifies the value of <paramref name="surfaceHandle" />.</param>
    /// <param name="jniEnv">Specifies the value of <paramref name="jniEnv" />.</param>
    public AndroidSurfaceSwapchainSource(IntPtr surfaceHandle, IntPtr jniEnv) {
        this.Surface = surfaceHandle;
        this.JniEnv = jniEnv;
    }

    /// <summary>
    /// Gets or sets Surface.
    /// </summary>
    public IntPtr Surface { get; }

    /// <summary>
    /// Gets or sets JniEnv.
    /// </summary>
    public IntPtr JniEnv { get; }
}

/// <summary>
/// Defines the behavior and responsibilities of the NSViewSwapchainSource class.
/// </summary>
internal class NSViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSViewSwapchainSource" /> type.
    /// </summary>
    /// <param name="nsView">Specifies the value of <paramref name="nsView" />.</param>
    public NSViewSwapchainSource(IntPtr nsView) {
        this.NSView = nsView;
    }

    /// <summary>
    /// Gets or sets NSView.
    /// </summary>
    public IntPtr NSView { get; }
}