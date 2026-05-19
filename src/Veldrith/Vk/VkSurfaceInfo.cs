using System;
using Vulkan;
using Vulkan.Xlib;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VkSurfaceSource class.
/// </summary>
public abstract class VkSurfaceSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSurfaceSource" /> class.
    /// </summary>
    internal VkSurfaceSource() { }

    /// <summary>
    /// Creates a new <see cref="VkSurfaceSource" /> from the given Win32 instance and window handle.
    /// </summary>
    /// <param name="hinstance">The Win32 instance handle.</param>
    /// <param name="hwnd">The Win32 window handle.</param>
    /// <returns>A new VkSurfaceSource.</returns>
    public static VkSurfaceSource CreateWin32(IntPtr hinstance, IntPtr hwnd) {
        return new Win32VkSurfaceInfo(hinstance, hwnd);
    }

    /// <summary>
    /// Creates a new VkSurfaceSource from the given Xlib information.
    /// </summary>
    /// <param name="display">A pointer to the Xlib Display.</param>
    /// <param name="window">An Xlib window.</param>
    /// <returns>A new VkSurfaceSource.</returns>
    public static unsafe VkSurfaceSource CreateXlib(Display* display, Window window) {
        return new XlibVkSurfaceInfo(display, window);
    }

    /// <summary>
    /// Creates a new VkSurfaceKHR attached to this source.
    /// </summary>
    /// <param name="instance">The VkInstance to use.</param>
    /// <returns>A new VkSurfaceKHR.</returns>
    public abstract VkSurfaceKHR CreateSurface(VkInstance instance);

    /// <summary>
    /// Executes GetSurfaceSource.
    /// </summary>
    internal abstract SwapchainSource GetSurfaceSource();
}

/// <summary>
/// Represents the Win32VkSurfaceInfo class.
/// </summary>
internal class Win32VkSurfaceInfo : VkSurfaceSource {

    /// <summary>
    /// Represents the hinstance field.
    /// </summary>
    private readonly IntPtr hinstance;

    /// <summary>
    /// Represents the hwnd field.
    /// </summary>
    private readonly IntPtr hwnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32VkSurfaceInfo" /> class.
    /// </summary>
    public Win32VkSurfaceInfo(IntPtr hinstance, IntPtr hwnd) {
        this.hinstance = hinstance;
        this.hwnd = hwnd;
    }

    /// <summary>
    /// Executes CreateSurface.
    /// </summary>
    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    /// <summary>
    /// Executes GetSurfaceSource.
    /// </summary>
    internal override SwapchainSource GetSurfaceSource() {
        return new Win32SwapchainSource(this.hwnd, this.hinstance);
    }
}

/// <summary>
/// Represents the XlibVkSurfaceInfo class.
/// </summary>
internal class XlibVkSurfaceInfo : VkSurfaceSource {

    /// <summary>
    /// Represents the display field.
    /// </summary>
    private readonly unsafe Display* display;

    /// <summary>
    /// Represents the window field.
    /// </summary>
    private readonly Window window;

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibVkSurfaceInfo" /> class.
    /// </summary>
    public unsafe XlibVkSurfaceInfo(Display* display, Window window) {
        this.display = display;
        this.window = window;
    }

    /// <summary>
    /// Executes CreateSurface.
    /// </summary>
    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    /// <summary>
    /// Executes GetSurfaceSource.
    /// </summary>
    internal override unsafe SwapchainSource GetSurfaceSource() {
        return new XlibSwapchainSource((IntPtr)this.display, this.window.Value);
    }
}