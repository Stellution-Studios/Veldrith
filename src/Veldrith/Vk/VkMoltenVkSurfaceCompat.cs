using System;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides compatibility bindings for MoltenVK surface creation entry points missing from Vortice.
/// </summary>
internal static unsafe class VkMoltenVkSurfaceCompat {

    /// <summary>
    /// Creates a macOS MoltenVK surface.
    /// </summary>
    /// <param name="instance">The Vulkan instance used to create the surface.</param>
    /// <param name="createInfo">The macOS surface creation information.</param>
    /// <param name="allocator">The optional Vulkan allocation callbacks.</param>
    /// <param name="surface">The created surface.</param>
    /// <returns>The Vulkan result produced by the surface creation call.</returns>
    public static VkResult CreateMacOSSurfaceMVK(VkInstance instance, ref VkMacOSSurfaceCreateInfoMVK createInfo, VkAllocationCallbacks* allocator, out VkSurfaceKHR surface) {
        surface = default;
        nint address = (nint)Vulkan.vkGetInstanceProcAddr(instance, "vkCreateMacOSSurfaceMVK").Value;
        if (address == 0) {
            return VkResult.ErrorExtensionNotPresent;
        }

        fixed (VkMacOSSurfaceCreateInfoMVK* createInfoPtr = &createInfo)
        fixed (VkSurfaceKHR* surfacePtr = &surface) {
            return ((delegate* unmanaged<VkInstance, VkMacOSSurfaceCreateInfoMVK*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)address)(instance, createInfoPtr, allocator, surfacePtr);
        }
    }

    /// <summary>
    /// Creates an iOS MoltenVK surface.
    /// </summary>
    /// <param name="instance">The Vulkan instance used to create the surface.</param>
    /// <param name="createInfo">The iOS surface creation information.</param>
    /// <param name="allocator">The optional Vulkan allocation callbacks.</param>
    /// <param name="surface">The created surface.</param>
    /// <returns>The Vulkan result produced by the surface creation call.</returns>
    public static VkResult CreateIOSSurfaceMVK(VkInstance instance, ref VkIOSSurfaceCreateInfoMVK createInfo, VkAllocationCallbacks* allocator, out VkSurfaceKHR surface) {
        surface = default;
        nint address = (nint)Vulkan.vkGetInstanceProcAddr(instance, "vkCreateIOSSurfaceMVK").Value;
        if (address == 0) {
            return VkResult.ErrorExtensionNotPresent;
        }

        fixed (VkIOSSurfaceCreateInfoMVK* createInfoPtr = &createInfo)
        fixed (VkSurfaceKHR* surfacePtr = &surface) {
            return ((delegate* unmanaged<VkInstance, VkIOSSurfaceCreateInfoMVK*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)address)(instance, createInfoPtr, allocator, surfacePtr);
        }
    }
}

/// <summary>
/// Describes a macOS MoltenVK surface.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkMacOSSurfaceCreateInfoMVK {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkMacOSSurfaceCreateInfoMVK" /> type.
    /// </summary>
    public VkMacOSSurfaceCreateInfoMVK() {
        this.SType = (VkStructureType) 1000123000;
        this.PNext = null;
        this.Flags = 0;
        this.PView = null;
    }

    /// <summary>
    /// Stores the Vulkan structure type.
    /// </summary>
    public VkStructureType SType;

    /// <summary>
    /// Stores the optional pointer to extension-specific structure data.
    /// </summary>
    public void* PNext;

    /// <summary>
    /// Stores the reserved surface creation flags.
    /// </summary>
    public uint Flags;

    /// <summary>
    /// Stores the native NSView pointer used to create the surface.
    /// </summary>
    public void* PView;
}

/// <summary>
/// Describes an iOS MoltenVK surface.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkIOSSurfaceCreateInfoMVK {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkIOSSurfaceCreateInfoMVK" /> type.
    /// </summary>
    public VkIOSSurfaceCreateInfoMVK() {
        this.SType = (VkStructureType)1000122000;
        this.PNext = null;
        this.Flags = 0;
        this.PView = null;
    }

    /// <summary>
    /// Stores the Vulkan structure type.
    /// </summary>
    public VkStructureType SType;

    /// <summary>
    /// Stores the optional pointer to extension-specific structure data.
    /// </summary>
    public void* PNext;

    /// <summary>
    /// Stores the reserved surface creation flags.
    /// </summary>
    public uint Flags;

    /// <summary>
    /// Stores the native UIView pointer used to create the surface.
    /// </summary>
    public void* PView;
}
