using System;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VkShader class.
/// </summary>
internal unsafe class VkShader : Shader {

    /// <summary>
    /// Represents the _shaderModule field.
    /// </summary>
    private readonly VkShaderModule _shaderModule;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Represents the _name field.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkShader" /> class.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the base operation.</returns>
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
    /// Represents the ShaderModule field.
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
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            vkDestroyShaderModule(this.gd.Device, this.ShaderModule, null);
        }
    }

    #endregion
}
