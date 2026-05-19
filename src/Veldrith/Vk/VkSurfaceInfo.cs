using System;
using Vulkan;
using Vulkan.Xlib;

namespace Veldrith.Vk;

/// <summary>
///     An object which can be used to create a VkSurfaceKHR.
/// </summary>
public abstract class VkSurfaceSource {
    internal VkSurfaceSource() { }

    /// <summary>
    ///     Creates a new <see cref="VkSurfaceSource" /> from the given Win32 instance and window handle.
    /// </summary>
    /// <param name="hinstance">The Win32 instance handle.</param>
    /// <param name="hwnd">The Win32 window handle.</param>
    /// <returns>A new VkSurfaceSource.</returns>
    public static VkSurfaceSource CreateWin32(IntPtr hinstance, IntPtr hwnd) {
        return new Win32VkSurfaceInfo(hinstance, hwnd);
    }

    /// <summary>
    ///     Creates a new VkSurfaceSource from the given Xlib information.
    /// </summary>
    /// <param name="display">A pointer to the Xlib Display.</param>
    /// <param name="window">An Xlib window.</param>
    /// <returns>A new VkSurfaceSource.</returns>
    public static unsafe VkSurfaceSource CreateXlib(Display* display, Window window) {
        return new XlibVkSurfaceInfo(display, window);
    }

    /// <summary>
    ///     Creates a new VkSurfaceKHR attached to this source.
    /// </summary>
    /// <param name="instance">The VkInstance to use.</param>
    /// <returns>A new VkSurfaceKHR.</returns>
    public abstract VkSurfaceKHR CreateSurface(VkInstance instance);

    internal abstract SwapchainSource GetSurfaceSource();
}

internal class Win32VkSurfaceInfo : VkSurfaceSource {
    private readonly IntPtr hinstance;
    private readonly IntPtr hwnd;

    public Win32VkSurfaceInfo(IntPtr hinstance, IntPtr hwnd) {
        this.hinstance = hinstance;
        this.hwnd = hwnd;
    }

    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    internal override SwapchainSource GetSurfaceSource() {
        return new Win32SwapchainSource(this.hwnd, this.hinstance);
    }
}

internal class XlibVkSurfaceInfo : VkSurfaceSource {
    private readonly unsafe Display* display;
    private readonly Window window;

    public unsafe XlibVkSurfaceInfo(Display* display, Window window) {
        this.display = display;
        this.window = window;
    }

    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    internal override unsafe SwapchainSource GetSurfaceSource() {
        return new XlibSwapchainSource((IntPtr)this.display, this.window.Value);
    }
}