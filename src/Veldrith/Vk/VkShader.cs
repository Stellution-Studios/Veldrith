using System;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkShader.
/// </summary>
internal unsafe class VkShader : Shader {

    /// <summary>
    /// Stores the shader module state used by this instance.
    /// </summary>
    private readonly VkShaderModule _shaderModule;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkShader" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkShader(VkGraphicsDevice gd, ref ShaderDescription description) : base(description.Stage, description.EntryPoint) {
        this.gd = gd;

        VkShaderModuleCreateInfo shaderModuleCi = VkShaderModuleCreateInfo.New();

        fixed (byte* codePtr = description.ShaderBytes) {
            shaderModuleCi.codeSize = (UIntPtr)description.ShaderBytes.Length;
            shaderModuleCi.pCode = (uint*)codePtr;
            VkResult result = vkCreateShaderModule(gd.Device, ref shaderModuleCi, null, out this._shaderModule);
            CheckResult(result);
        }
    }

    /// <summary>
    /// Stores the shader module state used by this instance.
    /// </summary>
    public VkShaderModule ShaderModule => this._shaderModule;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            vkDestroyShaderModule(this.gd.Device, this.ShaderModule, null);
        }
    }

    #endregion
}