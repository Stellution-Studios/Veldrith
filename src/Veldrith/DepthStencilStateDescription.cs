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
    /// Initializes a new instance of the <see cref="DepthStencilStateDescription" /> type.
    /// </summary>
    /// <param name="depthTestEnabled">Specifies the value of <paramref name="depthTestEnabled" />.</param>
    /// <param name="depthWriteEnabled">Specifies the value of <paramref name="depthWriteEnabled" />.</param>
    /// <param name="comparisonKind">Specifies the value of <paramref name="comparisonKind" />.</param>
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
    /// Initializes a new instance of the <see cref="DepthStencilStateDescription" /> type.
    /// </summary>
    /// <param name="depthTestEnabled">Specifies the value of <paramref name="depthTestEnabled" />.</param>
    /// <param name="depthWriteEnabled">Specifies the value of <paramref name="depthWriteEnabled" />.</param>
    /// <param name="comparisonKind">Specifies the value of <paramref name="comparisonKind" />.</param>
    /// <param name="stencilTestEnabled">Specifies the value of <paramref name="stencilTestEnabled" />.</param>
    /// <param name="stencilFront">Specifies the value of <paramref name="stencilFront" />.</param>
    /// <param name="stencilBack">Specifies the value of <paramref name="stencilBack" />.</param>
    /// <param name="stencilReadMask">Specifies the value of <paramref name="stencilReadMask" />.</param>
    /// <param name="stencilWriteMask">Specifies the value of <paramref name="stencilWriteMask" />.</param>
    /// <param name="stencilReference">Specifies the value of <paramref name="stencilReference" />.</param>
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
    /// Defines the predefined value exposed by <c>DEPTH_ONLY_LESS_EQUAL</c>.
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_LESS_EQUAL = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = true,
        DepthComparison = ComparisonKind.LessEqual
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>DEPTH_ONLY_LESS_EQUAL_READ</c>.
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_LESS_EQUAL_READ = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = false,
        DepthComparison = ComparisonKind.LessEqual
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>DEPTH_ONLY_GREATER_EQUAL</c>.
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_GREATER_EQUAL = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = true,
        DepthComparison = ComparisonKind.GreaterEqual
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>DEPTH_ONLY_GREATER_EQUAL_READ</c>.
    /// </summary>
    public static readonly DepthStencilStateDescription DEPTH_ONLY_GREATER_EQUAL_READ = new() {
        DepthTestEnabled = true,
        DepthWriteEnabled = false,
        DepthComparison = ComparisonKind.GreaterEqual
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>DISABLED</c>.
    /// </summary>
    public static readonly DepthStencilStateDescription DISABLED = new() {
        DepthTestEnabled = false,
        DepthWriteEnabled = false,
        DepthComparison = ComparisonKind.LessEqual
    };

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
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
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.DepthTestEnabled.GetHashCode(), this.DepthWriteEnabled.GetHashCode(), (int)this.DepthComparison, this.StencilTestEnabled.GetHashCode(), this.StencilFront.GetHashCode(), this.StencilBack.GetHashCode(), this.StencilReadMask.GetHashCode(), this.StencilWriteMask.GetHashCode(), this.StencilReference.GetHashCode());
    }
}
