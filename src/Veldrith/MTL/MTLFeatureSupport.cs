using System;
using System.Collections;
using System.Collections.Generic;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlFeatureSupport class.
/// </summary>
internal class MtlFeatureSupport : IReadOnlyCollection<MTLFeatureSet> {

    /// <summary>
    /// Stores the value associated with <c>_supportedFeatureSets</c>.
    /// </summary>
    private readonly HashSet<MTLFeatureSet> _supportedFeatureSets = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFeatureSupport" /> type.
    /// </summary>
    /// <param name="device">Specifies the value of <paramref name="device" />.</param>
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
    /// Stores the value associated with <c>Count</c>.
    /// </summary>
    public int Count => this._supportedFeatureSets.Count;

    /// <summary>
    /// Executes the GetEnumerator operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetEnumerator operation.</returns>
    public IEnumerator<MTLFeatureSet> GetEnumerator() {
        return this._supportedFeatureSets.GetEnumerator();
    }

    /// <summary>
    /// Executes the GetEnumerator operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetEnumerator operation.</returns>
    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }

    /// <summary>
    /// Executes the IsSupported operation.
    /// </summary>
    /// <param name="featureSet">Specifies the value of <paramref name="featureSet" />.</param>
    /// <returns>Returns the result produced by the IsSupported operation.</returns>
    public bool IsSupported(MTLFeatureSet featureSet) {
        return this._supportedFeatureSets.Contains(featureSet);
    }

    /// <summary>
    /// Executes the IsDrawBaseVertexInstanceSupported operation.
    /// </summary>
    /// <returns>Returns the result produced by the IsDrawBaseVertexInstanceSupported operation.</returns>
    public bool IsDrawBaseVertexInstanceSupported() {
        return this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v1)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v2)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v3)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily4_v1)
               || this.IsSupported(MTLFeatureSet.tvOS_GPUFamily2_v1)
               || this.IsMacOS;
    }
}
