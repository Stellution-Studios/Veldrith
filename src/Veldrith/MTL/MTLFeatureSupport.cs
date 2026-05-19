using System;
using System.Collections;
using System.Collections.Generic;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlFeatureSupport : IReadOnlyCollection<MTLFeatureSet> {
    private readonly HashSet<MTLFeatureSet> _supportedFeatureSets = new();

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

    public bool IsMacOS { get; }

    public MTLFeatureSet MaxFeatureSet { get; }

    public int Count => this._supportedFeatureSets.Count;

    public IEnumerator<MTLFeatureSet> GetEnumerator() {
        return this._supportedFeatureSets.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }

    public bool IsSupported(MTLFeatureSet featureSet) {
        return this._supportedFeatureSets.Contains(featureSet);
    }

    public bool IsDrawBaseVertexInstanceSupported() {
        return this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v1)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v2)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily3_v3)
               || this.IsSupported(MTLFeatureSet.iOS_GPUFamily4_v1)
               || this.IsSupported(MTLFeatureSet.tvOS_GPUFamily2_v1)
               || this.IsMacOS;
    }
}