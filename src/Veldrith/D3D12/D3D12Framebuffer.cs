using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Framebuffer.
/// </summary>
internal class D3D12Framebuffer : Framebuffer {

    /// <summary>
    /// Stores the color target textures collection used by this instance.
    /// </summary>
    private readonly D3D12Texture[] _colorTargetTextures;

    /// <summary>
    /// Stores the color target views state used by this instance.
    /// </summary>
    private readonly CpuDescriptorHandle[] _colorTargetViews;

    /// <summary>
    /// Stores the depth stencil view value used during command execution.
    /// </summary>
    private readonly CpuDescriptorHandle? _depthStencilView;

    /// <summary>
    /// Stores the dsv heap state used by this instance.
    /// </summary>
    private readonly ID3D12DescriptorHeap _dsvHeap;

    /// <summary>
    /// Stores the rtv heap state used by this instance.
    /// </summary>
    private readonly ID3D12DescriptorHeap _rtvHeap;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Framebuffer" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Framebuffer(D3D12GraphicsDevice gd, ref FramebufferDescription description) : base(description.DepthTarget, description.ColorTargets) {
        this._colorTargetTextures = new D3D12Texture[this.ColorTargets.Count];
        this._colorTargetViews = new CpuDescriptorHandle[this.ColorTargets.Count];
        if (this.ColorTargets.Count > 0) {
            this._rtvHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.RenderTargetView, (uint)this.ColorTargets.Count));
            int rtvDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
            CpuDescriptorHandle baseRtvHandle = this._rtvHeap.GetCPUDescriptorHandleForHeapStart();

            for (int i = 0; i < this.ColorTargets.Count; i++) {
                FramebufferAttachment attachment = this.ColorTargets[i];
                D3D12Texture texture = Util.AssertSubtype<Texture, D3D12Texture>(attachment.Target);
                this._colorTargetTextures[i] = texture;
                this._colorTargetViews[i] = baseRtvHandle + i * rtvDescriptorSize;

                if (texture.NativeTexture != null) {
                    RenderTargetViewDescription rtvDescription = CreateRenderTargetViewDescription(texture, attachment);
                    gd.Device.CreateRenderTargetView(texture.NativeTexture, rtvDescription, this._colorTargetViews[i]);
                }
            }
        }

        if (this.DepthTarget is FramebufferAttachment depthAttachment) {
            this.DepthTargetTexture = Util.AssertSubtype<Texture, D3D12Texture>(depthAttachment.Target);
            this._dsvHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.DepthStencilView, 1));
            CpuDescriptorHandle dsv = this._dsvHeap.GetCPUDescriptorHandleForHeapStart();
            this._depthStencilView = dsv;

            if (this.DepthTargetTexture.NativeTexture != null) {
                DepthStencilViewDescription dsvDescription = CreateDepthStencilViewDescription(this.DepthTargetTexture, depthAttachment);
                gd.Device.CreateDepthStencilView(this.DepthTargetTexture.NativeTexture, dsvDescription, dsv);
            }
        }
    }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Stores the color target textures collection used by this instance.
    /// </summary>
    internal ReadOnlySpan<D3D12Texture> ColorTargetTextures => this._colorTargetTextures;

    /// <summary>
    /// Gets or sets DepthTargetTexture.
    /// </summary>
    internal D3D12Texture DepthTargetTexture { get; }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Attempts to get color target view and reports whether it succeeded.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="handle">The handle value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool TryGetColorTargetView(uint index, out CpuDescriptorHandle handle) {
        if (index >= this._colorTargetViews.Length) {
            handle = default;
            return false;
        }

        handle = this._colorTargetViews[index];
        return this._colorTargetTextures[index]?.NativeTexture != null;
    }

    /// <summary>
    /// Attempts to get color target views and reports whether it succeeded.
    /// </summary>
    /// <param name="handles">The handles value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool TryGetColorTargetViews(out CpuDescriptorHandle[] handles) {
        handles = this._colorTargetViews;
        if (this._colorTargetViews.Length == 0) {
            return false;
        }

        for (int i = 0; i < this._colorTargetTextures.Length; i++) {
            if (this._colorTargetTextures[i]?.NativeTexture == null) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Attempts to get depth stencil view and reports whether it succeeded.
    /// </summary>
    /// <param name="handle">The handle value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool TryGetDepthStencilView(out CpuDescriptorHandle handle) {
        if (!this._depthStencilView.HasValue || this.DepthTargetTexture?.NativeTexture == null) {
            handle = default;
            return false;
        }

        handle = this._depthStencilView.Value;
        return true;
    }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this._rtvHeap?.Dispose();
        this._dsvHeap?.Dispose();
        this._disposed = true;
    }

    /// <summary>
    /// Creates the render target view description instance used by this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="attachment">The attachment value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static RenderTargetViewDescription CreateRenderTargetViewDescription(D3D12Texture texture, FramebufferAttachment attachment) {
        RenderTargetViewDescription description = new() {
            Format = D3D12Formats.GetViewFormat(D3D12Formats.ToDxgiFormat(texture.Format))
        };

        bool multisampled = texture.SampleCount != TextureSampleCount.Count1;
        switch (texture.Type) {
            case TextureType.Texture1D:
                if (texture.ArrayLayers > 1) {
                    description.ViewDimension = RenderTargetViewDimension.Texture1DArray;
                    description.Texture1DArray = new Texture1DArrayRenderTargetView {
                        MipSlice = attachment.MipLevel,
                        FirstArraySlice = attachment.ArrayLayer,
                        ArraySize = 1
                    };
                }
                else {
                    description.ViewDimension = RenderTargetViewDimension.Texture1D;
                    description.Texture1D = new Texture1DRenderTargetView {
                        MipSlice = attachment.MipLevel
                    };
                }

                break;
            case TextureType.Texture2D:
                if (multisampled) {
                    if (texture.ArrayLayers > 1) {
                        description.ViewDimension = RenderTargetViewDimension.Texture2DMultisampledArray;
                        description.Texture2DMSArray = new Texture2DMultisampledArrayRenderTargetView {
                            FirstArraySlice = attachment.ArrayLayer,
                            ArraySize = 1
                        };
                    }
                    else {
                        description.ViewDimension = RenderTargetViewDimension.Texture2DMultisampled;
                        description.Texture2DMS = new Texture2DMultisampledRenderTargetView();
                    }
                }
                else if (texture.ArrayLayers > 1) {
                    description.ViewDimension = RenderTargetViewDimension.Texture2DArray;
                    description.Texture2DArray = new Texture2DArrayRenderTargetView {
                        MipSlice = attachment.MipLevel,
                        FirstArraySlice = attachment.ArrayLayer,
                        ArraySize = 1,
                        PlaneSlice = 0
                    };
                }
                else {
                    description.ViewDimension = RenderTargetViewDimension.Texture2D;
                    description.Texture2D = new Texture2DRenderTargetView {
                        MipSlice = attachment.MipLevel,
                        PlaneSlice = 0
                    };
                }

                break;
            case TextureType.Texture3D:
                description.ViewDimension = RenderTargetViewDimension.Texture3D;
                description.Texture3D = new Texture3DRenderTargetView {
                    MipSlice = attachment.MipLevel,
                    FirstWSlice = attachment.ArrayLayer,
                    WSize = 1
                };
                break;
            default: throw Illegal.Value<TextureType>();
        }

        return description;
    }

    /// <summary>
    /// Creates the depth stencil view description instance used by this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="attachment">The attachment value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static DepthStencilViewDescription CreateDepthStencilViewDescription(D3D12Texture texture, FramebufferAttachment attachment) {
        DepthStencilViewDescription description = new() {
            Format = D3D12Formats.ToDepthFormat(texture.Format),
            Flags = DepthStencilViewFlags.None
        };

        bool multisampled = texture.SampleCount != TextureSampleCount.Count1;
        switch (texture.Type) {
            case TextureType.Texture1D:
                if (texture.ArrayLayers > 1) {
                    description.ViewDimension = DepthStencilViewDimension.Texture1DArray;
                    description.Texture1DArray = new Texture1DArrayDepthStencilView {
                        MipSlice = attachment.MipLevel,
                        FirstArraySlice = attachment.ArrayLayer,
                        ArraySize = 1
                    };
                }
                else {
                    description.ViewDimension = DepthStencilViewDimension.Texture1D;
                    description.Texture1D = new Texture1DDepthStencilView {
                        MipSlice = attachment.MipLevel
                    };
                }

                break;
            case TextureType.Texture2D:
                if (multisampled) {
                    if (texture.ArrayLayers > 1) {
                        description.ViewDimension = DepthStencilViewDimension.Texture2DMultisampledArray;
                        description.Texture2DMSArray = new Texture2DMultisampledArrayDepthStencilView {
                            FirstArraySlice = attachment.ArrayLayer,
                            ArraySize = 1
                        };
                    }
                    else {
                        description.ViewDimension = DepthStencilViewDimension.Texture2DMultisampled;
                        description.Texture2DMS = new Texture2DMultisampledDepthStencilView();
                    }
                }
                else if (texture.ArrayLayers > 1) {
                    description.ViewDimension = DepthStencilViewDimension.Texture2DArray;
                    description.Texture2DArray = new Texture2DArrayDepthStencilView {
                        MipSlice = attachment.MipLevel,
                        FirstArraySlice = attachment.ArrayLayer,
                        ArraySize = 1
                    };
                }
                else {
                    description.ViewDimension = DepthStencilViewDimension.Texture2D;
                    description.Texture2D = new Texture2DDepthStencilView {
                        MipSlice = attachment.MipLevel
                    };
                }

                break;
            default: throw new PlatformNotSupportedException("Depth-stencil views are not supported for this texture type.");
        }

        return description;
    }
}