using System;
using Veldrith.Android;
using Veldrith.MetalBindings;
using Vulkan;
using Vulkan.Android;
using Vulkan.Wayland;
using Vulkan.Xlib;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal static unsafe class VkSurfaceUtil {

    /// <summary>
    /// Executes CreateSurface.
    /// </summary>
    internal static VkSurfaceKHR CreateSurface(VkGraphicsDevice gd, VkInstance instance, SwapchainSource swapchainSource) {
        // TODO a null GD is passed from VkSurfaceSource.CreateSurface for compatibility
        //      when VkSurfaceInfo is removed we do not have to handle gd == null anymore
        bool doCheck = gd != null;

        if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VkKhrSurfaceExtensionName)) {
            throw new VeldridException($"The required instance extension was not available: {CommonStrings.VkKhrSurfaceExtensionName}");
        }

        switch (swapchainSource) {
            case XlibSwapchainSource xlibSource:
                if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VkKhrXlibSurfaceExtensionName)) {
                    throw new VeldridException($"The required instance extension was not available: {CommonStrings.VkKhrXlibSurfaceExtensionName}");
                }

                return CreateXlib(instance, xlibSource);

            case WaylandSwapchainSource waylandSource:
                if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VkKhrWaylandSurfaceExtensionName)) {
                    throw new VeldridException($"The required instance extension was not available: {CommonStrings.VkKhrWaylandSurfaceExtensionName}");
                }

                return CreateWayland(instance, waylandSource);

            case Win32SwapchainSource win32Source:
                if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VkKhrWin32SurfaceExtensionName)) {
                    throw new VeldridException($"The required instance extension was not available: {CommonStrings.VkKhrWin32SurfaceExtensionName}");
                }

                return CreateWin32(instance, win32Source);

            case AndroidSurfaceSwapchainSource androidSource:
                if (doCheck && !gd.HasSurfaceExtension(CommonStrings.VkKhrAndroidSurfaceExtensionName)) {
                    throw new VeldridException($"The required instance extension was not available: {CommonStrings.VkKhrAndroidSurfaceExtensionName}");
                }

                return CreateAndroidSurface(instance, androidSource);

            case NSWindowSwapchainSource nsWindowSource:
                if (doCheck) {
                    bool hasMetalExtension = gd.HasSurfaceExtension(CommonStrings.VkExtMetalSurfaceExtensionName);
                    if (hasMetalExtension || gd.HasSurfaceExtension(CommonStrings.VkMvkMacosSurfaceExtensionName)) {
                        return CreateNSWindowSurface(gd, instance, nsWindowSource, hasMetalExtension);
                    }

                    throw new VeldridException("Neither macOS surface extension was available: " + $"{CommonStrings.VkMvkMacosSurfaceExtensionName}, {CommonStrings.VkExtMetalSurfaceExtensionName}");
                }

                return CreateNSWindowSurface(null, instance, nsWindowSource, false);

            case NSViewSwapchainSource nsViewSource:
                if (doCheck) {
                    bool hasMetalExtension = gd.HasSurfaceExtension(CommonStrings.VkExtMetalSurfaceExtensionName);
                    if (hasMetalExtension || gd.HasSurfaceExtension(CommonStrings.VkMvkMacosSurfaceExtensionName)) {
                        return CreateNSViewSurface(gd, instance, nsViewSource, hasMetalExtension);
                    }

                    throw new VeldridException("Neither macOS surface extension was available: " + $"{CommonStrings.VkMvkMacosSurfaceExtensionName}, {CommonStrings.VkExtMetalSurfaceExtensionName}");
                }

                return CreateNSViewSurface(null, instance, nsViewSource, false);

            case UIViewSwapchainSource uiViewSource:
                if (doCheck) {
                    bool hasMetalExtension = gd.HasSurfaceExtension(CommonStrings.VkExtMetalSurfaceExtensionName);
                    if (hasMetalExtension || gd.HasSurfaceExtension(CommonStrings.VkMvkIOSSurfaceExtensionName)) {
                        return CreateUIViewSurface(gd, instance, uiViewSource, hasMetalExtension);
                    }

                    throw new VeldridException("Neither macOS surface extension was available: " + $"{CommonStrings.VkMvkMacosSurfaceExtensionName}, {CommonStrings.VkMvkIOSSurfaceExtensionName}");
                }

                return CreateUIViewSurface(null, instance, uiViewSource, false);

            default: throw new VeldridException("The provided SwapchainSource cannot be used to create a Vulkan surface.");
        }
    }

    /// <summary>
    /// Executes CreateWin32.
    /// </summary>
    private static VkSurfaceKHR CreateWin32(VkInstance instance, Win32SwapchainSource win32Source) {
        VkWin32SurfaceCreateInfoKHR surfaceCi = VkWin32SurfaceCreateInfoKHR.New();
        surfaceCi.hwnd = win32Source.Hwnd;
        surfaceCi.hinstance = win32Source.Hinstance;
        VkResult result = vkCreateWin32SurfaceKHR(instance, ref surfaceCi, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Executes CreateXlib.
    /// </summary>
    private static VkSurfaceKHR CreateXlib(VkInstance instance, XlibSwapchainSource xlibSource) {
        VkXlibSurfaceCreateInfoKHR xsci = VkXlibSurfaceCreateInfoKHR.New();
        xsci.dpy = (Display*)xlibSource.Display;
        xsci.window = new Window { Value = xlibSource.Window };
        VkResult result = vkCreateXlibSurfaceKHR(instance, ref xsci, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Executes CreateWayland.
    /// </summary>
    private static VkSurfaceKHR CreateWayland(VkInstance instance, WaylandSwapchainSource waylandSource) {
        VkWaylandSurfaceCreateInfoKHR wsci = VkWaylandSurfaceCreateInfoKHR.New();
        wsci.display = (wl_display*)waylandSource.Display;
        wsci.surface = (wl_surface*)waylandSource.Surface;
        VkResult result = vkCreateWaylandSurfaceKHR(instance, ref wsci, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Executes CreateAndroidSurface.
    /// </summary>
    private static VkSurfaceKHR CreateAndroidSurface(VkInstance instance, AndroidSurfaceSwapchainSource androidSource) {
        IntPtr aNativeWindow = AndroidRuntime.ANativeWindow_fromSurface(androidSource.JniEnv, androidSource.Surface);

        VkAndroidSurfaceCreateInfoKHR androidSurfaceCi = VkAndroidSurfaceCreateInfoKHR.New();
        androidSurfaceCi.window = (ANativeWindow*)aNativeWindow;
        VkResult result = vkCreateAndroidSurfaceKHR(instance, ref androidSurfaceCi, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Executes CreateNSWindowSurface.
    /// </summary>
    private static VkSurfaceKHR CreateNSWindowSurface(VkGraphicsDevice gd, VkInstance instance, NSWindowSwapchainSource nsWindowSource, bool hasExtMetalSurface) {
        NSWindow nswindow = new(nsWindowSource.NSWindow);
        return CreateNSViewSurface(gd, instance, new NSViewSwapchainSource(nswindow.contentView), hasExtMetalSurface);
    }

    /// <summary>
    /// Executes CreateNSViewSurface.
    /// </summary>
    private static VkSurfaceKHR CreateNSViewSurface(VkGraphicsDevice gd, VkInstance instance, NSViewSwapchainSource nsViewSource, bool hasExtMetalSurface) {
        NSView contentView = new(nsViewSource.NSView);

        if (!CAMetalLayer.TryCast(contentView.layer, out CAMetalLayer metalLayer)) {
            metalLayer = CAMetalLayer.New();
            contentView.wantsLayer = true;
            contentView.layer = metalLayer.NativePtr;
        }

        if (hasExtMetalSurface) {
            VkMetalSurfaceCreateInfoExt surfaceCi = new() {
                SType = VkMetalSurfaceCreateInfoExt.VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT,
                PLayer = metalLayer.NativePtr.ToPointer()
            };
            VkSurfaceKHR surface;
            VkResult result = gd.CreateMetalSurfaceExt(instance, &surfaceCi, null, &surface);
            CheckResult(result);
            return surface;
        }
        else {
            VkMacOSSurfaceCreateInfoMVK surfaceCi = VkMacOSSurfaceCreateInfoMVK.New();
            surfaceCi.pView = contentView.NativePtr.ToPointer();
            VkResult result = vkCreateMacOSSurfaceMVK(instance, ref surfaceCi, null, out VkSurfaceKHR surface);
            CheckResult(result);
            return surface;
        }
    }

    /// <summary>
    /// Executes CreateUIViewSurface.
    /// </summary>
    private static VkSurfaceKHR CreateUIViewSurface(VkGraphicsDevice gd, VkInstance instance, UIViewSwapchainSource uiViewSource, bool hasExtMetalSurface) {
        UIView uiView = new(uiViewSource.UIView);

        if (!CAMetalLayer.TryCast(uiView.layer, out CAMetalLayer metalLayer)) {
            metalLayer = CAMetalLayer.New();
            metalLayer.frame = uiView.frame;
            metalLayer.opaque = true;
            uiView.layer.addSublayer(metalLayer.NativePtr);
        }

        if (hasExtMetalSurface) {
            VkMetalSurfaceCreateInfoExt surfaceCi = new() {
                SType = VkMetalSurfaceCreateInfoExt.VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT,
                PLayer = metalLayer.NativePtr.ToPointer()
            };
            VkSurfaceKHR surface;
            VkResult result = gd.CreateMetalSurfaceExt(instance, &surfaceCi, null, &surface);
            CheckResult(result);
            return surface;
        }
        else {
            VkIOSSurfaceCreateInfoMVK surfaceCi = VkIOSSurfaceCreateInfoMVK.New();
            surfaceCi.pView = uiView.NativePtr.ToPointer();
            vkCreateIOSSurfaceMVK(instance, ref surfaceCi, null, out VkSurfaceKHR surface);
            return surface;
        }
    }
}