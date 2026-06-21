using System.Collections.Generic;
using Vortice.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Tracks Vortice Vulkan dispatch instances for handles created by the Vulkan backend.
/// </summary>
internal static class VulkanDispatch {

    /// <summary>
    /// Identifies that a queue family index should be ignored.
    /// </summary>
    public const uint QueueFamilyIgnored = uint.MaxValue;

    /// <summary>
    /// Defines the maximum number of bytes in a Vulkan physical device name.
    /// </summary>
    public const uint MaxPhysicalDeviceNameSize = 256;

    /// <summary>
    /// Identifies the implicit external subpass dependency.
    /// </summary>
    public const uint SubpassExternal = uint.MaxValue;

    /// <summary>
    /// Represents indirect command read access.
    /// </summary>
    public const VkAccessFlags AccessIndirectCommandReadBit = VkAccessFlags.IndirectCommandRead;

    /// <summary>
    /// Represents index buffer read access.
    /// </summary>
    public const VkAccessFlags AccessIndexReadBit = VkAccessFlags.IndexRead;

    /// <summary>
    /// Represents vertex attribute read access.
    /// </summary>
    public const VkAccessFlags AccessVertexAttributeReadBit = VkAccessFlags.VertexAttributeRead;

    /// <summary>
    /// Represents uniform buffer read access.
    /// </summary>
    public const VkAccessFlags AccessUniformReadBit = VkAccessFlags.UniformRead;

    /// <summary>
    /// Represents input attachment read access.
    /// </summary>
    public const VkAccessFlags AccessInputAttachmentReadBit = VkAccessFlags.InputAttachmentRead;

    /// <summary>
    /// Represents shader read access.
    /// </summary>
    public const VkAccessFlags AccessShaderReadBit = VkAccessFlags.ShaderRead;

    /// <summary>
    /// Represents shader write access.
    /// </summary>
    public const VkAccessFlags AccessShaderWriteBit = VkAccessFlags.ShaderWrite;

    /// <summary>
    /// Represents color attachment read access.
    /// </summary>
    public const VkAccessFlags AccessColorAttachmentReadBit = VkAccessFlags.ColorAttachmentRead;

    /// <summary>
    /// Represents color attachment write access.
    /// </summary>
    public const VkAccessFlags AccessColorAttachmentWriteBit = VkAccessFlags.ColorAttachmentWrite;

    /// <summary>
    /// Represents depth/stencil attachment read access.
    /// </summary>
    public const VkAccessFlags AccessDepthStencilAttachmentReadBit = VkAccessFlags.DepthStencilAttachmentRead;

    /// <summary>
    /// Represents depth/stencil attachment write access.
    /// </summary>
    public const VkAccessFlags AccessDepthStencilAttachmentWriteBit = VkAccessFlags.DepthStencilAttachmentWrite;

    /// <summary>
    /// Represents transfer read access.
    /// </summary>
    public const VkAccessFlags AccessTransferReadBit = VkAccessFlags.TransferRead;

    /// <summary>
    /// Represents transfer write access.
    /// </summary>
    public const VkAccessFlags AccessTransferWriteBit = VkAccessFlags.TransferWrite;

    /// <summary>
    /// Represents host read access.
    /// </summary>
    public const VkAccessFlags AccessHostReadBit = VkAccessFlags.HostRead;

    /// <summary>
    /// Represents host write access.
    /// </summary>
    public const VkAccessFlags AccessHostWriteBit = VkAccessFlags.HostWrite;

    /// <summary>
    /// Represents the all commands pipeline stage mask.
    /// </summary>
    public const VkPipelineStageFlags PipelineStageAllCommandsBit = VkPipelineStageFlags.AllCommands;

    /// <summary>
    /// Stores instance dispatch APIs by Vulkan instance handle.
    /// </summary>
    private static readonly Dictionary<VkInstance, VkInstanceApi> _instanceApis = new();

    /// <summary>
    /// Stores instance dispatch APIs by Vulkan physical device handle.
    /// </summary>
    private static readonly Dictionary<VkPhysicalDevice, VkInstanceApi> _physicalDeviceApis = new();

    /// <summary>
    /// Stores device dispatch APIs by Vulkan device handle.
    /// </summary>
    private static readonly Dictionary<VkDevice, VkDeviceApi> _deviceApis = new();

    /// <summary>
    /// Stores device dispatch APIs by Vulkan queue handle.
    /// </summary>
    private static readonly Dictionary<VkQueue, VkDeviceApi> _queueApis = new();

    /// <summary>
    /// Stores device dispatch APIs by Vulkan command buffer handle.
    /// </summary>
    private static readonly Dictionary<VkCommandBuffer, VkDeviceApi> _commandBufferApis = new();

    /// <summary>
    /// Gets the dispatch API associated with the specified Vulkan instance.
    /// </summary>
    /// <param name="instance">The Vulkan instance handle.</param>
    /// <returns>The dispatch API associated with <paramref name="instance" />.</returns>
    public static VkInstanceApi GetApi(VkInstance instance) {
        return _instanceApis[instance];
    }

    /// <summary>
    /// Gets the dispatch API associated with the specified Vulkan physical device.
    /// </summary>
    /// <param name="physicalDevice">The Vulkan physical device handle.</param>
    /// <returns>The dispatch API associated with <paramref name="physicalDevice" />.</returns>
    public static VkInstanceApi GetApi(VkPhysicalDevice physicalDevice) {
        return _physicalDeviceApis[physicalDevice];
    }

    /// <summary>
    /// Gets the dispatch API associated with the specified Vulkan device.
    /// </summary>
    /// <param name="device">The Vulkan device handle.</param>
    /// <returns>The dispatch API associated with <paramref name="device" />.</returns>
    public static VkDeviceApi GetApi(VkDevice device) {
        return _deviceApis[device];
    }

    /// <summary>
    /// Gets the dispatch API associated with the specified Vulkan queue.
    /// </summary>
    /// <param name="queue">The Vulkan queue handle.</param>
    /// <returns>The dispatch API associated with <paramref name="queue" />.</returns>
    public static VkDeviceApi GetApi(VkQueue queue) {
        return _queueApis[queue];
    }

    /// <summary>
    /// Gets the dispatch API associated with the specified Vulkan command buffer.
    /// </summary>
    /// <param name="commandBuffer">The Vulkan command buffer handle.</param>
    /// <returns>The dispatch API associated with <paramref name="commandBuffer" />.</returns>
    public static VkDeviceApi GetApi(VkCommandBuffer commandBuffer) {
        return _commandBufferApis[commandBuffer];
    }

    /// <summary>
    /// Registers a Vulkan instance and creates its instance dispatch API.
    /// </summary>
    /// <param name="instance">The Vulkan instance handle.</param>
    /// <returns>The dispatch API created for <paramref name="instance" />.</returns>
    public static VkInstanceApi RegisterInstance(VkInstance instance) {
        VkInstanceApi api = new(instance);
        _instanceApis[instance] = api;
        return api;
    }

    /// <summary>
    /// Registers physical devices with the instance dispatch API that created them.
    /// </summary>
    /// <param name="instanceApi">The dispatch API associated with the physical devices.</param>
    /// <param name="physicalDevices">The physical devices to register.</param>
    /// <param name="count">The number of physical devices to register.</param>
    public static void RegisterPhysicalDevices(VkInstanceApi instanceApi, VkPhysicalDevice[] physicalDevices, uint count) {
        for (uint i = 0; i < count; i++) {
            _physicalDeviceApis[physicalDevices[i]] = instanceApi;
        }
    }

    /// <summary>
    /// Registers a Vulkan device and creates its device dispatch API.
    /// </summary>
    /// <param name="instanceApi">The instance dispatch API used to create the device.</param>
    /// <param name="device">The Vulkan device handle.</param>
    /// <returns>The dispatch API created for <paramref name="device" />.</returns>
    public static VkDeviceApi RegisterDevice(VkInstanceApi instanceApi, VkDevice device) {
        VkDeviceApi api = new(instanceApi, device);
        _deviceApis[device] = api;
        return api;
    }

    /// <summary>
    /// Registers a Vulkan queue with its device dispatch API.
    /// </summary>
    /// <param name="queue">The Vulkan queue handle.</param>
    /// <param name="deviceApi">The dispatch API associated with the queue.</param>
    public static void RegisterQueue(VkQueue queue, VkDeviceApi deviceApi) {
        _queueApis[queue] = deviceApi;
    }

    /// <summary>
    /// Registers a Vulkan command buffer with its device dispatch API.
    /// </summary>
    /// <param name="commandBuffer">The Vulkan command buffer handle.</param>
    /// <param name="deviceApi">The dispatch API associated with the command buffer.</param>
    public static void RegisterCommandBuffer(VkCommandBuffer commandBuffer, VkDeviceApi deviceApi) {
        _commandBufferApis[commandBuffer] = deviceApi;
    }
}
