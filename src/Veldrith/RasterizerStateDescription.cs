using System;

namespace Veldrith;

/// <summary>
/// A <see cref="Pipeline" /> component describing the properties of the rasterizer.
/// </summary>
public struct RasterizerStateDescription : IEquatable<RasterizerStateDescription> {

    /// <summary>
    /// Controls which face will be culled.
    /// </summary>
    public FaceCullMode CullMode;

    /// <summary>
    /// Controls how the rasterizer fills polygons.
    /// </summary>
    public PolygonFillMode FillMode;

    /// <summary>
    /// Controls the winding order used to determine the front face of primitives.
    /// </summary>
    public FrontFace FrontFace;

    /// <summary>
    /// Controls whether depth clipping is enabled.
    /// </summary>
    public bool DepthClipEnabled;

    /// <summary>
    /// Controls whether the scissor test is enabled.
    /// </summary>
    public bool ScissorTestEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="RasterizerStateDescription" /> type.
    /// </summary>
    /// <param name="cullMode">The cull mode value used by this operation.</param>
    /// <param name="fillMode">The fill mode value used by this operation.</param>
    /// <param name="frontFace">The front face value used by this operation.</param>
    /// <param name="depthClipEnabled">The depth clip enabled value used by this operation.</param>
    /// <param name="scissorTestEnabled">The scissor test enabled value used by this operation.</param>
    public RasterizerStateDescription(FaceCullMode cullMode, PolygonFillMode fillMode, FrontFace frontFace, bool depthClipEnabled, bool scissorTestEnabled) {
        this.CullMode = cullMode;
        this.FillMode = fillMode;
        this.FrontFace = frontFace;
        this.DepthClipEnabled = depthClipEnabled;
        this.ScissorTestEnabled = scissorTestEnabled;
    }

    /// <summary>
    /// Defines the predefined value exposed by <c>DEFAULT</c>.
    /// </summary>
    public static readonly RasterizerStateDescription DEFAULT = new() {
        CullMode = FaceCullMode.Back,
        FillMode = PolygonFillMode.Solid,
        FrontFace = FrontFace.Clockwise,
        DepthClipEnabled = true,
        ScissorTestEnabled = false
    };

    /// <summary>
    /// Defines the predefined value exposed by <c>CULL_NONE</c>.
    /// </summary>
    public static readonly RasterizerStateDescription CULL_NONE = new() {
        CullMode = FaceCullMode.None,
        FillMode = PolygonFillMode.Solid,
        FrontFace = FrontFace.Clockwise,
        DepthClipEnabled = true,
        ScissorTestEnabled = false
    };

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(RasterizerStateDescription other) {
        return this.CullMode == other.CullMode
               && this.FillMode == other.FillMode
               && this.FrontFace == other.FrontFace
               && this.DepthClipEnabled.Equals(other.DepthClipEnabled)
               && this.ScissorTestEnabled.Equals(other.ScissorTestEnabled);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine((int)this.CullMode, (int)this.FillMode, (int)this.FrontFace, this.DepthClipEnabled.GetHashCode(), this.ScissorTestEnabled.GetHashCode());
    }
}