using System;

namespace Veldrith;

/// <summary>
/// Represents the SwapchainSource type used by the graphics runtime.
/// </summary>
public abstract class SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapchainSource" /> type.
    /// </summary>
    internal SwapchainSource() { }

    /// <summary>
    /// Creates the win32 instance used by this backend.
    /// </summary>
    /// <param name="hwnd">The hwnd value used by this operation.</param>
    /// <param name="hinstance">The hinstance value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateWin32(IntPtr hwnd, IntPtr hinstance) {
        return new Win32SwapchainSource(hwnd, hinstance);
    }

    /// <summary>
    /// Creates the uwp instance used by this backend.
    /// </summary>
    /// <param name="swapChainPanel">The swap chain panel value used by this operation.</param>
    /// <param name="logicalDpi">The logical dpi value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateUwp(object swapChainPanel, float logicalDpi) {
        return new UwpSwapchainSource(swapChainPanel, logicalDpi);
    }

    /// <summary>
    /// Creates the xlib instance used by this backend.
    /// </summary>
    /// <param name="display">The display value used by this operation.</param>
    /// <param name="window">The window value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateXlib(IntPtr display, IntPtr window) {
        return new XlibSwapchainSource(display, window);
    }

    /// <summary>
    /// Creates the wayland instance used by this backend.
    /// </summary>
    /// <param name="display">The display value used by this operation.</param>
    /// <param name="surface">The surface value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateWayland(IntPtr display, IntPtr surface) {
        return new WaylandSwapchainSource(display, surface);
    }

    /// <summary>
    /// Creates the nswindow instance used by this backend.
    /// </summary>
    /// <param name="nsWindow">The ns window value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateNSWindow(IntPtr nsWindow) {
        return new NSWindowSwapchainSource(nsWindow);
    }

    /// <summary>
    /// Creates the uiview instance used by this backend.
    /// </summary>
    /// <param name="uiView">The ui view value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateUIView(IntPtr uiView) {
        return new UIViewSwapchainSource(uiView);
    }

    /// <summary>
    /// Creates the android surface instance used by this backend.
    /// </summary>
    /// <param name="surfaceHandle">The surface handle value used by this operation.</param>
    /// <param name="jniEnv">The jni env value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateAndroidSurface(IntPtr surfaceHandle, IntPtr jniEnv) {
        return new AndroidSurfaceSwapchainSource(surfaceHandle, jniEnv);
    }

    /// <summary>
    /// Creates the nsview instance used by this backend.
    /// </summary>
    /// <param name="nsView">The ns view value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SwapchainSource CreateNSView(IntPtr nsView) {
        return new NSViewSwapchainSource(nsView);
    }
}

/// <summary>
/// Represents the Win32SwapchainSource type used by the graphics runtime.
/// </summary>
internal class Win32SwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32SwapchainSource" /> type.
    /// </summary>
    /// <param name="hwnd">The hwnd value used by this operation.</param>
    /// <param name="hinstance">The hinstance value used by this operation.</param>
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
/// Represents the UwpSwapchainSource type used by the graphics runtime.
/// </summary>
internal class UwpSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UwpSwapchainSource" /> type.
    /// </summary>
    /// <param name="swapChainPanelNative">The swap chain panel native value used by this operation.</param>
    /// <param name="logicalDpi">The logical dpi value used by this operation.</param>
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
/// Represents the XlibSwapchainSource type used by the graphics runtime.
/// </summary>
internal class XlibSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibSwapchainSource" /> type.
    /// </summary>
    /// <param name="display">The display value used by this operation.</param>
    /// <param name="window">The window value used by this operation.</param>
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
/// Represents the WaylandSwapchainSource type used by the graphics runtime.
/// </summary>
internal class WaylandSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="WaylandSwapchainSource" /> type.
    /// </summary>
    /// <param name="display">The display value used by this operation.</param>
    /// <param name="surface">The surface value used by this operation.</param>
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
/// Provides Objective-C interop bindings for NSWindowSwapchainSource.
/// </summary>
internal class NSWindowSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSWindowSwapchainSource" /> type.
    /// </summary>
    /// <param name="nsWindow">The ns window value used by this operation.</param>
    public NSWindowSwapchainSource(IntPtr nsWindow) {
        this.NSWindow = nsWindow;
    }

    /// <summary>
    /// Gets or sets NSWindow.
    /// </summary>
    public IntPtr NSWindow { get; }
}

/// <summary>
/// Represents the UIViewSwapchainSource type used by the graphics runtime.
/// </summary>
internal class UIViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UIViewSwapchainSource" /> type.
    /// </summary>
    /// <param name="uiView">The ui view value used by this operation.</param>
    public UIViewSwapchainSource(IntPtr uiView) {
        this.UIView = uiView;
    }

    /// <summary>
    /// Gets or sets UIView.
    /// </summary>
    public IntPtr UIView { get; }
}

/// <summary>
/// Represents the AndroidSurfaceSwapchainSource type used by the graphics runtime.
/// </summary>
internal class AndroidSurfaceSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidSurfaceSwapchainSource" /> type.
    /// </summary>
    /// <param name="surfaceHandle">The surface handle value used by this operation.</param>
    /// <param name="jniEnv">The jni env value used by this operation.</param>
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
/// Provides Objective-C interop bindings for NSViewSwapchainSource.
/// </summary>
internal class NSViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSViewSwapchainSource" /> type.
    /// </summary>
    /// <param name="nsView">The ns view value used by this operation.</param>
    public NSViewSwapchainSource(IntPtr nsView) {
        this.NSView = nsView;
    }

    /// <summary>
    /// Gets or sets NSView.
    /// </summary>
    public IntPtr NSView { get; }
}