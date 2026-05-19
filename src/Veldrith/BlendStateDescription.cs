using System;

namespace Veldrith;

/// <summary>
/// A <see cref="Pipeline" /> component describing how values are blended into each individual color target.
/// </summary>
public struct BlendStateDescription : IEquatable<BlendStateDescription> {

    /// <summary>
    /// A constant blend color used in <see cref="BlendFactor.BlendFactor" /> and
    /// </summary>
    public RgbaFloat BlendFactor;

    /// <summary>
    /// An array of <see cref="BlendAttachmentDescription" /> describing how blending is performed for each color target
    /// </summary>
    public BlendAttachmentDescription[] AttachmentStates;

    /// <summary>
    /// Enables alpha-to-coverage, which causes a fragment's alpha value to be used when determining multi-sample coverage.
    /// </summary>
    public bool AlphaToCoverageEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlendStateDescription" /> type.
    /// </summary>
    /// <param name="blendFactor">The blend factor value used by this operation.</param>
    /// <param name="attachmentStates">The attachment states value used by this operation.</param>
    public BlendStateDescription(RgbaFloat blendFactor, params BlendAttachmentDescription[] attachmentStates) {
        this.BlendFactor = blendFactor;
        this.AttachmentStates = attachmentStates;
        this.AlphaToCoverageEnabled = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlendStateDescription" /> type.
    /// </summary>
    /// <param name="blendFactor">The blend factor value used by this operation.</param>
    /// <param name="alphaToCoverageEnabled">The alpha to coverage enabled value used by this operation.</param>
    /// <param name="attachmentStates">The attachment states value used by this operation.</param>
    public BlendStateDescription(RgbaFloat blendFactor, bool alphaToCoverageEnabled, params BlendAttachmentDescription[] attachmentStates) {
        this.BlendFactor = blendFactor;
        this.AttachmentStates = attachmentStates;
        this.AlphaToCoverageEnabled = alphaToCoverageEnabled;
    }

    /// <summary>
    /// Defines the predefined value exposed by <c>SINGLE_OVERRIDE_BLEND</c>.
    /// </summary>
    public static readonly BlendStateDescription SINGLE_OVERRIDE_BLEND = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.OVERRIDE_BLEND }
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>SINGLE_ALPHA_BLEND</c>.
    /// </summary>
    public static readonly BlendStateDescription SINGLE_ALPHA_BLEND = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.ALPHA_BLEND }
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>SINGLE_ADDITIVE_BLEND</c>.
    /// </summary>
    public static readonly BlendStateDescription SINGLE_ADDITIVE_BLEND = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.ADDITIVE_BLEND }
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>SINGLE_DISABLED</c>.
    /// </summary>
    public static readonly BlendStateDescription SINGLE_DISABLED = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.DISABLED }
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>EMPTY</c>.
    /// </summary>
    public static readonly BlendStateDescription EMPTY = new() {
        AttachmentStates = Array.Empty<BlendAttachmentDescription>()
    };

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(BlendStateDescription other) {
        return this.BlendFactor.Equals(other.BlendFactor)
               && this.AlphaToCoverageEnabled.Equals(other.AlphaToCoverageEnabled)
               && Util.ArrayEqualsEquatable(this.AttachmentStates, other.AttachmentStates);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.BlendFactor.GetHashCode(), this.AlphaToCoverageEnabled.GetHashCode(), HashHelper.Array(this.AttachmentStates));
    }

    /// <summary>
    /// Executes the shallow clone logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal BlendStateDescription ShallowClone() {
        BlendStateDescription result = this;
        result.AttachmentStates = Util.ShallowClone(result.AttachmentStates);
        return result;
    }
}