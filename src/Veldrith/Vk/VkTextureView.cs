using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk
{
    internal unsafe class VkTextureView : TextureView
    {
        public VkImageView ImageView => _imageView;

        public new VkTexture Target => (VkTexture)base.Target;

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => _destroyed;

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
        private readonly VkImageView _imageView;
        private bool _destroyed;
        private string _name;

        public VkTextureView(VkGraphicsDevice gd, ref TextureViewDescription description)
            : base(ref description)
        {
            this.gd = gd;
            var imageViewCi = VkImageViewCreateInfo.New();
            var tex = Util.AssertSubtype<Texture, VkTexture>(description.Target);
            imageViewCi.image = tex.OptimalDeviceImage;
            imageViewCi.format = VkFormats.VdToVkPixelFormat(Format, (Target.Usage & TextureUsage.DepthStencil) != 0);

            var aspectFlags = (description.Target.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil
                ? VkImageAspectFlags.Depth
                : VkImageAspectFlags.Color;

            imageViewCi.subresourceRange = new VkImageSubresourceRange(
                aspectFlags,
                description.BaseMipLevel,
                description.MipLevels,
                description.BaseArrayLayer,
                description.ArrayLayers);

            if ((tex.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap)
            {
                imageViewCi.viewType = description.ArrayLayers == 1 ? VkImageViewType.ImageCube : VkImageViewType.ImageCubeArray;
                imageViewCi.subresourceRange.layerCount *= 6;
            }
            else
            {
                switch (tex.Type)
                {
                    case TextureType.Texture1D:
                        imageViewCi.viewType = description.ArrayLayers == 1
                            ? VkImageViewType.Image1D
                            : VkImageViewType.Image1DArray;
                        break;

                    case TextureType.Texture2D:
                        imageViewCi.viewType = description.ArrayLayers == 1
                            ? VkImageViewType.Image2D
                            : VkImageViewType.Image2DArray;
                        break;

                    case TextureType.Texture3D:
                        imageViewCi.viewType = VkImageViewType.Image3D;
                        break;
                }
            }

            vkCreateImageView(this.gd.Device, ref imageViewCi, null, out _imageView);
            RefCount = new ResourceRefCount(disposeCore);
        }

        #region Disposal

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        #endregion

        private void disposeCore()
        {
            if (!_destroyed)
            {
                _destroyed = true;
                vkDestroyImageView(gd.Device, ImageView, null);
            }
        }
    }
}
