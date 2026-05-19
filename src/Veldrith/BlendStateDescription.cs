using System;

namespace Veldrith
{
    /// <summary>
    ///     A <see cref="Pipeline" /> component describing how values are blended into each individual color target.
    /// </summary>
    public struct BlendStateDescription : IEquatable<BlendStateDescription>
    {
        /// <summary>
        ///     A constant blend color used in <see cref="BlendFactor.BlendFactor" /> and
        ///     <see cref="BlendFactor.InverseBlendFactor" />,
        ///     or otherwise ignored.
        /// </summary>
        public RgbaFloat BlendFactor;

        /// <summary>
        ///     An array of <see cref="BlendAttachmentDescription" /> describing how blending is performed for each color target
        ///     used in the <see cref="Pipeline" />.
        /// </summary>
        public BlendAttachmentDescription[] AttachmentStates;

        /// <summary>
        ///     Enables alpha-to-coverage, which causes a fragment's alpha value to be used when determining multi-sample coverage.
        /// </summary>
        public bool AlphaToCoverageEnabled;

        /// <summary>
        ///     Constructs a new <see cref="BlendStateDescription" />,
        /// </summary>
        /// <param name="blendFactor">The constant blend color.</param>
        /// <param name="attachmentStates">The blend attachment states.</param>
        public BlendStateDescription(RgbaFloat blendFactor, params BlendAttachmentDescription[] attachmentStates)
        {
            this.BlendFactor = blendFactor;
            this.AttachmentStates = attachmentStates;
            this.AlphaToCoverageEnabled = false;
        }

        /// <summary>
        ///     Constructs a new <see cref="BlendStateDescription" />,
        /// </summary>
        /// <param name="blendFactor">The constant blend color.</param>
        /// <param name="alphaToCoverageEnabled">
        ///     Enables alpha-to-coverage, which causes a fragment's alpha value to be
        ///     used when determining multi-sample coverage.
        /// </param>
        /// <param name="attachmentStates">The blend attachment states.</param>
        public BlendStateDescription(
            RgbaFloat blendFactor,
            bool alphaToCoverageEnabled,
            params BlendAttachmentDescription[] attachmentStates)
        {
            this.BlendFactor = blendFactor;
            this.AttachmentStates = attachmentStates;
            this.AlphaToCoverageEnabled = alphaToCoverageEnabled;
        }

        /// <summary>
        ///     Describes a blend state in which a single color target is blended with
        ///     <see cref="BlendAttachmentDescription.OVERRIDE_BLEND" />.
        /// </summary>
        public static readonly BlendStateDescription SINGLE_OVERRIDE_BLEND = new BlendStateDescription
        {
            AttachmentStates = new[] { BlendAttachmentDescription.OVERRIDE_BLEND }
        };

        /// <summary>
        ///     Describes a blend state in which a single color target is blended with
        ///     <see cref="BlendAttachmentDescription.ALPHA_BLEND" />.
        /// </summary>
        public static readonly BlendStateDescription SINGLE_ALPHA_BLEND = new BlendStateDescription
        {
            AttachmentStates = new[] { BlendAttachmentDescription.ALPHA_BLEND }
        };

        /// <summary>
        ///     Describes a blend state in which a single color target is blended with
        ///     <see cref="BlendAttachmentDescription.ADDITIVE_BLEND" />.
        /// </summary>
        public static readonly BlendStateDescription SINGLE_ADDITIVE_BLEND = new BlendStateDescription
        {
            AttachmentStates = new[] { BlendAttachmentDescription.ADDITIVE_BLEND }
        };

        /// <summary>
        ///     Describes a blend state in which a single color target is blended with
        ///     <see cref="BlendAttachmentDescription.DISABLED" />.
        /// </summary>
        public static readonly BlendStateDescription SINGLE_DISABLED = new BlendStateDescription
        {
            AttachmentStates = new[] { BlendAttachmentDescription.DISABLED }
        };

        /// <summary>
        ///     Describes an empty blend state in which no color targets are used.
        /// </summary>
        public static readonly BlendStateDescription EMPTY = new BlendStateDescription
        {
            AttachmentStates = Array.Empty<BlendAttachmentDescription>()
        };

        /// <summary>
        ///     Element-wise equality.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True if all elements and all array elements are equal; false otherswise.</returns>
        public bool Equals(BlendStateDescription other)
        {
            return this.BlendFactor.Equals(other.BlendFactor)
                   && this.AlphaToCoverageEnabled.Equals(other.AlphaToCoverageEnabled)
                   && Util.ArrayEqualsEquatable(this.AttachmentStates, other.AttachmentStates);
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return HashHelper.Combine(
                this.BlendFactor.GetHashCode(),
                this.AlphaToCoverageEnabled.GetHashCode(),
                HashHelper.Array(this.AttachmentStates));
        }

        internal BlendStateDescription ShallowClone()
        {
            var result = this;
            result.AttachmentStates = Util.ShallowClone(result.AttachmentStates);
            return result;
        }
    }
}
