using System;
using System.Diagnostics;
using Vortice.Vulkan;
using static Veldrith.Vk.VulkanDispatch;
using static Vortice.Vulkan.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VulkanUtil type used by the graphics runtime.
/// </summary>
internal static unsafe class VulkanUtil {

    /// <summary>
    /// Stores the s is vulkan loaded state used by this instance.
    /// </summary>

    private static readonly Lazy<bool> _s_is_vulkan_loaded = new(TryLoadVulkan);

    /// <summary>
    /// Stores the s instance extensions state used by this instance.
    /// </summary>

    private static readonly Lazy<string[]> _s_instance_extensions = new(EnumerateInstanceExtensions);

    [Conditional("DEBUG")]

    /// <summary>
    /// Executes the check result logic for this backend.
    /// </summary>
    /// <param name="result">The result value used by this operation.</param>
    public static void CheckResult(VkResult result) {
        if (result != VkResult.Success) {
            throw new VeldridException("Unsuccessful VkResult: " + result);
        }
    }

    /// <summary>
    /// Attempts to find memory type and reports whether it succeeded.
    /// </summary>
    /// <param name="memProperties">The mem properties value used by this operation.</param>
    /// <param name="typeFilter">The type filter value used by this operation.</param>
    /// <param name="properties">The properties value used by this operation.</param>
    /// <param name="typeIndex">The type index value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public static bool TryFindMemoryType(VkPhysicalDeviceMemoryProperties memProperties, uint typeFilter, VkMemoryPropertyFlags properties, out uint typeIndex) {
        typeIndex = 0;

        for (int i = 0; i < memProperties.memoryTypeCount; i++) {
            if ((typeFilter & (1 << i)) != 0
                && (memProperties.GetMemoryType((uint)i).propertyFlags & properties) == properties) {
                typeIndex = (uint)i;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Executes the enumerate instance layers logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static string[] EnumerateInstanceLayers() {
        uint propCount = 0;
        VkResult result = vkEnumerateInstanceLayerProperties(&propCount, null);
        CheckResult(result);
        if (propCount == 0) {
            return Array.Empty<string>();
        }

        VkLayerProperties[] props = new VkLayerProperties[propCount];
        fixed (VkLayerProperties* propsPtr = props) {
            vkEnumerateInstanceLayerProperties(&propCount, propsPtr);
        }

        string[] ret = new string[propCount];

        for (int i = 0; i < propCount; i++) {
            fixed (byte* layerNamePtr = props[i].layerName) {
                ret[i] = Util.GetString(layerNamePtr);
            }
        }

        return ret;
    }

    /// <summary>
    /// Gets the instance extensions value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static string[] GetInstanceExtensions() {
        return _s_instance_extensions.Value;
    }

    /// <summary>
    /// Executes the is vulkan loaded logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public static bool IsVulkanLoaded() {
        return _s_is_vulkan_loaded.Value;
    }

    /// <summary>
    /// Executes the transition image layout logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    /// <param name="image">The image value used by this operation.</param>
    /// <param name="baseMipLevel">The base mip level value used by this operation.</param>
    /// <param name="levelCount">The level count value used by this operation.</param>
    /// <param name="baseArrayLayer">The base array layer value used by this operation.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
    /// <param name="aspectMask">The aspect mask value used by this operation.</param>
    /// <param name="oldLayout">The old layout value used by this operation.</param>
    /// <param name="newLayout">The new layout value used by this operation.</param>
    public static void TransitionImageLayout(VkCommandBuffer cb, VkImage image, uint baseMipLevel, uint levelCount, uint baseArrayLayer, uint layerCount, VkImageAspectFlags aspectMask, VkImageLayout oldLayout, VkImageLayout newLayout) {
        Debug.Assert(oldLayout != newLayout);
        VkImageMemoryBarrier barrier = new VkImageMemoryBarrier();
        barrier.oldLayout = oldLayout;
        barrier.newLayout = newLayout;
        barrier.srcQueueFamilyIndex = QueueFamilyIgnored;
        barrier.dstQueueFamilyIndex = QueueFamilyIgnored;
        barrier.image = image;
        barrier.subresourceRange.aspectMask = aspectMask;
        barrier.subresourceRange.baseMipLevel = baseMipLevel;
        barrier.subresourceRange.levelCount = levelCount;
        barrier.subresourceRange.baseArrayLayer = baseArrayLayer;
        barrier.subresourceRange.layerCount = layerCount;

        VkPipelineStageFlags srcStageFlags = VkPipelineStageFlags.None;
        VkPipelineStageFlags dstStageFlags = VkPipelineStageFlags.None;

        if ((oldLayout == VkImageLayout.Undefined || oldLayout == VkImageLayout.Preinitialized) && newLayout == VkImageLayout.TransferDstOptimal) {
            barrier.srcAccessMask = VkAccessFlags.None;
            barrier.dstAccessMask = VkAccessFlags.TransferWrite;
            srcStageFlags = VkPipelineStageFlags.TopOfPipe;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.ShaderReadOnlyOptimal && newLayout == VkImageLayout.TransferSrcOptimal) {
            barrier.srcAccessMask = VkAccessFlags.ShaderRead;
            barrier.dstAccessMask = VkAccessFlags.TransferRead;
            srcStageFlags = VkPipelineStageFlags.FragmentShader;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.ShaderReadOnlyOptimal && newLayout == VkImageLayout.TransferDstOptimal) {
            barrier.srcAccessMask = VkAccessFlags.ShaderRead;
            barrier.dstAccessMask = VkAccessFlags.TransferWrite;
            srcStageFlags = VkPipelineStageFlags.FragmentShader;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.Preinitialized && newLayout == VkImageLayout.TransferSrcOptimal) {
            barrier.srcAccessMask = VkAccessFlags.None;
            barrier.dstAccessMask = VkAccessFlags.TransferRead;
            srcStageFlags = VkPipelineStageFlags.TopOfPipe;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.Preinitialized && newLayout == VkImageLayout.General) {
            barrier.srcAccessMask = VkAccessFlags.None;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.TopOfPipe;
            dstStageFlags = VkPipelineStageFlags.ComputeShader;
        }
        else if (oldLayout == VkImageLayout.Preinitialized && newLayout == VkImageLayout.ShaderReadOnlyOptimal) {
            barrier.srcAccessMask = VkAccessFlags.None;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.TopOfPipe;
            dstStageFlags = VkPipelineStageFlags.FragmentShader;
        }
        else if (oldLayout == VkImageLayout.General && newLayout == VkImageLayout.ShaderReadOnlyOptimal) {
            barrier.srcAccessMask = VkAccessFlags.TransferRead;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.FragmentShader;
        }
        else if (oldLayout == VkImageLayout.ShaderReadOnlyOptimal && newLayout == VkImageLayout.General) {
            barrier.srcAccessMask = VkAccessFlags.ShaderRead;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.FragmentShader;
            dstStageFlags = VkPipelineStageFlags.ComputeShader;
        }

        else if (oldLayout == VkImageLayout.TransferSrcOptimal && newLayout == VkImageLayout.ShaderReadOnlyOptimal) {
            barrier.srcAccessMask = VkAccessFlags.TransferRead;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.FragmentShader;
        }
        else if (oldLayout == VkImageLayout.TransferDstOptimal && newLayout == VkImageLayout.ShaderReadOnlyOptimal) {
            barrier.srcAccessMask = VkAccessFlags.TransferWrite;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.FragmentShader;
        }
        else if (oldLayout == VkImageLayout.TransferSrcOptimal && newLayout == VkImageLayout.TransferDstOptimal) {
            barrier.srcAccessMask = VkAccessFlags.TransferRead;
            barrier.dstAccessMask = VkAccessFlags.TransferWrite;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.TransferDstOptimal && newLayout == VkImageLayout.TransferSrcOptimal) {
            barrier.srcAccessMask = VkAccessFlags.TransferWrite;
            barrier.dstAccessMask = VkAccessFlags.TransferRead;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.ColorAttachmentOptimal && newLayout == VkImageLayout.TransferSrcOptimal) {
            barrier.srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
            barrier.dstAccessMask = VkAccessFlags.TransferRead;
            srcStageFlags = VkPipelineStageFlags.ColorAttachmentOutput;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.ColorAttachmentOptimal && newLayout == VkImageLayout.TransferDstOptimal) {
            barrier.srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
            barrier.dstAccessMask = VkAccessFlags.TransferWrite;
            srcStageFlags = VkPipelineStageFlags.ColorAttachmentOutput;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.ColorAttachmentOptimal && newLayout == VkImageLayout.ShaderReadOnlyOptimal) {
            barrier.srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.ColorAttachmentOutput;
            dstStageFlags = VkPipelineStageFlags.FragmentShader;
        }
        else if (oldLayout == VkImageLayout.DepthStencilAttachmentOptimal && newLayout == VkImageLayout.ShaderReadOnlyOptimal) {
            barrier.srcAccessMask = VkAccessFlags.DepthStencilAttachmentWrite;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            srcStageFlags = VkPipelineStageFlags.LateFragmentTests;
            dstStageFlags = VkPipelineStageFlags.FragmentShader;
        }
        else if (oldLayout == VkImageLayout.ColorAttachmentOptimal && newLayout == VkImageLayout.PresentSrcKHR) {
            barrier.srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
            barrier.dstAccessMask = VkAccessFlags.MemoryRead;
            srcStageFlags = VkPipelineStageFlags.ColorAttachmentOutput;
            dstStageFlags = VkPipelineStageFlags.BottomOfPipe;
        }
        else if (oldLayout == VkImageLayout.TransferDstOptimal && newLayout == VkImageLayout.PresentSrcKHR) {
            barrier.srcAccessMask = VkAccessFlags.TransferWrite;
            barrier.dstAccessMask = VkAccessFlags.MemoryRead;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.BottomOfPipe;
        }
        else if (oldLayout == VkImageLayout.TransferDstOptimal && newLayout == VkImageLayout.ColorAttachmentOptimal) {
            barrier.srcAccessMask = VkAccessFlags.TransferWrite;
            barrier.dstAccessMask = VkAccessFlags.ColorAttachmentWrite;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.ColorAttachmentOutput;
        }
        else if (oldLayout == VkImageLayout.TransferDstOptimal && newLayout == VkImageLayout.DepthStencilAttachmentOptimal) {
            barrier.srcAccessMask = VkAccessFlags.TransferWrite;
            barrier.dstAccessMask = VkAccessFlags.DepthStencilAttachmentWrite;
            srcStageFlags = VkPipelineStageFlags.Transfer;
            dstStageFlags = VkPipelineStageFlags.LateFragmentTests;
        }
        else if (oldLayout == VkImageLayout.General && newLayout == VkImageLayout.TransferSrcOptimal) {
            barrier.srcAccessMask = VkAccessFlags.ShaderWrite;
            barrier.dstAccessMask = VkAccessFlags.TransferRead;
            srcStageFlags = VkPipelineStageFlags.ComputeShader;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.General && newLayout == VkImageLayout.TransferDstOptimal) {
            barrier.srcAccessMask = VkAccessFlags.ShaderWrite;
            barrier.dstAccessMask = VkAccessFlags.TransferWrite;
            srcStageFlags = VkPipelineStageFlags.ComputeShader;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.PresentSrcKHR && newLayout == VkImageLayout.TransferSrcOptimal) {
            barrier.srcAccessMask = VkAccessFlags.MemoryRead;
            barrier.dstAccessMask = VkAccessFlags.TransferRead;
            srcStageFlags = VkPipelineStageFlags.BottomOfPipe;
            dstStageFlags = VkPipelineStageFlags.Transfer;
        }
        else {
            Debug.Fail("Invalid image layout transition.");
        }

        VulkanDispatch.GetApi(cb).vkCmdPipelineBarrier(cb, srcStageFlags, dstStageFlags, VkDependencyFlags.None, 0, null, 0, null, 1, &barrier);
    }

    /// <summary>
    /// Executes the enumerate instance extensions logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private static string[] EnumerateInstanceExtensions() {
        if (!IsVulkanLoaded()) {
            return Array.Empty<string>();
        }

        uint propCount = 0;
        VkResult result = vkEnumerateInstanceExtensionProperties(&propCount, null);
        if (result != VkResult.Success) {
            return Array.Empty<string>();
        }

        if (propCount == 0) {
            return Array.Empty<string>();
        }

        VkExtensionProperties[] props = new VkExtensionProperties[propCount];
        fixed (VkExtensionProperties* propsPtr = props) {
            vkEnumerateInstanceExtensionProperties(&propCount, propsPtr);
        }

        string[] ret = new string[propCount];

        for (int i = 0; i < propCount; i++) {
            fixed (byte* extensionNamePtr = props[i].extensionName) {
                ret[i] = Util.GetString(extensionNamePtr);
            }
        }

        return ret;
    }

    /// <summary>
    /// Attempts to load vulkan and reports whether it succeeded.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool TryLoadVulkan() {
        try {
            if (vkInitialize(null) != VkResult.Success) {
                return false;
            }

            uint propCount;
            vkEnumerateInstanceExtensionProperties(&propCount, null);
            return true;
        }
        catch {
            return false;
        }
    }
}

/// <summary>
/// Provides the Vulkan backend implementation for VkPhysicalDeviceMemoryPropertiesEx.
/// </summary>
internal static unsafe class VkPhysicalDeviceMemoryPropertiesEx {

    /// <summary>
    /// Gets the memory type value.
    /// </summary>
    /// <param name="memoryProperties">The memory properties value used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <returns>The value produced by this operation.</returns>
    public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index) {
        return (&memoryProperties.memoryTypes.e0)[index];
    }
}
