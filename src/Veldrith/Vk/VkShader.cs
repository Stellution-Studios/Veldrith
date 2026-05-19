using System;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkShader : Shader
    {
        public VkShaderModule ShaderModule => _shaderModule;

        public override bool IsDisposed => _disposed;

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly VkShaderModule _shaderModule;
        private bool _disposed;
        private string _name;

        public VkShader(VkGraphicsDevice gd, ref ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            this.gd = gd;

            var shaderModuleCi = VkShaderModuleCreateInfo.New();

            fixed (byte* codePtr = description.ShaderBytes)
            {
                shaderModuleCi.codeSize = (UIntPtr)description.ShaderBytes.Length;
                shaderModuleCi.pCode = (uint*)codePtr;
                var result = vkCreateShaderModule(gd.Device, ref shaderModuleCi, null, out _shaderModule);
                CheckResult(result);
            }
        }

        #region Disposal

        public override void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                vkDestroyShaderModule(gd.Device, ShaderModule, null);
            }
        }

        #endregion
    }
}
