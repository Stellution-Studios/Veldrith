using System;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

internal unsafe class VkShader : Shader {
    private readonly VkShaderModule _shaderModule;

    private readonly VkGraphicsDevice gd;
    private bool _disposed;
    private string _name;

    public VkShader(VkGraphicsDevice gd, ref ShaderDescription description)
        : base(description.Stage, description.EntryPoint) {
        this.gd = gd;

        VkShaderModuleCreateInfo shaderModuleCi = VkShaderModuleCreateInfo.New();

        fixed (byte* codePtr = description.ShaderBytes) {
            shaderModuleCi.codeSize = (UIntPtr)description.ShaderBytes.Length;
            shaderModuleCi.pCode = (uint*)codePtr;
            VkResult result = vkCreateShaderModule(gd.Device, ref shaderModuleCi, null, out this._shaderModule);
            CheckResult(result);
        }
    }

    public VkShaderModule ShaderModule => this._shaderModule;

    public override bool IsDisposed => this._disposed;

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            vkDestroyShaderModule(this.gd.Device, this.ShaderModule, null);
        }
    }

    #endregion
}