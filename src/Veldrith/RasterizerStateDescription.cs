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
    /// <param name="cullMode">Specifies the value of <paramref name="cullMode" />.</param>
    /// <param name="fillMode">Specifies the value of <paramref name="fillMode" />.</param>
    /// <param name="frontFace">Specifies the value of <paramref name="frontFace" />.</param>
    /// <param name="depthClipEnabled">Specifies the value of <paramref name="depthClipEnabled" />.</param>
    /// <param name="scissorTestEnabled">Specifies the value of <paramref name="scissorTestEnabled" />.</param>
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
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(RasterizerStateDescription other) {
        return this.CullMode == other.CullMode
               && this.FillMode == other.FillMode
               && this.FrontFace == other.FrontFace
               && this.DepthClipEnabled.Equals(other.DepthClipEnabled)
               && this.ScissorTestEnabled.Equals(other.ScissorTestEnabled);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine((int)this.CullMode, (int)this.FillMode, (int)this.FrontFace, this.DepthClipEnabled.GetHashCode(), this.ScissorTestEnabled.GetHashCode());
    }
}
