using System;
using System.Collections;
using System.Collections.Generic;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlFeatureSupport.
/// </summary>
internal class MtlFeatureSupport : IReadOnlyCollection<MTLFeatureSet> {

    /// <summary>
    /// Stores the supported feature sets state used by this instance.
    /// </summary>
    private readonly HashSet<MTLFeatureSet> _supportedFeatureSets = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFeatureSupport" /> type.
    /// </summary>
    /// <param name="device">The device value used by this operation.</param>
    public MtlFeatureSupport(MTLDevice device) {
        foreach (MTLFeatureSet set in Enum.GetValues(typeof(MTLFeatureSet))) {
            if (device.SupportsFeatureSet(set)) {
                this._supportedFeatureSets.Add(set);
                this.MaxFeatureSet = set;
            }
        }

        this.IsMacOS = this.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v1)
                       || this.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v2)
                       || this.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3);
    }

    /// <summary>
    /// Gets or sets IsMacOS.
    /// </summary>
    public bool IsMacOS { get; }

    /// <summary>
    /// Gets or sets MaxFeatureSet.
    /// </summary>
    public MTLFeatureSet MaxFeatureSet { get; }

    /// <summary>
    /// Stores the count value used during command execution.
    /// </summary>
    public int Count => this._supportedFeatureSets.Count;

    /// <summary>
    /// Gets the enumerator value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public IEnumerator<MTLFeatureSet> GetEnumerator() {
        return this._supportedFeatureSets.GetEnumerator();
    }

    /// <summary>
    /// Gets the enumerator value.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }

    /// <summary>
    /// Executes the is supported logic for this backend.
    /// </summary>
    /// <param name="featureSet">The feature set value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool IsSupported(MTLFeatureSet featureSet) {
        return this._supportedFeatureSets.Contains(featureSet);
    }

    /// <summary>
    /// Executes the is draw base vertex instance supported logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool IsDrawBaseVertexInstanceSupported() {
        return this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v1)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v2)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v3)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily4_v1)
               || this.IsSupported(MTLFeatureSet.tvOS_GPUFamily2_v1)
               || this.IsMacOS;
    }
}