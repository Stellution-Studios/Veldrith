namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLViewport data structure used by the graphics runtime.
/// </summary>
public struct MTLViewport {

    /// <summary>
    /// Stores the origin x state used by this instance.
    /// </summary>
    public double OriginX;

    /// <summary>
    /// Stores the origin y state used by this instance.
    /// </summary>
    public double OriginY;

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    public double Width;

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    public double Height;

    /// <summary>
    /// Stores the znear state used by this instance.
    /// </summary>
    public double ZNear;

    /// <summary>
    /// Stores the zfar state used by this instance.
    /// </summary>
    public double ZFar;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLViewport" /> type.
    /// </summary>
    /// <param name="originX">The origin x value used by this operation.</param>
    /// <param name="originY">The origin y value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="zNear">The zNear value used by this operation.</param>
    /// <param name="zFar">The zFar value used by this operation.</param>
    public MTLViewport(double originX, double originY, double width, double height, double zNear, double zFar) {
        this.OriginX = originX;
        this.OriginY = originY;
        this.Width = width;
        this.Height = height;
        this.ZNear = zNear;
        this.ZFar = zFar;
    }
}