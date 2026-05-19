#if !EXCLUDE_METAL_BACKEND
using System.Collections.ObjectModel;
using System.Linq;
using Veldrith.MetalBindings;
using Veldrith.MTL;

namespace Veldrith
{
    /// <summary>
    ///     Exposes Metal-specific functionality,
    ///     useful for interoperating with native components which interface directly with Metal.
    ///     Can only be used on <see cref="GraphicsBackend.Metal" />.
    /// </summary>
    public class BackendInfoMetal
    {
        public ReadOnlyCollection<MTLFeatureSet> FeatureSet { get; }

        public MTLFeatureSet MaxFeatureSet => gd.MetalFeatures.MaxFeatureSet;
        private readonly MtlGraphicsDevice gd;

        internal BackendInfoMetal(MtlGraphicsDevice gd)
        {
            this.gd = gd;
            FeatureSet = new ReadOnlyCollection<MTLFeatureSet>(this.gd.MetalFeatures.ToArray());
        }
    }
}
#endif
