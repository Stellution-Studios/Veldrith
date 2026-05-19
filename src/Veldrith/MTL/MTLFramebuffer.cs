using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlFramebuffer : Framebuffer
    {
        public override bool IsDisposed => this._disposed;

        public override string Name { get; set; }
        private bool _disposed;

        public MtlFramebuffer(MtlGraphicsDevice gd, ref FramebufferDescription description)
            : base(description.DepthTarget, description.ColorTargets)
        {
        }

        public MtlFramebuffer()
        {
        }

        #region Disposal

        public override void Dispose()
        {
            this._disposed = true;
        }

        #endregion

        public MTLRenderPassDescriptor CreateRenderPassDescriptor()
        {
            var ret = MTLRenderPassDescriptor.New();

            for (int i = 0; i < ColorTargets.Count; i++)
            {
                var colorTarget = ColorTargets[i];
                var mtlTarget = Util.AssertSubtype<Texture, MtlTexture>(colorTarget.Target);
                var colorDescriptor = ret.colorAttachments[(uint)i];
                colorDescriptor.texture = mtlTarget.DeviceTexture;
                colorDescriptor.loadAction = MTLLoadAction.Load;
                colorDescriptor.slice = colorTarget.ArrayLayer;
                colorDescriptor.level = colorTarget.MipLevel;
            }

            if (DepthTarget != null)
            {
                var mtlDepthTarget = Util.AssertSubtype<Texture, MtlTexture>(DepthTarget.Value.Target);
                var depthDescriptor = ret.depthAttachment;
                depthDescriptor.loadAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLLoadAction.DontCare : MTLLoadAction.Load;
                depthDescriptor.storeAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLStoreAction.DontCare : MTLStoreAction.Store;
                depthDescriptor.texture = mtlDepthTarget.DeviceTexture;
                depthDescriptor.slice = DepthTarget.Value.ArrayLayer;
                depthDescriptor.level = DepthTarget.Value.MipLevel;

                if (FormatHelpers.IsStencilFormat(mtlDepthTarget.Format))
                {
                    var stencilDescriptor = ret.stencilAttachment;
                    stencilDescriptor.loadAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLLoadAction.DontCare : MTLLoadAction.Load;
                    stencilDescriptor.storeAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLStoreAction.DontCare : MTLStoreAction.Store;
                    stencilDescriptor.texture = mtlDepthTarget.DeviceTexture;
                    stencilDescriptor.slice = DepthTarget.Value.ArrayLayer;
                }
            }

            return ret;
        }
    }
}
