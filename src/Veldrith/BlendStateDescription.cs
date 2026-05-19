using System;

namespace Veldrith;

/// <summary>
/// A <see cref="Pipeline" /> component describing how values are blended into each individual color target.
/// </summary>
public struct BlendStateDescription : IEquatable<BlendStateDescription> {

    /// <summary>
    /// A constant blend color used in <see cref="BlendFactor.BlendFactor" /> and
    /// <see cref="BlendFactor.InverseBlendFactor" />,
    /// or otherwise ignored.
    /// </summary>
    public RgbaFloat BlendFactor;

    /// <summary>
    /// An array of <see cref="BlendAttachmentDescription" /> describing how blending is performed for each color target
    /// used in the <see cref="Pipeline" />.
    /// </summary>
    public BlendAttachmentDescription[] AttachmentStates;

    /// <summary>
    /// Enables alpha-to-coverage, which causes a fragment's alpha value to be used when determining multi-sample coverage.
    /// </summary>
    public bool AlphaToCoverageEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlendStateDescription" /> type.
    /// </summary>
    /// <param name="blendFactor">The value of blendFactor.</param>
    /// <param name="attachmentStates">The value of attachmentStates.</param>
    public BlendStateDescription(RgbaFloat blendFactor, params BlendAttachmentDescription[] attachmentStates) {
        this.BlendFactor = blendFactor;
        this.AttachmentStates = attachmentStates;
        this.AlphaToCoverageEnabled = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlendStateDescription" /> type.
    /// </summary>
    /// <param name="blendFactor">The value of blendFactor.</param>
    /// <param name="alphaToCoverageEnabled">The value of alphaToCoverageEnabled.</param>
    /// <param name="attachmentStates">The value of attachmentStates.</param>
    public BlendStateDescription(RgbaFloat blendFactor, bool alphaToCoverageEnabled, params BlendAttachmentDescription[] attachmentStates) {
        this.BlendFactor = blendFactor;
        this.AttachmentStates = attachmentStates;
        this.AlphaToCoverageEnabled = alphaToCoverageEnabled;
    }

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly BlendStateDescription SINGLE_OVERRIDE_BLEND = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.OVERRIDE_BLEND }
    };

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly BlendStateDescription SINGLE_ALPHA_BLEND = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.ALPHA_BLEND }
    };

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly BlendStateDescription SINGLE_ADDITIVE_BLEND = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.ADDITIVE_BLEND }
    };

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly BlendStateDescription SINGLE_DISABLED = new() {
        AttachmentStates = new[] { BlendAttachmentDescription.DISABLED }
    };

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly BlendStateDescription EMPTY = new() {
        AttachmentStates = Array.Empty<BlendAttachmentDescription>()
    };

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(BlendStateDescription other) {
        return this.BlendFactor.Equals(other.BlendFactor)
               && this.AlphaToCoverageEnabled.Equals(other.AlphaToCoverageEnabled)
               && Util.ArrayEqualsEquatable(this.AttachmentStates, other.AttachmentStates);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.BlendFactor.GetHashCode(), this.AlphaToCoverageEnabled.GetHashCode(), HashHelper.Array(this.AttachmentStates));
    }

    /// <summary>
    /// Performs the ShallowClone operation.
    /// </summary>
    /// <returns>The result of the ShallowClone operation.</returns>
    internal BlendStateDescription ShallowClone() {
        BlendStateDescription result = this;
        result.AttachmentStates = Util.ShallowClone(result.AttachmentStates);
        return result;
    }
}