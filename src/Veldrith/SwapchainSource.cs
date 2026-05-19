using System;

namespace Veldrith;

/// <summary>
/// Represents the SwapchainSource class.
/// </summary>
public abstract class SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapchainSource" /> type.
    /// </summary>
    internal SwapchainSource() { }

    /// <summary>
    /// Performs the CreateWin32 operation.
    /// </summary>
    /// <param name="hwnd">The value of hwnd.</param>
    /// <param name="hinstance">The value of hinstance.</param>
    /// <returns>The result of the CreateWin32 operation.</returns>
    public static SwapchainSource CreateWin32(IntPtr hwnd, IntPtr hinstance) {
        return new Win32SwapchainSource(hwnd, hinstance);
    }

    /// <summary>
    /// Performs the CreateUwp operation.
    /// </summary>
    /// <param name="swapChainPanel">The value of swapChainPanel.</param>
    /// <param name="logicalDpi">The value of logicalDpi.</param>
    /// <returns>The result of the CreateUwp operation.</returns>
    public static SwapchainSource CreateUwp(object swapChainPanel, float logicalDpi) {
        return new UwpSwapchainSource(swapChainPanel, logicalDpi);
    }

    /// <summary>
    /// Performs the CreateXlib operation.
    /// </summary>
    /// <param name="display">The value of display.</param>
    /// <param name="window">The value of window.</param>
    /// <returns>The result of the CreateXlib operation.</returns>
    public static SwapchainSource CreateXlib(IntPtr display, IntPtr window) {
        return new XlibSwapchainSource(display, window);
    }

    /// <summary>
    /// Performs the CreateWayland operation.
    /// </summary>
    /// <param name="display">The value of display.</param>
    /// <param name="surface">The value of surface.</param>
    /// <returns>The result of the CreateWayland operation.</returns>
    public static SwapchainSource CreateWayland(IntPtr display, IntPtr surface) {
        return new WaylandSwapchainSource(display, surface);
    }

    /// <summary>
    /// Performs the CreateNSWindow operation.
    /// </summary>
    /// <param name="nsWindow">The value of nsWindow.</param>
    /// <returns>The result of the CreateNSWindow operation.</returns>
    public static SwapchainSource CreateNSWindow(IntPtr nsWindow) {
        return new NSWindowSwapchainSource(nsWindow);
    }

    /// <summary>
    /// Performs the CreateUIView operation.
    /// </summary>
    /// <param name="uiView">The value of uiView.</param>
    /// <returns>The result of the CreateUIView operation.</returns>
    public static SwapchainSource CreateUIView(IntPtr uiView) {
        return new UIViewSwapchainSource(uiView);
    }

    /// <summary>
    /// Performs the CreateAndroidSurface operation.
    /// </summary>
    /// <param name="surfaceHandle">The value of surfaceHandle.</param>
    /// <param name="jniEnv">The value of jniEnv.</param>
    /// <returns>The result of the CreateAndroidSurface operation.</returns>
    public static SwapchainSource CreateAndroidSurface(IntPtr surfaceHandle, IntPtr jniEnv) {
        return new AndroidSurfaceSwapchainSource(surfaceHandle, jniEnv);
    }

    /// <summary>
    /// Performs the CreateNSView operation.
    /// </summary>
    /// <param name="nsView">The value of nsView.</param>
    /// <returns>The result of the CreateNSView operation.</returns>
    public static SwapchainSource CreateNSView(IntPtr nsView) {
        return new NSViewSwapchainSource(nsView);
    }
}

/// <summary>
/// Represents the Win32SwapchainSource class.
/// </summary>
internal class Win32SwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32SwapchainSource" /> type.
    /// </summary>
    /// <param name="hwnd">The value of hwnd.</param>
    /// <param name="hinstance">The value of hinstance.</param>
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
/// Represents the UwpSwapchainSource class.
/// </summary>
internal class UwpSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UwpSwapchainSource" /> type.
    /// </summary>
    /// <param name="swapChainPanelNative">The value of swapChainPanelNative.</param>
    /// <param name="logicalDpi">The value of logicalDpi.</param>
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
/// Represents the XlibSwapchainSource class.
/// </summary>
internal class XlibSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibSwapchainSource" /> type.
    /// </summary>
    /// <param name="display">The value of display.</param>
    /// <param name="window">The value of window.</param>
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
/// Represents the WaylandSwapchainSource class.
/// </summary>
internal class WaylandSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="WaylandSwapchainSource" /> type.
    /// </summary>
    /// <param name="display">The value of display.</param>
    /// <param name="surface">The value of surface.</param>
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
/// Represents the NSWindowSwapchainSource class.
/// </summary>
internal class NSWindowSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSWindowSwapchainSource" /> type.
    /// </summary>
    /// <param name="nsWindow">The value of nsWindow.</param>
    public NSWindowSwapchainSource(IntPtr nsWindow) {
        this.NSWindow = nsWindow;
    }

    /// <summary>
    /// Gets or sets NSWindow.
    /// </summary>
    public IntPtr NSWindow { get; }
}

/// <summary>
/// Represents the UIViewSwapchainSource class.
/// </summary>
internal class UIViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UIViewSwapchainSource" /> type.
    /// </summary>
    /// <param name="uiView">The value of uiView.</param>
    public UIViewSwapchainSource(IntPtr uiView) {
        this.UIView = uiView;
    }

    /// <summary>
    /// Gets or sets UIView.
    /// </summary>
    public IntPtr UIView { get; }
}

/// <summary>
/// Represents the AndroidSurfaceSwapchainSource class.
/// </summary>
internal class AndroidSurfaceSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidSurfaceSwapchainSource" /> type.
    /// </summary>
    /// <param name="surfaceHandle">The value of surfaceHandle.</param>
    /// <param name="jniEnv">The value of jniEnv.</param>
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
/// Represents the NSViewSwapchainSource class.
/// </summary>
internal class NSViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSViewSwapchainSource" /> type.
    /// </summary>
    /// <param name="nsView">The value of nsView.</param>
    public NSViewSwapchainSource(IntPtr nsView) {
        this.NSView = nsView;
    }

    /// <summary>
    /// Gets or sets NSView.
    /// </summary>
    public IntPtr NSView { get; }
}