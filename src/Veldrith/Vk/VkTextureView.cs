using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal unsafe class VkTextureView : TextureView {
    private readonly VkImageView _imageView;

    private readonly VkGraphicsDevice gd;
    private bool _destroyed;
    private string _name;

    public VkTextureView(VkGraphicsDevice gd, ref TextureViewDescription description)
        : base(ref description) {
        this.gd = gd;
        VkImageViewCreateInfo imageViewCi = VkImageViewCreateInfo.New();
        VkTexture tex = Util.AssertSubtype<Texture, VkTexture>(description.Target);
        imageViewCi.image = tex.OptimalDeviceImage;
        imageViewCi.format =
            VkFormats.VdToVkPixelFormat(this.Format, (this.Target.Usage & TextureUsage.DepthStencil) != 0);

        VkImageAspectFlags aspectFlags =
            (description.Target.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil
                ? VkImageAspectFlags.Depth
                : VkImageAspectFlags.Color;

        imageViewCi.subresourceRange = new VkImageSubresourceRange(
            aspectFlags,
            description.BaseMipLevel,
            description.MipLevels,
            description.BaseArrayLayer,
            description.ArrayLayers);

        if ((tex.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap) {
            imageViewCi.viewType =
                description.ArrayLayers == 1 ? VkImageViewType.ImageCube : VkImageViewType.ImageCubeArray;
            imageViewCi.subresourceRange.layerCount *= 6;
        }
        else {
            switch (tex.Type) {
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

        vkCreateImageView(this.gd.Device, ref imageViewCi, null, out this._imageView);
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    public VkImageView ImageView => this._imageView;

    public new VkTexture Target => (VkTexture)base.Target;

    public ResourceRefCount RefCount { get; }

    public override bool IsDisposed => this._destroyed;

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            vkDestroyImageView(this.gd.Device, this.ImageView, null);
        }
    }
}