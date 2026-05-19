using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkTextureView.
/// </summary>
internal unsafe class VkTextureView : TextureView {

    /// <summary>
    /// Stores the image view state used by this instance.
    /// </summary>
    private readonly VkImageView _imageView;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkTextureView" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkTextureView(VkGraphicsDevice gd, ref TextureViewDescription description) : base(ref description) {
        this.gd = gd;
        VkImageViewCreateInfo imageViewCi = VkImageViewCreateInfo.New();
        VkTexture tex = Util.AssertSubtype<Texture, VkTexture>(description.Target);
        imageViewCi.image = tex.OptimalDeviceImage;
        imageViewCi.format = VkFormats.VdToVkPixelFormat(this.Format, (this.Target.Usage & TextureUsage.DepthStencil) != 0);

        VkImageAspectFlags aspectFlags = (description.Target.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil
                ? VkImageAspectFlags.Depth
                : VkImageAspectFlags.Color;

        imageViewCi.subresourceRange = new VkImageSubresourceRange(aspectFlags, description.BaseMipLevel, description.MipLevels, description.BaseArrayLayer, description.ArrayLayers);

        if ((tex.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap) {
            imageViewCi.viewType = description.ArrayLayers == 1 ? VkImageViewType.ImageCube : VkImageViewType.ImageCubeArray;
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

    /// <summary>
    /// Stores the image view state used by this instance.
    /// </summary>
    public VkImageView ImageView => this._imageView;

    /// <summary>
    /// Gets or sets Target.
    /// </summary>

    public new VkTexture Target => (VkTexture)base.Target;

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

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
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            vkDestroyImageView(this.gd.Device, this.ImageView, null);
        }
    }
}