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
    /// If <c>null</c>, the mask will be set to <see cref="Veldrith.ColorWriteMask.All" />.
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
    /// Constructs a new <see cref="BlendAttachmentDescription" />.
    /// </summary>
    /// <param name="blendEnabled">Controls whether blending is enabled for the color attachment.</param>
    /// <param name="sourceColorFactor">Controls the source color's influence on the blend result.</param>
    /// <param name="destinationColorFactor">Controls the destination color's influence on the blend result.</param>
    /// <param name="colorFunction">Controls the function used to combine the source and destination color factors.</param>
    /// <param name="sourceAlphaFactor">Controls the source alpha's influence on the blend result.</param>
    /// <param name="destinationAlphaFactor">Controls the destination alpha's influence on the blend result.</param>
    /// <param name="alphaFunction">Controls the function used to combine the source and destination alpha factors.</param>
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
    /// Constructs a new <see cref="BlendAttachmentDescription" />.
    /// </summary>
    /// <param name="blendEnabled">Controls whether blending is enabled for the color attachment.</param>
    /// <param name="colorWriteMask">Controls which components of the color will be written to the framebuffer.</param>
    /// <param name="sourceColorFactor">Controls the source color's influence on the blend result.</param>
    /// <param name="destinationColorFactor">Controls the destination color's influence on the blend result.</param>
    /// <param name="colorFunction">Controls the function used to combine the source and destination color factors.</param>
    /// <param name="sourceAlphaFactor">Controls the source alpha's influence on the blend result.</param>
    /// <param name="destinationAlphaFactor">Controls the destination alpha's influence on the blend result.</param>
    /// <param name="alphaFunction">Controls the function used to combine the source and destination alpha factors.</param>
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
    /// Describes a blend attachment state in which the source completely overrides the destination.
    /// Settings:
    /// BlendEnabled = true
    /// ColorWriteMask = null
    /// SourceColorFactor = BlendFactor.One
    /// DestinationColorFactor = BlendFactor.Zero
    /// ColorFunction = BlendFunction.Add
    /// SourceAlphaFactor = BlendFactor.One
    /// DestinationAlphaFactor = BlendFactor.Zero
    /// AlphaFunction = BlendFunction.Add
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
    /// Describes a blend attachment state in which the source and destination are blended in an inverse relationship.
    /// Settings:
    /// BlendEnabled = true
    /// ColorWriteMask = null
    /// SourceColorFactor = BlendFactor.SourceAlpha
    /// DestinationColorFactor = BlendFactor.InverseSourceAlpha
    /// ColorFunction = BlendFunction.Add
    /// SourceAlphaFactor = BlendFactor.SourceAlpha
    /// DestinationAlphaFactor = BlendFactor.InverseSourceAlpha
    /// AlphaFunction = BlendFunction.Add
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
    /// Describes a blend attachment state in which the source is added to the destination based on its alpha channel.
    /// Settings:
    /// BlendEnabled = true
    /// ColorWriteMask = null
    /// SourceColorFactor = BlendFactor.SourceAlpha
    /// DestinationColorFactor = BlendFactor.One
    /// ColorFunction = BlendFunction.Add
    /// SourceAlphaFactor = BlendFactor.SourceAlpha
    /// DestinationAlphaFactor = BlendFactor.One
    /// AlphaFunction = BlendFunction.Add
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
    /// Describes a blend attachment state in which blending is disabled.
    /// Settings:
    /// BlendEnabled = false
    /// ColorWriteMask = null
    /// SourceColorFactor = BlendFactor.One
    /// DestinationColorFactor = BlendFactor.Zero
    /// ColorFunction = BlendFunction.Add
    /// SourceAlphaFactor = BlendFactor.One
    /// DestinationAlphaFactor = BlendFactor.Zero
    /// AlphaFunction = BlendFunction.Add
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
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements and all array elements are equal; false otherswise.</returns>
    public bool Equals(BlendAttachmentDescription other) {
        return this.BlendEnabled == other.BlendEnabled
               && this.ColorWriteMask == other.ColorWriteMask
               && this.SourceColorFactor == other.SourceColorFactor
               && this.DestinationColorFactor == other.DestinationColorFactor && this.ColorFunction == other.ColorFunction
               && this.SourceAlphaFactor == other.SourceAlphaFactor && this.DestinationAlphaFactor == other.DestinationAlphaFactor
               && this.AlphaFunction == other.AlphaFunction;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.BlendEnabled.GetHashCode(), this.ColorWriteMask.GetHashCode(), (int)this.SourceColorFactor, (int)this.DestinationColorFactor, (int)this.ColorFunction, (int)this.SourceAlphaFactor, (int)this.DestinationAlphaFactor, (int)this.AlphaFunction);
    }
}