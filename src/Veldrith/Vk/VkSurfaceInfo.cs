using System;
using Vortice.Vulkan;

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
    public static VkSurfaceSource CreateXlib(IntPtr display, IntPtr window) {
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
    /// Stores the native HINSTANCE used by this instance.
    /// </summary>
    private readonly IntPtr _hinstance;

    /// <summary>
    /// Stores the native HWND used by this instance.
    /// </summary>
    private readonly IntPtr _hwnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32VkSurfaceInfo" /> type.
    /// </summary>
    /// <param name="hinstance">The hinstance value used by this operation.</param>
    /// <param name="hwnd">The hwnd value used by this operation.</param>
    public Win32VkSurfaceInfo(IntPtr hinstance, IntPtr hwnd) {
        this._hinstance = hinstance;
        this._hwnd = hwnd;
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
        return new Win32SwapchainSource(this._hwnd, this._hinstance);
    }
}

/// <summary>
/// Represents the XlibVkSurfaceInfo type used by the graphics runtime.
/// </summary>
internal class XlibVkSurfaceInfo : VkSurfaceSource {

    /// <summary>
    /// Stores the native display handle used by this instance.
    /// </summary>
    private readonly IntPtr _display;

    /// <summary>
    /// Stores the native window handle used by this instance.
    /// </summary>
    private readonly IntPtr _window;

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibVkSurfaceInfo" /> type.
    /// </summary>
    /// <param name="display">The display value used by this operation.</param>
    /// <param name="window">The window value used by this operation.</param>
    public XlibVkSurfaceInfo(IntPtr display, IntPtr window) {
        this._display = display;
        this._window = window;
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
        return new XlibSwapchainSource(this._display, this._window);
    }
}
