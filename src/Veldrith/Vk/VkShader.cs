using System;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkShader class.
/// </summary>
internal unsafe class VkShader : Shader {

    /// <summary>
    /// Stores the value associated with <c>_shaderModule</c>.
    /// </summary>
    private readonly VkShaderModule _shaderModule;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the value associated with <c>_name</c>.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkShader" /> class.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
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
    /// Stores the value associated with <c>ShaderModule</c>.
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            vkDestroyShaderModule(this.gd.Device, this.ShaderModule, null);
        }
    }

    #endregion
}
