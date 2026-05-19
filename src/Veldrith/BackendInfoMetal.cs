#if !EXCLUDE_METAL_BACKEND
using System.Collections.ObjectModel;
using System.Linq;
using Veldrith.MetalBindings;
using Veldrith.MTL;

namespace Veldrith;

/// <summary>
/// Exposes backend-specific native handles and metadata for BackendInfoMetal.
/// </summary>
public class BackendInfoMetal {

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInfoMetal" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    internal BackendInfoMetal(MtlGraphicsDevice gd) {
        this.gd = gd;
        this.FeatureSet = new ReadOnlyCollection<MTLFeatureSet>(this.gd.MetalFeatures.ToArray());
    }

    /// <summary>
    /// Gets or sets FeatureSet.
    /// </summary>
    public ReadOnlyCollection<MTLFeatureSet> FeatureSet { get; }

    /// <summary>
    /// Stores the max feature set state used by this instance.
    /// </summary>
    public MTLFeatureSet MaxFeatureSet => this.gd.MetalFeatures.MaxFeatureSet;
}
#endif