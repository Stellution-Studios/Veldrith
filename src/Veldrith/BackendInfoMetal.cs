#if !EXCLUDE_METAL_BACKEND
using System.Collections.ObjectModel;
using System.Linq;
using Veldrith.MetalBindings;
using Veldrith.MTL;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the BackendInfoMetal class.
/// </summary>
public class BackendInfoMetal {

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInfoMetal" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    internal BackendInfoMetal(MtlGraphicsDevice gd) {
        this.gd = gd;
        this.FeatureSet = new ReadOnlyCollection<MTLFeatureSet>(this.gd.MetalFeatures.ToArray());
    }

    /// <summary>
    /// Gets or sets FeatureSet.
    /// </summary>
    public ReadOnlyCollection<MTLFeatureSet> FeatureSet { get; }

    /// <summary>
    /// Stores the value associated with <c>MaxFeatureSet</c>.
    /// </summary>
    public MTLFeatureSet MaxFeatureSet => this.gd.MetalFeatures.MaxFeatureSet;
}
#endif