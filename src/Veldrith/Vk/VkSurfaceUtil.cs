using System;
using Veldrith.Android;
using Veldrith.MetalBindings;
using Vortice.Vulkan;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkSurfaceUtil.
/// </summary>
internal static unsafe class VkSurfaceUtil {

    /// <summary>
    /// Creates the surface instance used by this backend.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="swapchainSource">The swapchain source value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Creates the win32 instance used by this backend.
    /// </summary>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="win32Source">The win32 source value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VkSurfaceKHR CreateWin32(VkInstance instance, Win32SwapchainSource win32Source) {
        VkWin32SurfaceCreateInfoKHR surfaceCi = new VkWin32SurfaceCreateInfoKHR();
        surfaceCi.hwnd = win32Source.Hwnd;
        surfaceCi.hinstance = win32Source.Hinstance;
        VkResult result = VulkanDispatch.GetApi(instance).vkCreateWin32SurfaceKHR(ref surfaceCi, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Creates the xlib instance used by this backend.
    /// </summary>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="xlibSource">The xlib source value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VkSurfaceKHR CreateXlib(VkInstance instance, XlibSwapchainSource xlibSource) {
        VkXlibSurfaceCreateInfoKHR xsci = new VkXlibSurfaceCreateInfoKHR();
        xsci.dpy = xlibSource.Display;
        xsci.window = unchecked((ulong)xlibSource.Window.ToInt64());
        VkResult result = VulkanDispatch.GetApi(instance).vkCreateXlibSurfaceKHR(ref xsci, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Creates the wayland instance used by this backend.
    /// </summary>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="waylandSource">The wayland source value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VkSurfaceKHR CreateWayland(VkInstance instance, WaylandSwapchainSource waylandSource) {
        VkWaylandSurfaceCreateInfoKHR wsci = new VkWaylandSurfaceCreateInfoKHR();
        wsci.display = waylandSource.Display;
        wsci.surface = waylandSource.Surface;
        VkResult result = VulkanDispatch.GetApi(instance).vkCreateWaylandSurfaceKHR(ref wsci, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Creates the android surface instance used by this backend.
    /// </summary>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="androidSource">The android source value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VkSurfaceKHR CreateAndroidSurface(VkInstance instance, AndroidSurfaceSwapchainSource androidSource) {
        IntPtr aNativeWindow = AndroidRuntime.ANativeWindow_fromSurface(androidSource.JniEnv, androidSource.Surface);

        VkAndroidSurfaceCreateInfoKHR androidSurfaceCi = new VkAndroidSurfaceCreateInfoKHR();
        androidSurfaceCi.window = aNativeWindow;
        VkResult result = VulkanDispatch.GetApi(instance).vkCreateAndroidSurfaceKHR(ref androidSurfaceCi, null, out VkSurfaceKHR surface);
        CheckResult(result);
        return surface;
    }

    /// <summary>
    /// Creates the nswindow surface instance used by this backend.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="nsWindowSource">The ns window source value used by this operation.</param>
    /// <param name="hasExtMetalSurface">The has ext metal surface value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VkSurfaceKHR CreateNSWindowSurface(VkGraphicsDevice gd, VkInstance instance, NSWindowSwapchainSource nsWindowSource, bool hasExtMetalSurface) {
        NSWindow nswindow = new(nsWindowSource.NSWindow);
        return CreateNSViewSurface(gd, instance, new NSViewSwapchainSource(nswindow.ContentView), hasExtMetalSurface);
    }

    /// <summary>
    /// Creates the nsview surface instance used by this backend.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="nsViewSource">The ns view source value used by this operation.</param>
    /// <param name="hasExtMetalSurface">The has ext metal surface value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VkSurfaceKHR CreateNSViewSurface(VkGraphicsDevice gd, VkInstance instance, NSViewSwapchainSource nsViewSource, bool hasExtMetalSurface) {
        NSView contentView = new(nsViewSource.NSView);

        if (!CAMetalLayer.TryCast(contentView.Layer, out CAMetalLayer metalLayer)) {
            metalLayer = CAMetalLayer.New();
            contentView.WantsLayer = true;
            contentView.Layer = metalLayer.NativePtr;
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
            VkMacOSSurfaceCreateInfoMVK surfaceCi = new VkMacOSSurfaceCreateInfoMVK();
            surfaceCi.PView = contentView.NativePtr.ToPointer();
            VkResult result = VkMoltenVkSurfaceCompat.CreateMacOSSurfaceMVK(instance, ref surfaceCi, null, out VkSurfaceKHR surface);
            CheckResult(result);
            return surface;
        }
    }

    /// <summary>
    /// Creates the uiview surface instance used by this backend.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="instance">The instance value used by this operation.</param>
    /// <param name="uiViewSource">The ui view source value used by this operation.</param>
    /// <param name="hasExtMetalSurface">The has ext metal surface value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VkSurfaceKHR CreateUIViewSurface(VkGraphicsDevice gd, VkInstance instance, UIViewSwapchainSource uiViewSource, bool hasExtMetalSurface) {
        UIView uiView = new(uiViewSource.UIView);

        if (!CAMetalLayer.TryCast(uiView.Layer, out CAMetalLayer metalLayer)) {
            metalLayer = CAMetalLayer.New();
            metalLayer.frame = uiView.Frame;
            metalLayer.opaque = true;
            uiView.Layer.AddSublayer(metalLayer.NativePtr);
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
            VkIOSSurfaceCreateInfoMVK surfaceCi = new VkIOSSurfaceCreateInfoMVK();
            surfaceCi.PView = uiView.NativePtr.ToPointer();
            VkMoltenVkSurfaceCompat.CreateIOSSurfaceMVK(instance, ref surfaceCi, null, out VkSurfaceKHR surface);
            return surface;
        }
    }
}
