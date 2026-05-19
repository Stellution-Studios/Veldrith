using System;

namespace Veldrith;

/// <summary>
///     A platform-specific object representing a renderable surface.
///     A SwapchainSource can be created with one of several static factory methods.
///     A SwapchainSource is used to describe a Swapchain (see <see cref="SwapchainDescription" />).
/// </summary>
public abstract class SwapchainSource {
    internal SwapchainSource() { }

    /// <summary>
    ///     Creates a new SwapchainSource for a Win32 window.
    /// </summary>
    /// <param name="hwnd">The Win32 window handle.</param>
    /// <param name="hinstance">The Win32 instance handle.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given Win32 window.
    /// </returns>
    public static SwapchainSource CreateWin32(IntPtr hwnd, IntPtr hinstance) {
        return new Win32SwapchainSource(hwnd, hinstance);
    }

    /// <summary>
    ///     Creates a new SwapchainSource for a UWP SwapChain panel.
    /// </summary>
    /// <param name="swapChainPanel">
    ///     A COM object which must implement the <see cref="Vortice.DXGI.ISwapChainPanelNative" />
    ///     or <see cref="Vortice.DXGI.ISwapChainBackgroundPanelNative" /> interface. Generally, this should be a
    ///     SwapChainPanel
    ///     or SwapChainBackgroundPanel contained in your application window.
    /// </param>
    /// <param name="logicalDpi">The logical DPI of the swapchain panel.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given UWP panel.
    /// </returns>
    public static SwapchainSource CreateUwp(object swapChainPanel, float logicalDpi) {
        return new UwpSwapchainSource(swapChainPanel, logicalDpi);
    }

    /// <summary>
    ///     Creates a new SwapchainSource from the given Xlib information.
    /// </summary>
    /// <param name="display">An Xlib Display.</param>
    /// <param name="window">An Xlib Window.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given Xlib window.
    /// </returns>
    public static SwapchainSource CreateXlib(IntPtr display, IntPtr window) {
        return new XlibSwapchainSource(display, window);
    }

    /// <summary>
    ///     Creates a new SwapchainSource from the given Wayland information.
    /// </summary>
    /// <param name="display">The Wayland display proxy.</param>
    /// <param name="surface">The Wayland surface proxy to map.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a <see cref="Swapchain" /> for the given Wayland surface.
    /// </returns>
    public static SwapchainSource CreateWayland(IntPtr display, IntPtr surface) {
        return new WaylandSwapchainSource(display, surface);
    }

    /// <summary>
    ///     Creates a new SwapchainSource for the given NSWindow.
    /// </summary>
    /// <param name="nsWindow">A pointer to an NSWindow.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a Metal <see cref="Swapchain" /> for the given NSWindow.
    /// </returns>
    public static SwapchainSource CreateNSWindow(IntPtr nsWindow) {
        return new NSWindowSwapchainSource(nsWindow);
    }

    /// <summary>
    ///     Creates a new SwapchainSource for the given UIView.
    /// </summary>
    /// <param name="uiView">The UIView's native handle.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a Metal <see cref="Swapchain" /> or an OpenGLES
    ///     <see cref="GraphicsDevice" /> for the given UIView.
    /// </returns>
    public static SwapchainSource CreateUIView(IntPtr uiView) {
        return new UIViewSwapchainSource(uiView);
    }

    /// <summary>
    ///     Creates a new SwapchainSource for the given Android Surface.
    /// </summary>
    /// <param name="surfaceHandle">The handle of the Android Surface.</param>
    /// <param name="jniEnv">The Java Native Interface Environment handle.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a Vulkan <see cref="Swapchain" /> or an OpenGLES
    ///     <see cref="GraphicsDevice" /> for the given Android Surface.
    /// </returns>
    public static SwapchainSource CreateAndroidSurface(IntPtr surfaceHandle, IntPtr jniEnv) {
        return new AndroidSurfaceSwapchainSource(surfaceHandle, jniEnv);
    }

    /// <summary>
    ///     Creates a new SwapchainSource for the given NSView.
    /// </summary>
    /// <param name="nsView">A pointer to an NSView.</param>
    /// <returns>
    ///     A new SwapchainSource which can be used to create a Metal <see cref="Swapchain" /> for the given NSView.
    /// </returns>
    public static SwapchainSource CreateNSView(IntPtr nsView) {
        return new NSViewSwapchainSource(nsView);
    }
}

internal class Win32SwapchainSource : SwapchainSource {
    public Win32SwapchainSource(IntPtr hwnd, IntPtr hinstance) {
        this.Hwnd = hwnd;
        this.Hinstance = hinstance;
    }

    public IntPtr Hwnd { get; }
    public IntPtr Hinstance { get; }
}

internal class UwpSwapchainSource : SwapchainSource {
    public UwpSwapchainSource(object swapChainPanelNative, float logicalDpi) {
        this.SwapChainPanelNative = swapChainPanelNative;
        this.LogicalDpi = logicalDpi;
    }

    public object SwapChainPanelNative { get; }
    public float LogicalDpi { get; }
}

internal class XlibSwapchainSource : SwapchainSource {
    public XlibSwapchainSource(IntPtr display, IntPtr window) {
        this.Display = display;
        this.Window = window;
    }

    public IntPtr Display { get; }
    public IntPtr Window { get; }
}

internal class WaylandSwapchainSource : SwapchainSource {
    public WaylandSwapchainSource(IntPtr display, IntPtr surface) {
        this.Display = display;
        this.Surface = surface;
    }

    public IntPtr Display { get; }
    public IntPtr Surface { get; }
}

internal class NSWindowSwapchainSource : SwapchainSource {
    public NSWindowSwapchainSource(IntPtr nsWindow) {
        this.NSWindow = nsWindow;
    }

    public IntPtr NSWindow { get; }
}

internal class UIViewSwapchainSource : SwapchainSource {
    public UIViewSwapchainSource(IntPtr uiView) {
        this.UIView = uiView;
    }

    public IntPtr UIView { get; }
}

internal class AndroidSurfaceSwapchainSource : SwapchainSource {
    public AndroidSurfaceSwapchainSource(IntPtr surfaceHandle, IntPtr jniEnv) {
        this.Surface = surfaceHandle;
        this.JniEnv = jniEnv;
    }

    public IntPtr Surface { get; }
    public IntPtr JniEnv { get; }
}

internal class NSViewSwapchainSource : SwapchainSource {
    public NSViewSwapchainSource(IntPtr nsView) {
        this.NSView = nsView;
    }

    public IntPtr NSView { get; }
}