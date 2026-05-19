using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12
{
    internal class D3D12Framebuffer : Framebuffer
    {
        private readonly D3D12Texture[] _colorTargetTextures;
        private readonly CpuDescriptorHandle[] _colorTargetViews;
        private readonly D3D12Texture _depthTargetTexture;
        private readonly CpuDescriptorHandle? _depthStencilView;
        private readonly ID3D12DescriptorHeap _rtvHeap;
        private readonly ID3D12DescriptorHeap _dsvHeap;
        private bool _disposed;
        private string _name;

        public D3D12Framebuffer(D3D12GraphicsDevice gd, ref FramebufferDescription description)
            : base(description.DepthTarget, description.ColorTargets)
        {
            _colorTargetTextures = new D3D12Texture[ColorTargets.Count];
            _colorTargetViews = new CpuDescriptorHandle[ColorTargets.Count];
            if (ColorTargets.Count > 0)
            {
                _rtvHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.RenderTargetView, (uint)ColorTargets.Count));
                int rtvDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
                CpuDescriptorHandle baseRtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();

                for (int i = 0; i < ColorTargets.Count; i++)
                {
                    FramebufferAttachment attachment = ColorTargets[i];
                    var texture = Util.AssertSubtype<Texture, D3D12Texture>(attachment.Target);
                    _colorTargetTextures[i] = texture;
                    _colorTargetViews[i] = baseRtvHandle + (i * rtvDescriptorSize);

                    if (texture.NativeTexture != null)
                    {
                        RenderTargetViewDescription rtvDescription = createRenderTargetViewDescription(texture, attachment);
                        gd.Device.CreateRenderTargetView(texture.NativeTexture, rtvDescription, _colorTargetViews[i]);
                    }
                }
            }

            if (DepthTarget is FramebufferAttachment depthAttachment)
            {
                _depthTargetTexture = Util.AssertSubtype<Texture, D3D12Texture>(depthAttachment.Target);
                _dsvHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.DepthStencilView, 1));
                CpuDescriptorHandle dsv = _dsvHeap.GetCPUDescriptorHandleForHeapStart();
                _depthStencilView = dsv;

                if (_depthTargetTexture.NativeTexture != null)
                {
                    DepthStencilViewDescription dsvDescription = createDepthStencilViewDescription(_depthTargetTexture, depthAttachment);
                    gd.Device.CreateDepthStencilView(_depthTargetTexture.NativeTexture, dsvDescription, dsv);
                }
            }
        }

        public override bool IsDisposed => _disposed;
        internal ReadOnlySpan<D3D12Texture> ColorTargetTextures => _colorTargetTextures;
        internal D3D12Texture DepthTargetTexture => _depthTargetTexture;

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        internal bool TryGetColorTargetView(uint index, out CpuDescriptorHandle handle)
        {
            if (index >= _colorTargetViews.Length)
            {
                handle = default;
                return false;
            }

            handle = _colorTargetViews[index];
            return _colorTargetTextures[index]?.NativeTexture != null;
        }

        internal bool TryGetColorTargetViews(out CpuDescriptorHandle[] handles)
        {
            handles = _colorTargetViews;
            if (_colorTargetViews.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < _colorTargetTextures.Length; i++)
            {
                if (_colorTargetTextures[i]?.NativeTexture == null)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool TryGetDepthStencilView(out CpuDescriptorHandle handle)
        {
            if (!_depthStencilView.HasValue || _depthTargetTexture?.NativeTexture == null)
            {
                handle = default;
                return false;
            }

            handle = _depthStencilView.Value;
            return true;
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _rtvHeap?.Dispose();
            _dsvHeap?.Dispose();
            _disposed = true;
        }

        private static RenderTargetViewDescription createRenderTargetViewDescription(D3D12Texture texture, FramebufferAttachment attachment)
        {
            var description = new RenderTargetViewDescription
            {
                Format = D3D12Formats.GetViewFormat(D3D12Formats.ToDxgiFormat(texture.Format))
            };

            bool multisampled = texture.SampleCount != TextureSampleCount.Count1;
            switch (texture.Type)
            {
                case TextureType.Texture1D:
                    if (texture.ArrayLayers > 1)
                    {
                        description.ViewDimension = RenderTargetViewDimension.Texture1DArray;
                        description.Texture1DArray = new Texture1DArrayRenderTargetView
                        {
                            MipSlice = attachment.MipLevel,
                            FirstArraySlice = attachment.ArrayLayer,
                            ArraySize = 1
                        };
                    }
                    else
                    {
                        description.ViewDimension = RenderTargetViewDimension.Texture1D;
                        description.Texture1D = new Texture1DRenderTargetView
                        {
                            MipSlice = attachment.MipLevel
                        };
                    }

                    break;
                case TextureType.Texture2D:
                    if (multisampled)
                    {
                        if (texture.ArrayLayers > 1)
                        {
                            description.ViewDimension = RenderTargetViewDimension.Texture2DMultisampledArray;
                            description.Texture2DMSArray = new Texture2DMultisampledArrayRenderTargetView
                            {
                                FirstArraySlice = attachment.ArrayLayer,
                                ArraySize = 1
                            };
                        }
                        else
                        {
                            description.ViewDimension = RenderTargetViewDimension.Texture2DMultisampled;
                            description.Texture2DMS = new Texture2DMultisampledRenderTargetView();
                        }
                    }
                    else if (texture.ArrayLayers > 1)
                    {
                        description.ViewDimension = RenderTargetViewDimension.Texture2DArray;
                        description.Texture2DArray = new Texture2DArrayRenderTargetView
                        {
                            MipSlice = attachment.MipLevel,
                            FirstArraySlice = attachment.ArrayLayer,
                            ArraySize = 1,
                            PlaneSlice = 0
                        };
                    }
                    else
                    {
                        description.ViewDimension = RenderTargetViewDimension.Texture2D;
                        description.Texture2D = new Texture2DRenderTargetView
                        {
                            MipSlice = attachment.MipLevel,
                            PlaneSlice = 0
                        };
                    }

                    break;
                case TextureType.Texture3D:
                    description.ViewDimension = RenderTargetViewDimension.Texture3D;
                    description.Texture3D = new Texture3DRenderTargetView
                    {
                        MipSlice = attachment.MipLevel,
                        FirstWSlice = attachment.ArrayLayer,
                        WSize = 1
                    };
                    break;
                default:
                    throw Illegal.Value<TextureType>();
            }

            return description;
        }

        private static DepthStencilViewDescription createDepthStencilViewDescription(D3D12Texture texture, FramebufferAttachment attachment)
        {
            var description = new DepthStencilViewDescription
            {
                Format = D3D12Formats.ToDepthFormat(texture.Format),
                Flags = DepthStencilViewFlags.None
            };

            bool multisampled = texture.SampleCount != TextureSampleCount.Count1;
            switch (texture.Type)
            {
                case TextureType.Texture1D:
                    if (texture.ArrayLayers > 1)
                    {
                        description.ViewDimension = DepthStencilViewDimension.Texture1DArray;
                        description.Texture1DArray = new Texture1DArrayDepthStencilView
                        {
                            MipSlice = attachment.MipLevel,
                            FirstArraySlice = attachment.ArrayLayer,
                            ArraySize = 1
                        };
                    }
                    else
                    {
                        description.ViewDimension = DepthStencilViewDimension.Texture1D;
                        description.Texture1D = new Texture1DDepthStencilView
                        {
                            MipSlice = attachment.MipLevel
                        };
                    }

                    break;
                case TextureType.Texture2D:
                    if (multisampled)
                    {
                        if (texture.ArrayLayers > 1)
                        {
                            description.ViewDimension = DepthStencilViewDimension.Texture2DMultisampledArray;
                            description.Texture2DMSArray = new Texture2DMultisampledArrayDepthStencilView
                            {
                                FirstArraySlice = attachment.ArrayLayer,
                                ArraySize = 1
                            };
                        }
                        else
                        {
                            description.ViewDimension = DepthStencilViewDimension.Texture2DMultisampled;
                            description.Texture2DMS = new Texture2DMultisampledDepthStencilView();
                        }
                    }
                    else if (texture.ArrayLayers > 1)
                    {
                        description.ViewDimension = DepthStencilViewDimension.Texture2DArray;
                        description.Texture2DArray = new Texture2DArrayDepthStencilView
                        {
                            MipSlice = attachment.MipLevel,
                            FirstArraySlice = attachment.ArrayLayer,
                            ArraySize = 1
                        };
                    }
                    else
                    {
                        description.ViewDimension = DepthStencilViewDimension.Texture2D;
                        description.Texture2D = new Texture2DDepthStencilView
                        {
                            MipSlice = attachment.MipLevel
                        };
                    }

                    break;
                default:
                    throw new PlatformNotSupportedException("Depth-stencil views are not supported for this texture type.");
            }

            return description;
        }
    }
}
