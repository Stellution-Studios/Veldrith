using System;

namespace Veldrith;

/// <summary>
/// A <see cref="Pipeline" /> component describing the blend behavior for an individual color attachment.
/// </summary>
public struct BlendAttachmentDescription : IEquatable<BlendAttachmentDescription> {

    /// <summary>
    /// Controls whether blending is enabled for the color attachment.
    /// </summary>
    public bool BlendEnabled;

    /// <summary>
    /// Controls which components of the color will be written to the framebuffer.
    /// </summary>
    public ColorWriteMask? ColorWriteMask;

    /// <summary>
    /// Controls the source color's influence on the blend result.
    /// </summary>
    public BlendFactor SourceColorFactor;

    /// <summary>
    /// Controls the destination color's influence on the blend result.
    /// </summary>
    public BlendFactor DestinationColorFactor;

    /// <summary>
    /// Controls the function used to combine the source and destination color factors.
    /// </summary>
    public BlendFunction ColorFunction;

    /// <summary>
    /// Controls the source alpha's influence on the blend result.
    /// </summary>
    public BlendFactor SourceAlphaFactor;

    /// <summary>
    /// Controls the destination alpha's influence on the blend result.
    /// </summary>
    public BlendFactor DestinationAlphaFactor;

    /// <summary>
    /// Controls the function used to combine the source and destination alpha factors.
    /// </summary>
    public BlendFunction AlphaFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlendAttachmentDescription" /> type.
    /// </summary>
    /// <param name="blendEnabled">The blend enabled value used by this operation.</param>
    /// <param name="sourceColorFactor">The source color factor value used by this operation.</param>
    /// <param name="destinationColorFactor">The destination color factor value used by this operation.</param>
    /// <param name="colorFunction">The color function value used by this operation.</param>
    /// <param name="sourceAlphaFactor">The source alpha factor value used by this operation.</param>
    /// <param name="destinationAlphaFactor">The destination alpha factor value used by this operation.</param>
    /// <param name="alphaFunction">The alpha function value used by this operation.</param>
    public BlendAttachmentDescription(bool blendEnabled, BlendFactor sourceColorFactor, BlendFactor destinationColorFactor, BlendFunction colorFunction, BlendFactor sourceAlphaFactor, BlendFactor destinationAlphaFactor, BlendFunction alphaFunction) {
        this.BlendEnabled = blendEnabled;
        this.SourceColorFactor = sourceColorFactor;
        this.DestinationColorFactor = destinationColorFactor;
        this.ColorFunction = colorFunction;
        this.SourceAlphaFactor = sourceAlphaFactor;
        this.DestinationAlphaFactor = destinationAlphaFactor;
        this.AlphaFunction = alphaFunction;
        this.ColorWriteMask = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlendAttachmentDescription" /> type.
    /// </summary>
    /// <param name="blendEnabled">The blend enabled value used by this operation.</param>
    /// <param name="colorWriteMask">The color write mask value used by this operation.</param>
    /// <param name="sourceColorFactor">The source color factor value used by this operation.</param>
    /// <param name="destinationColorFactor">The destination color factor value used by this operation.</param>
    /// <param name="colorFunction">The color function value used by this operation.</param>
    /// <param name="sourceAlphaFactor">The source alpha factor value used by this operation.</param>
    /// <param name="destinationAlphaFactor">The destination alpha factor value used by this operation.</param>
    /// <param name="alphaFunction">The alpha function value used by this operation.</param>
    public BlendAttachmentDescription(bool blendEnabled, ColorWriteMask colorWriteMask, BlendFactor sourceColorFactor, BlendFactor destinationColorFactor, BlendFunction colorFunction, BlendFactor sourceAlphaFactor, BlendFactor destinationAlphaFactor, BlendFunction alphaFunction) {
        this.BlendEnabled = blendEnabled;
        this.ColorWriteMask = colorWriteMask;
        this.SourceColorFactor = sourceColorFactor;
        this.DestinationColorFactor = destinationColorFactor;
        this.ColorFunction = colorFunction;
        this.SourceAlphaFactor = sourceAlphaFactor;
        this.DestinationAlphaFactor = destinationAlphaFactor;
        this.AlphaFunction = alphaFunction;
    }

    /// <summary>
    /// Defines the predefined value exposed by <c>OVERRIDE_BLEND</c>.
    /// </summary>
    public static readonly BlendAttachmentDescription OVERRIDE_BLEND = new() {
        BlendEnabled = true,
        SourceColorFactor = BlendFactor.One,
        DestinationColorFactor = BlendFactor.Zero,
        ColorFunction = BlendFunction.Add,
        SourceAlphaFactor = BlendFactor.One,
        DestinationAlphaFactor = BlendFactor.Zero,
        AlphaFunction = BlendFunction.Add
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>ALPHA_BLEND</c>.
    /// </summary>
    public static readonly BlendAttachmentDescription ALPHA_BLEND = new() {
        BlendEnabled = true,
        SourceColorFactor = BlendFactor.SourceAlpha,
        DestinationColorFactor = BlendFactor.InverseSourceAlpha,
        ColorFunction = BlendFunction.Add,
        SourceAlphaFactor = BlendFactor.SourceAlpha,
        DestinationAlphaFactor = BlendFactor.InverseSourceAlpha,
        AlphaFunction = BlendFunction.Add
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>ADDITIVE_BLEND</c>.
    /// </summary>
    public static readonly BlendAttachmentDescription ADDITIVE_BLEND = new() {
        BlendEnabled = true,
        SourceColorFactor = BlendFactor.SourceAlpha,
        DestinationColorFactor = BlendFactor.One,
        ColorFunction = BlendFunction.Add,
        SourceAlphaFactor = BlendFactor.SourceAlpha,
        DestinationAlphaFactor = BlendFactor.One,
        AlphaFunction = BlendFunction.Add
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>DISABLED</c>.
    /// </summary>
    public static readonly BlendAttachmentDescription DISABLED = new() {
        BlendEnabled = false,
        SourceColorFactor = BlendFactor.One,
        DestinationColorFactor = BlendFactor.Zero,
        ColorFunction = BlendFunction.Add,
        SourceAlphaFactor = BlendFactor.One,
        DestinationAlphaFactor = BlendFactor.Zero,
        AlphaFunction = BlendFunction.Add
    };

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(BlendAttachmentDescription other) {
        return this.BlendEnabled == other.BlendEnabled
               && this.ColorWriteMask == other.ColorWriteMask
               && this.SourceColorFactor == other.SourceColorFactor
               && this.DestinationColorFactor == other.DestinationColorFactor && this.ColorFunction == other.ColorFunction
               && this.SourceAlphaFactor == other.SourceAlphaFactor && this.DestinationAlphaFactor == other.DestinationAlphaFactor
               && this.AlphaFunction == other.AlphaFunction;
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.BlendEnabled.GetHashCode(), this.ColorWriteMask.GetHashCode(), (int)this.SourceColorFactor, (int)this.DestinationColorFactor, (int)this.ColorFunction, (int)this.SourceAlphaFactor, (int)this.DestinationAlphaFactor, (int)this.AlphaFunction);
    }
}