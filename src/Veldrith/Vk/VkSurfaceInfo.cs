using System;
using Vulkan;
using Vulkan.Xlib;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkSurfaceSource.
/// </summary>
public abstract class VkSurfaceSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSurfaceSource" /> type.
    /// </summary>
    internal VkSurfaceSource() { }

    /// <summary>
    /// Creates the win32 instance used by this backend.
    /// </summary>
    /// <param name="hinstance">The hinstance value used by this operation.</param>
    /// <param name="hwnd">The hwnd value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static VkSurfaceSource CreateWin32(IntPtr hinstance, IntPtr hwnd) {
        return new Win32VkSurfaceInfo(hinstance, hwnd);
    }

    /// <summary>
    /// Creates the xlib instance used by this backend.
    /// </summary>
    /// <param name="display">The display value used by this operation.</param>
    /// <param name="window">The window value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static unsafe VkSurfaceSource CreateXlib(Display* display, Window window) {
        return new XlibVkSurfaceInfo(display, window);
    }

    /// <summary>
    /// Creates the surface instance used by this backend.
    /// </summary>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract VkSurfaceKHR CreateSurface(VkInstance instance);

    /// <summary>
    /// Gets the surface source value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal abstract SwapchainSource GetSurfaceSource();
}

/// <summary>
/// Represents the Win32VkSurfaceInfo type used by the graphics runtime.
/// </summary>
internal class Win32VkSurfaceInfo : VkSurfaceSource {

    /// <summary>
    /// Stores the hinstance state used by this instance.
    /// </summary>
    private readonly IntPtr hinstance;

    /// <summary>
    /// Stores the hwnd state used by this instance.
    /// </summary>
    private readonly IntPtr hwnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32VkSurfaceInfo" /> type.
    /// </summary>
    /// <param name="hinstance">The hinstance value used by this operation.</param>
    /// <param name="hwnd">The hwnd value used by this operation.</param>
    public Win32VkSurfaceInfo(IntPtr hinstance, IntPtr hwnd) {
        this.hinstance = hinstance;
        this.hwnd = hwnd;
    }

    /// <summary>
    /// Creates the surface instance used by this backend.
    /// </summary>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    /// <summary>
    /// Gets the surface source value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override SwapchainSource GetSurfaceSource() {
        return new Win32SwapchainSource(this.hwnd, this.hinstance);
    }
}

/// <summary>
/// Represents the XlibVkSurfaceInfo type used by the graphics runtime.
/// </summary>
internal class XlibVkSurfaceInfo : VkSurfaceSource {

    /// <summary>
    /// Stores the display state used by this instance.
    /// </summary>
    private readonly unsafe Display* display;

    /// <summary>
    /// Stores the window state used by this instance.
    /// </summary>
    private readonly Window window;

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibVkSurfaceInfo" /> type.
    /// </summary>
    /// <param name="display">The display value used by this operation.</param>
    /// <param name="window">The window value used by this operation.</param>
    public unsafe XlibVkSurfaceInfo(Display* display, Window window) {
        this.display = display;
        this.window = window;
    }

    /// <summary>
    /// Creates the surface instance used by this backend.
    /// </summary>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    /// <summary>
    /// Gets the surface source value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override unsafe SwapchainSource GetSurfaceSource() {
        return new XlibSwapchainSource((IntPtr)this.display, this.window.Value);
    }
}