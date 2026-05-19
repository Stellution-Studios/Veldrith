using System;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkShader : Shader
    {
        public VkShaderModule ShaderModule => shaderModule;

        public override bool IsDisposed => disposed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly VkShaderModule shaderModule;
        private bool disposed;
        private string name;

        public VkShader(VkGraphicsDevice gd, ref ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            this.gd = gd;

            var shaderModuleCi = VkShaderModuleCreateInfo.New();

            fixed (byte* codePtr = description.ShaderBytes)
            {
                shaderModuleCi.codeSize = (UIntPtr)description.ShaderBytes.Length;
                shaderModuleCi.pCode = (uint*)codePtr;
                var result = vkCreateShaderModule(gd.Device, ref shaderModuleCi, null, out shaderModule);
                CheckResult(result);
            }
        }

        #region Disposal

        public override void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                vkDestroyShaderModule(gd.Device, ShaderModule, null);
            }
        }

        #endregion
    }
}
