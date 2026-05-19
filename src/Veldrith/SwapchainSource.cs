using System;

namespace Veldrith;

public abstract class SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapchainSource" /> class.
    /// </summary>
    internal SwapchainSource() { }

    /// <summary>
    /// Creates a new SwapchainSource for a Win32 window.
    /// </summary>
    /// <param name="hwnd">The Win32 window handle.</param>
    /// <param name="hinstance">The Win32 instance handle.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given Win32 window.
    /// </returns>
    public static SwapchainSource CreateWin32(IntPtr hwnd, IntPtr hinstance) {
        return new Win32SwapchainSource(hwnd, hinstance);
    }

    /// <summary>
    /// Creates a new SwapchainSource for a UWP SwapChain panel.
    /// </summary>
    /// <param name="swapChainPanel">
    /// A COM object which must implement the <see cref="Vortice.DXGI.ISwapChainPanelNative" />
    /// or <see cref="Vortice.DXGI.ISwapChainBackgroundPanelNative" /> interface. Generally, this should be a
    /// SwapChainPanel
    /// or SwapChainBackgroundPanel contained in your application window.
    /// </param>
    /// <param name="logicalDpi">The logical DPI of the swapchain panel.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given UWP panel.
    /// </returns>
    public static SwapchainSource CreateUwp(object swapChainPanel, float logicalDpi) {
        return new UwpSwapchainSource(swapChainPanel, logicalDpi);
    }

    /// <summary>
    /// Creates a new SwapchainSource from the given Xlib information.
    /// </summary>
    /// <param name="display">An Xlib Display.</param>
    /// <param name="window">An Xlib Window.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given Xlib window.
    /// </returns>
    public static SwapchainSource CreateXlib(IntPtr display, IntPtr window) {
        return new XlibSwapchainSource(display, window);
    }

    /// <summary>
    /// Creates a new SwapchainSource from the given Wayland information.
    /// </summary>
    /// <param name="display">The Wayland display proxy.</param>
    /// <param name="surface">The Wayland surface proxy to map.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given Wayland surface.
    /// </returns>
    public static SwapchainSource CreateWayland(IntPtr display, IntPtr surface) {
        return new WaylandSwapchainSource(display, surface);
    }

    /// <summary>
    /// Creates a new SwapchainSource for the given NSWindow.
    /// </summary>
    /// <param name="nsWindow">A pointer to an NSWindow.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a Metal <see cref="Swapchain" /> for the given NSWindow.
    /// </returns>
    public static SwapchainSource CreateNSWindow(IntPtr nsWindow) {
        return new NSWindowSwapchainSource(nsWindow);
    }

    /// <summary>
    /// Creates a new SwapchainSource for the given UIView.
    /// </summary>
    /// <param name="uiView">The UIView's native handle.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a Metal <see cref="Swapchain" /> for the given UIView.
    /// </returns>
    public static SwapchainSource CreateUIView(IntPtr uiView) {
        return new UIViewSwapchainSource(uiView);
    }

    /// <summary>
    /// Creates a new SwapchainSource for the given Android Surface.
    /// </summary>
    /// <param name="surfaceHandle">The handle of the Android Surface.</param>
    /// <param name="jniEnv">The Java Native Interface Environment handle.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a Vulkan <see cref="Swapchain" /> for the given Android Surface.
    /// </returns>
    public static SwapchainSource CreateAndroidSurface(IntPtr surfaceHandle, IntPtr jniEnv) {
        return new AndroidSurfaceSwapchainSource(surfaceHandle, jniEnv);
    }

    /// <summary>
    /// Creates a new SwapchainSource for the given NSView.
    /// </summary>
    /// <param name="nsView">A pointer to an NSView.</param>
    /// <returns>
    /// A new SwapchainSource which can be used to create a Metal <see cref="Swapchain" /> for the given NSView.
    /// </returns>
    public static SwapchainSource CreateNSView(IntPtr nsView) {
        return new NSViewSwapchainSource(nsView);
    }
}

internal class Win32SwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32SwapchainSource" /> class.
    /// </summary>
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

internal class UwpSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UwpSwapchainSource" /> class.
    /// </summary>
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

internal class XlibSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibSwapchainSource" /> class.
    /// </summary>
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

internal class WaylandSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="WaylandSwapchainSource" /> class.
    /// </summary>
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

internal class NSWindowSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSWindowSwapchainSource" /> class.
    /// </summary>
    public NSWindowSwapchainSource(IntPtr nsWindow) {
        this.NSWindow = nsWindow;
    }

    /// <summary>
    /// Gets or sets NSWindow.
    /// </summary>
    public IntPtr NSWindow { get; }
}

internal class UIViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="UIViewSwapchainSource" /> class.
    /// </summary>
    public UIViewSwapchainSource(IntPtr uiView) {
        this.UIView = uiView;
    }

    /// <summary>
    /// Gets or sets UIView.
    /// </summary>
    public IntPtr UIView { get; }
}

internal class AndroidSurfaceSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidSurfaceSwapchainSource" /> class.
    /// </summary>
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

internal class NSViewSwapchainSource : SwapchainSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="NSViewSwapchainSource" /> class.
    /// </summary>
    public NSViewSwapchainSource(IntPtr nsView) {
        this.NSView = nsView;
    }

    /// <summary>
    /// Gets or sets NSView.
    /// </summary>
    public IntPtr NSView { get; }
}
