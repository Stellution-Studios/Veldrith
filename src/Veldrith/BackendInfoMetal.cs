#if !EXCLUDE_METAL_BACKEND
using System.Collections.ObjectModel;
using System.Linq;
using Veldrith.MetalBindings;
using Veldrith.MTL;

namespace Veldrith;

/// <summary>
/// Represents the BackendInfoMetal class.
/// </summary>
public class BackendInfoMetal {

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInfoMetal" /> class.
    /// </summary>
    internal BackendInfoMetal(MtlGraphicsDevice gd) {
        this.gd = gd;
        this.FeatureSet = new ReadOnlyCollection<MTLFeatureSet>(this.gd.MetalFeatures.ToArray());
    }

    /// <summary>
    /// Gets or sets FeatureSet.
    /// </summary>
    public ReadOnlyCollection<MTLFeatureSet> FeatureSet { get; }

    /// <summary>
    /// Represents the MaxFeatureSet field.
    /// </summary>
    public MTLFeatureSet MaxFeatureSet => this.gd.MetalFeatures.MaxFeatureSet;
}
#endif