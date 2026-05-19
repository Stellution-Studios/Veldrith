using System;
using System.Collections;
using System.Collections.Generic;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlFeatureSupport : IReadOnlyCollection<MTLFeatureSet>
    {
        public bool IsMacOS { get; }

        public MTLFeatureSet MaxFeatureSet { get; }

        public int Count => supportedFeatureSets.Count;
        private readonly HashSet<MTLFeatureSet> supportedFeatureSets = new HashSet<MTLFeatureSet>();

        public MtlFeatureSupport(MTLDevice device)
        {
            foreach (MTLFeatureSet set in Enum.GetValues(typeof(MTLFeatureSet)))
            {
                if (device.supportsFeatureSet(set))
                {
                    supportedFeatureSets.Add(set);
                    MaxFeatureSet = set;
                }
            }

            IsMacOS = IsSupported(MTLFeatureSet.macOS_GPUFamily1_v1)
                      || IsSupported(MTLFeatureSet.macOS_GPUFamily1_v2)
                      || IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3);
        }

        public bool IsSupported(MTLFeatureSet featureSet)
        {
            return supportedFeatureSets.Contains(featureSet);
        }

        public bool IsDrawBaseVertexInstanceSupported()
        {
            return IsSupported(MTLFeatureSet.iOS_GPUFamily3_v1)
                   || IsSupported(MTLFeatureSet.iOS_GPUFamily3_v2)
                   || IsSupported(MTLFeatureSet.iOS_GPUFamily3_v3)
                   || IsSupported(MTLFeatureSet.iOS_GPUFamily4_v1)
                   || IsSupported(MTLFeatureSet.tvOS_GPUFamily2_v1)
                   || IsMacOS;
        }

        public IEnumerator<MTLFeatureSet> GetEnumerator()
        {
            return supportedFeatureSets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
