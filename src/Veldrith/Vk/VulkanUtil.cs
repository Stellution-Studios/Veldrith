using System;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VulkanUtil class.
/// </summary>
internal static unsafe class VulkanUtil {

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="TryLoadVulkan">The value of TryLoadVulkan.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly Lazy<bool> _s_is_vulkan_loaded = new(TryLoadVulkan);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="EnumerateInstanceExtensions">The value of EnumerateInstanceExtensions.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly Lazy<string[]> _s_instance_extensions = new(EnumerateInstanceExtensions);

    [Conditional("DEBUG")]

    /// <summary>
    /// Performs the CheckResult operation.
    /// </summary>
    /// <param name="result">The value of result.</param>
    public static void CheckResult(VkResult result) {
        if (result != VkResult.Success) {
            throw new VeldridException("Unsuccessful VkResult: " + result);
        }
    }

    /// <summary>
    /// Performs the TryFindMemoryType operation.
    /// </summary>
    /// <param name="memProperties">The value of memProperties.</param>
    /// <param name="typeFilter">The value of typeFilter.</param>
    /// <param name="properties">The value of properties.</param>
    /// <param name="typeIndex">The value of typeIndex.</param>
    /// <returns>The result of the TryFindMemoryType operation.</returns>
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
    /// Performs the EnumerateInstanceLayers operation.
    /// </summary>
    /// <returns>The result of the EnumerateInstanceLayers operation.</returns>
    public static string[] EnumerateInstanceLayers() {
        uint propCount = 0;
        VkResult result = vkEnumerateInstanceLayerProperties(ref propCount, null);
        CheckResult(result);
        if (propCount == 0) {
            return Array.Empty<string>();
        }

        VkLayerProperties[] props = new VkLayerProperties[propCount];
        vkEnumerateInstanceLayerProperties(ref propCount, ref props[0]);

        string[] ret = new string[propCount];

        for (int i = 0; i < propCount; i++) {
            fixed (byte* layerNamePtr = props[i].layerName) {
                ret[i] = Util.GetString(layerNamePtr);
            }
        }

        return ret;
    }

    /// <summary>
    /// Performs the GetInstanceExtensions operation.
    /// </summary>
    /// <returns>The result of the GetInstanceExtensions operation.</returns>
    public static string[] GetInstanceExtensions() {
        return _s_instance_extensions.Value;
    }

    /// <summary>
    /// Performs the IsVulkanLoaded operation.
    /// </summary>
    /// <returns>The result of the IsVulkanLoaded operation.</returns>
    public static bool IsVulkanLoaded() {
        return _s_is_vulkan_loaded.Value;
    }

    /// <summary>
    /// Performs the TransitionImageLayout operation.
    /// </summary>
    /// <param name="cb">The value of cb.</param>
    /// <param name="image">The value of image.</param>
    /// <param name="baseMipLevel">The value of baseMipLevel.</param>
    /// <param name="levelCount">The value of levelCount.</param>
    /// <param name="baseArrayLayer">The value of baseArrayLayer.</param>
    /// <param name="layerCount">The value of layerCount.</param>
    /// <param name="aspectMask">The value of aspectMask.</param>
    /// <param name="oldLayout">The value of oldLayout.</param>
    /// <param name="newLayout">The value of newLayout.</param>
    public static void TransitionImageLayout(VkCommandBuffer cb, VkImage image, uint baseMipLevel, uint levelCount, uint baseArrayLayer, uint layerCount, VkImageAspectFlags aspectMask, VkImageLayout oldLayout, VkImageLayout newLayout) {
        Debug.Assert(oldLayout != newLayout);
        VkImageMemoryBarrier barrier = VkImageMemoryBarrier.New();
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

        vkCmdPipelineBarrier(cb, srcStageFlags, dstStageFlags, VkDependencyFlags.None, 0, null, 0, null, 1, &barrier);
    }

    /// <summary>
    /// Performs the EnumerateInstanceExtensions operation.
    /// </summary>
    /// <returns>The result of the EnumerateInstanceExtensions operation.</returns>
    private static string[] EnumerateInstanceExtensions() {
        if (!IsVulkanLoaded()) {
            return Array.Empty<string>();
        }

        uint propCount = 0;
        VkResult result = vkEnumerateInstanceExtensionProperties((byte*)null, ref propCount, null);
        if (result != VkResult.Success) {
            return Array.Empty<string>();
        }

        if (propCount == 0) {
            return Array.Empty<string>();
        }

        VkExtensionProperties[] props = new VkExtensionProperties[propCount];
        vkEnumerateInstanceExtensionProperties((byte*)null, ref propCount, ref props[0]);

        string[] ret = new string[propCount];

        for (int i = 0; i < propCount; i++) {
            fixed (byte* extensionNamePtr = props[i].extensionName) {
                ret[i] = Util.GetString(extensionNamePtr);
            }
        }

        return ret;
    }

    /// <summary>
    /// Performs the TryLoadVulkan operation.
    /// </summary>
    /// <returns>The result of the TryLoadVulkan operation.</returns>
    private static bool TryLoadVulkan() {
        try {
            uint propCount;
            vkEnumerateInstanceExtensionProperties((byte*)null, &propCount, null);
            return true;
        }
        catch {
            return false;
        }
    }
}

/// <summary>
/// Represents the VkPhysicalDeviceMemoryPropertiesEx class.
/// </summary>
internal static unsafe class VkPhysicalDeviceMemoryPropertiesEx {

    /// <summary>
    /// Performs the GetMemoryType operation.
    /// </summary>
    /// <param name="memoryProperties">The value of memoryProperties.</param>
    /// <param name="index">The value of index.</param>
    /// <returns>The result of the GetMemoryType operation.</returns>
    public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index) {
        return (&memoryProperties.memoryTypes_0)[index];
    }
}