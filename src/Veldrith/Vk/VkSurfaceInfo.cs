using System;
using Vulkan;
using Vulkan.Xlib;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkSurfaceSource class.
/// </summary>
public abstract class VkSurfaceSource {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSurfaceSource" /> type.
    /// </summary>
    internal VkSurfaceSource() { }

    /// <summary>
    /// Executes the CreateWin32 operation.
    /// </summary>
    /// <param name="hinstance">Specifies the value of <paramref name="hinstance" />.</param>
    /// <param name="hwnd">Specifies the value of <paramref name="hwnd" />.</param>
    /// <returns>Returns the result produced by the CreateWin32 operation.</returns>
    public static VkSurfaceSource CreateWin32(IntPtr hinstance, IntPtr hwnd) {
        return new Win32VkSurfaceInfo(hinstance, hwnd);
    }

    /// <summary>
    /// Executes the CreateXlib operation.
    /// </summary>
    /// <param name="display">Specifies the value of <paramref name="display" />.</param>
    /// <param name="window">Specifies the value of <paramref name="window" />.</param>
    /// <returns>Returns the result produced by the CreateXlib operation.</returns>
    public static unsafe VkSurfaceSource CreateXlib(Display* display, Window window) {
        return new XlibVkSurfaceInfo(display, window);
    }

    /// <summary>
    /// Executes the CreateSurface operation.
    /// </summary>
    /// <param name="instance">Specifies the value of <paramref name="instance" />.</param>
    /// <returns>Returns the result produced by the CreateSurface operation.</returns>
    public abstract VkSurfaceKHR CreateSurface(VkInstance instance);

    /// <summary>
    /// Executes the GetSurfaceSource operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetSurfaceSource operation.</returns>
    internal abstract SwapchainSource GetSurfaceSource();
}

/// <summary>
/// Defines the behavior and responsibilities of the Win32VkSurfaceInfo class.
/// </summary>
internal class Win32VkSurfaceInfo : VkSurfaceSource {

    /// <summary>
    /// Stores the value associated with <c>hinstance</c>.
    /// </summary>
    private readonly IntPtr hinstance;

    /// <summary>
    /// Stores the value associated with <c>hwnd</c>.
    /// </summary>
    private readonly IntPtr hwnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32VkSurfaceInfo" /> type.
    /// </summary>
    /// <param name="hinstance">Specifies the value of <paramref name="hinstance" />.</param>
    /// <param name="hwnd">Specifies the value of <paramref name="hwnd" />.</param>
    public Win32VkSurfaceInfo(IntPtr hinstance, IntPtr hwnd) {
        this.hinstance = hinstance;
        this.hwnd = hwnd;
    }

    /// <summary>
    /// Executes the CreateSurface operation.
    /// </summary>
    /// <param name="instance">Specifies the value of <paramref name="instance" />.</param>
    /// <returns>Returns the result produced by the CreateSurface operation.</returns>
    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    /// <summary>
    /// Executes the GetSurfaceSource operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetSurfaceSource operation.</returns>
    internal override SwapchainSource GetSurfaceSource() {
        return new Win32SwapchainSource(this.hwnd, this.hinstance);
    }
}

/// <summary>
/// Defines the behavior and responsibilities of the XlibVkSurfaceInfo class.
/// </summary>
internal class XlibVkSurfaceInfo : VkSurfaceSource {

    /// <summary>
    /// Stores the value associated with <c>display</c>.
    /// </summary>
    private readonly unsafe Display* display;

    /// <summary>
    /// Stores the value associated with <c>window</c>.
    /// </summary>
    private readonly Window window;

    /// <summary>
    /// Initializes a new instance of the <see cref="XlibVkSurfaceInfo" /> type.
    /// </summary>
    /// <param name="display">Specifies the value of <paramref name="display" />.</param>
    /// <param name="window">Specifies the value of <paramref name="window" />.</param>
    public unsafe XlibVkSurfaceInfo(Display* display, Window window) {
        this.display = display;
        this.window = window;
    }

    /// <summary>
    /// Executes the CreateSurface operation.
    /// </summary>
    /// <param name="instance">Specifies the value of <paramref name="instance" />.</param>
    /// <returns>Returns the result produced by the CreateSurface operation.</returns>
    public override VkSurfaceKHR CreateSurface(VkInstance instance) {
        return VkSurfaceUtil.CreateSurface(null, instance, this.GetSurfaceSource());
    }

    /// <summary>
    /// Executes the GetSurfaceSource operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetSurfaceSource operation.</returns>
    internal override unsafe SwapchainSource GetSurfaceSource() {
        return new XlibSwapchainSource((IntPtr)this.display, this.window.Value);
    }
}