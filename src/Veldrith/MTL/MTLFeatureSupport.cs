using System;
using System.Collections;
using System.Collections.Generic;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlFeatureSupport class.
/// </summary>
internal class MtlFeatureSupport : IReadOnlyCollection<MTLFeatureSet> {

    /// <summary>
    /// Represents the _supportedFeatureSets field.
    /// </summary>
    private readonly HashSet<MTLFeatureSet> _supportedFeatureSets = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFeatureSupport" /> class.
    /// </summary>
    public MtlFeatureSupport(MTLDevice device) {
        foreach (MTLFeatureSet set in Enum.GetValues(typeof(MTLFeatureSet))) {
            if (device.supportsFeatureSet(set)) {
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
    /// Represents the Count field.
    /// </summary>
    public int Count => this._supportedFeatureSets.Count;

    /// <summary>
    /// Executes GetEnumerator.
    /// </summary>
    public IEnumerator<MTLFeatureSet> GetEnumerator() {
        return this._supportedFeatureSets.GetEnumerator();
    }

    /// <summary>
    /// Executes IEnumerable.GetEnumerator.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }

    /// <summary>
    /// Executes IsSupported.
    /// </summary>
    public bool IsSupported(MTLFeatureSet featureSet) {
        return this._supportedFeatureSets.Contains(featureSet);
    }

    /// <summary>
    /// Executes IsDrawBaseVertexInstanceSupported.
    /// </summary>
    public bool IsDrawBaseVertexInstanceSupported() {
        return this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v1)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v2)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v3)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily4_v1)
               || this.IsSupported(MTLFeatureSet.tvOS_GPUFamily2_v1)
               || this.IsMacOS;
    }
}