using System;

namespace Veldrith;

/// <summary>
/// A <see cref="Pipeline" /> component describing the properties of the depth stencil state.
/// </summary>
public struct DepthStencilStateDescription : IEquatable<DepthStencilStateDescription> {

    /// <summary>
    /// Controls whether depth testing is enabled.
    /// </summary>
    public bool DepthTestEnabled;

    /// <summary>
    /// Controls whether new depth values are written to the depth buffer.
    /// </summary>
    public bool DepthWriteEnabled;

    /// <summary>
    /// The <see cref="ComparisonKind" /> used when considering new depth values.
    /// </summary>
    public ComparisonKind DepthComparison;

    /// <summary>
    /// Controls whether the stencil test is enabled.
    /// </summary>
    public bool StencilTestEnabled;

    /// <summary>
    /// Controls how stencil tests are handled for pixels whose surface faces towards the camera.
    /// </summary>
    public StencilBehaviorDescription StencilFront;

    /// <summary>
    /// Controls how stencil tests are handled for pixels whose surface faces away from the camera.
    /// </summary>
    public StencilBehaviorDescription StencilBack;

    /// <summary>
    /// Controls the portion of the stencil buffer used for reading.
    /// </summary>
    public byte StencilReadMask;

    /// <summary>
    /// Controls the portion of the stencil buffer used for writing.
    /// </summary>
    public byte StencilWriteMask;

    /// <summary>
    /// The reference value to use when doing a stencil test.
    /// </summary>
    public uint StencilReference;

    /// <summary>
    /// Constructs a new <see cref="DepthStencilStateDescription" />. This describes a depth-stencil state with no stencil
    /// testing enabled.
    /// </summary>
    /// <param name="depthTestEnabled">Controls whether depth testing is enabled.</param>
    /// <param name="depthWriteEnabled">Controls whether new depth values are written to the depth buffer.</param>
    /// <param name="comparisonKind">The <see cref="ComparisonKind" /> used when considering new depth values.</param>
    public DepthStencilStateDescription(bool depthTestEnabled, bool depthWriteEnabled, ComparisonKind comparisonKind) {
        this.DepthTestEnabled = depthTestEnabled;
        this.DepthWriteEnabled = depthWriteEnabled;
        this.DepthComparison = comparisonKind;

        this.StencilTestEnabled = false;
        this.StencilFront = default;
        this.StencilBack = default;
        this.StencilReadMask = 0;
        this.StencilWriteMask = 0;
        this.StencilReference = 0;
    }

    /// <summary>
    /// Constructs a new <see cref="DepthStencilStateDescription" />. This describes a depth-stencil state with no stencil
    /// testing enabled.
    /// </summary>
    /// <param name="depthTestEnabled">Controls whether depth testing is enabled.</param>
    /// <param name="depthWriteEnabled">Controls whether new depth values are written to the depth buffer.</param>
    /// <param name="comparisonKind">The <see cref="ComparisonKind" /> used when considering new depth values.</param>
    /// <param name="stencilTestEnabled">Controls whether the stencil test is enabled.</param>
    /// <param name="stencilFront">Controls how stencil tests are handled for pixels whose surface faces towards the camera.</param>
    /// <param name="stencilBack">Controls how stencil tests are handled for pixels whose surface faces away from the camera.</param>
    /// <param name="stencilReadMask">Controls the portion of the stencil buffer used for reading.</param>
    /// <param name="stencilWriteMask">Controls the portion of the stencil buffer used for writing.</param>
    /// <param name="stencilReference">The reference value to use when doing a stencil test.</param>
    public DepthStencilStateDescription(bool depthTestEnabled, bool depthWriteEnabled, ComparisonKind comparisonKind, bool stencilTestEnabled, StencilBehaviorDescription stencilFront, StencilBehaviorDescription stencilBack, byte stencilReadMask, byte stencilWriteMask, uint stencilReference) {
        this.DepthTestEnabled = depthTestEnabled;
        this.DepthWriteEnabled = depthWriteEnabled;
        this.DepthComparison = comparisonKind;

        this.StencilTestEnabled = stencilTestEnabled;
        this.StencilFront = stencilFront;
        this.StencilBack = stencilBack;
        this.StencilReadMask = stencilReadMask;
        this.StencilWriteMask = stencilWriteMask;
        this.StencilReference = stencilReference;
    }

    /// <summary>
    /// Describes a depth-only depth stencil state which uses a <see cref="ComparisonKind.LessEqual" /> comparison.
    /// The stencil test is disabled.
    /// Settings:
    /// DepthTestEnabled = true
    /// DepthWriteEnabled = true
    /// ComparisonKind = DepthComparisonKind.LessEqual
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_LESS_EQUAL = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = true,
        DepthComparison = ComparisonKind.LessEqual
    };

    /// <summary>
    /// Describes a depth-only depth stencil state which uses a <see cref="ComparisonKind.LessEqual" /> comparison, and
    /// disables writing to the depth buffer.
    /// The stencil test is disabled.
    /// Settings:
    /// DepthTestEnabled = true
    /// DepthWriteEnabled = false
    /// ComparisonKind = DepthComparisonKind.LessEqual
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_LESS_EQUAL_READ = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = false,
        DepthComparison = ComparisonKind.LessEqual
    };

    /// <summary>
    /// Describes a depth-only depth stencil state which uses a <see cref="ComparisonKind.GreaterEqual" /> comparison.
    /// The stencil test is disabled.
    /// Settings:
    /// DepthTestEnabled = true
    /// DepthWriteEnabled = true
    /// ComparisonKind = DepthComparisonKind.GreaterEqual
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_GREATER_EQUAL = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = true,
        DepthComparison = ComparisonKind.GreaterEqual
    };

    /// <summary>
    /// Describes a depth-only depth stencil state which uses a <see cref="ComparisonKind.GreaterEqual" /> comparison, and
    /// disables writing to the depth buffer. The stencil test is disabled.
    /// Settings:
    /// DepthTestEnabled = true
    /// DepthWriteEnabled = false
    /// ComparisonKind = DepthComparisonKind.GreaterEqual
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_GREATER_EQUAL_READ = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = false,
        DepthComparison = ComparisonKind.GreaterEqual
    };

    /// <summary>
    /// Describes a depth-only depth stencil state in which depth testing and writing is disabled.
    /// The stencil test is disabled.
    /// Settings:
    /// DepthTestEnabled = false
    /// DepthWriteEnabled = false
    /// ComparisonKind = DepthComparisonKind.LessEqual
    /// </summary>
    public static readonly DepthStencilStateDescription DISABLED = new() {
        DepthTestEnabled = false,
        DepthWriteEnabled = false,
        DepthComparison = ComparisonKind.LessEqual
    };

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements are equal; false otherswise.</returns>
    public bool Equals(DepthStencilStateDescription other) {
        return this.DepthTestEnabled.Equals(other.DepthTestEnabled)
               && this.DepthWriteEnabled.Equals(other.DepthWriteEnabled)
               && this.DepthComparison == other.DepthComparison
               && this.StencilTestEnabled.Equals(other.StencilTestEnabled)
               && this.StencilFront.Equals(other.StencilFront)
               && this.StencilBack.Equals(other.StencilBack)
               && this.StencilReadMask.Equals(other.StencilReadMask)
               && this.StencilWriteMask.Equals(other.StencilWriteMask)
               && this.StencilReference.Equals(other.StencilReference);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.DepthTestEnabled.GetHashCode(), this.DepthWriteEnabled.GetHashCode(), (int)this.DepthComparison, this.StencilTestEnabled.GetHashCode(), this.StencilFront.GetHashCode(), this.StencilBack.GetHashCode(), this.StencilReadMask.GetHashCode(), this.StencilWriteMask.GetHashCode(), this.StencilReference.GetHashCode());
    }
}