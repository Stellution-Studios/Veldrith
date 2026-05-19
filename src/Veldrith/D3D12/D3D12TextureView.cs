using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12
{
    internal sealed class D3D12TextureView : TextureView
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly D3D12Texture _targetTexture;
        private ID3D12DescriptorHeap _srvDescriptorHeap;
        private ID3D12DescriptorHeap _uavDescriptorHeap;
        private bool _disposed;
        private string _name;

        public D3D12TextureView(D3D12GraphicsDevice gd, ref TextureViewDescription description)
            : base(ref description)
        {
            this.gd = gd;
            _targetTexture = Util.AssertSubtype<Texture, D3D12Texture>(description.Target);
        }

        internal D3D12Texture TargetTexture => _targetTexture;

        public override bool IsDisposed => _disposed;

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        internal CpuDescriptorHandle GetOrCreateShaderResourceViewDescriptor()
        {
            if (_srvDescriptorHeap == null)
            {
                _srvDescriptorHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                    DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                    1,
                    DescriptorHeapFlags.None));
                ID3D12Resource nativeTexture = _targetTexture.NativeTexture
                    ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
                ShaderResourceViewDescription srvDescription = GetShaderResourceViewDescription();
                gd.Device.CreateShaderResourceView(nativeTexture, srvDescription, _srvDescriptorHeap.GetCPUDescriptorHandleForHeapStart());
            }

            return _srvDescriptorHeap.GetCPUDescriptorHandleForHeapStart();
        }

        internal CpuDescriptorHandle GetOrCreateUnorderedAccessViewDescriptor()
        {
            if (_uavDescriptorHeap == null)
            {
                _uavDescriptorHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                    DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                    1,
                    DescriptorHeapFlags.None));
                ID3D12Resource nativeTexture = _targetTexture.NativeTexture
                    ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
                UnorderedAccessViewDescription uavDescription = GetUnorderedAccessViewDescription();
                gd.Device.CreateUnorderedAccessView(nativeTexture, null, uavDescription, _uavDescriptorHeap.GetCPUDescriptorHandleForHeapStart());
            }

            return _uavDescriptorHeap.GetCPUDescriptorHandleForHeapStart();
        }

        internal ShaderResourceViewDescription GetShaderResourceViewDescription()
        {
            var description = new ShaderResourceViewDescription
            {
                Format = D3D12Formats.GetViewFormat(D3D12Formats.ToDxgiFormat(Format, false)),
                Shader4ComponentMapping = ShaderComponentMapping.Default
            };

            if (_targetTexture.Type == TextureType.Texture3D)
            {
                description.ViewDimension = ShaderResourceViewDimension.Texture3D;
                description.Texture3D = new Texture3DShaderResourceView
                {
                    MostDetailedMip = BaseMipLevel,
                    MipLevels = MipLevels,
                    ResourceMinLODClamp = 0f
                };
                return description;
            }

            bool isMultisampled = _targetTexture.SampleCount != TextureSampleCount.Count1;
            bool isCube = (_targetTexture.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap;

            if (isCube)
            {
                if (ArrayLayers > 1)
                {
                    description.ViewDimension = ShaderResourceViewDimension.TextureCubeArray;
                    description.TextureCubeArray = new TextureCubeArrayShaderResourceView
                    {
                        MostDetailedMip = BaseMipLevel,
                        MipLevels = MipLevels,
                        First2DArrayFace = BaseArrayLayer * 6,
                        NumCubes = ArrayLayers,
                        ResourceMinLODClamp = 0f
                    };
                }
                else
                {
                    description.ViewDimension = ShaderResourceViewDimension.TextureCube;
                    description.TextureCube = new TextureCubeShaderResourceView
                    {
                        MostDetailedMip = BaseMipLevel,
                        MipLevels = MipLevels,
                        ResourceMinLODClamp = 0f
                    };
                }

                return description;
            }

            if (ArrayLayers > 1)
            {
                description.ViewDimension = isMultisampled
                    ? ShaderResourceViewDimension.Texture2DMultisampledArray
                    : ShaderResourceViewDimension.Texture2DArray;
                if (isMultisampled)
                {
                    description.Texture2DMSArray = new Texture2DMultisampledArrayShaderResourceView
                    {
                        FirstArraySlice = BaseArrayLayer,
                        ArraySize = ArrayLayers
                    };
                }
                else
                {
                    description.Texture2DArray = new Texture2DArrayShaderResourceView
                    {
                        MostDetailedMip = BaseMipLevel,
                        MipLevels = MipLevels,
                        FirstArraySlice = BaseArrayLayer,
                        ArraySize = ArrayLayers,
                        PlaneSlice = 0,
                        ResourceMinLODClamp = 0f
                    };
                }
            }
            else
            {
                description.ViewDimension = isMultisampled
                    ? ShaderResourceViewDimension.Texture2DMultisampled
                    : ShaderResourceViewDimension.Texture2D;
                if (isMultisampled)
                {
                    description.Texture2DMS = new Texture2DMultisampledShaderResourceView();
                }
                else
                {
                    description.Texture2D = new Texture2DShaderResourceView
                    {
                        MostDetailedMip = BaseMipLevel,
                        MipLevels = MipLevels,
                        PlaneSlice = 0,
                        ResourceMinLODClamp = 0f
                    };
                }
            }

            return description;
        }

        internal UnorderedAccessViewDescription GetUnorderedAccessViewDescription()
        {
            if (_targetTexture.SampleCount != TextureSampleCount.Count1)
            {
                throw new PlatformNotSupportedException("Multisampled UAV textures are not supported.");
            }

            var description = new UnorderedAccessViewDescription
            {
                Format = D3D12Formats.GetViewFormat(D3D12Formats.ToDxgiFormat(Format, false))
            };

            if (_targetTexture.Type == TextureType.Texture3D)
            {
                description.ViewDimension = UnorderedAccessViewDimension.Texture3D;
                description.Texture3D = new Texture3DUnorderedAccessView
                {
                    MipSlice = BaseMipLevel,
                    FirstWSlice = 0,
                    WSize = uint.MaxValue
                };
                return description;
            }

            if (ArrayLayers > 1)
            {
                description.ViewDimension = UnorderedAccessViewDimension.Texture2DArray;
                description.Texture2DArray = new Texture2DArrayUnorderedAccessView
                {
                    MipSlice = BaseMipLevel,
                    FirstArraySlice = BaseArrayLayer,
                    ArraySize = ArrayLayers,
                    PlaneSlice = 0
                };
            }
            else
            {
                description.ViewDimension = UnorderedAccessViewDimension.Texture2D;
                description.Texture2D = new Texture2DUnorderedAccessView
                {
                    MipSlice = BaseMipLevel,
                    PlaneSlice = 0
                };
            }

            return description;
        }

        public override void Dispose()
        {
            _srvDescriptorHeap?.Dispose();
            _uavDescriptorHeap?.Dispose();
            _disposed = true;
        }
    }
}
