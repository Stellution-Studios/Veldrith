namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLViewport data structure used by the graphics runtime.
/// </summary>
public struct MTLViewport {

    /// <summary>
    /// Stores the origin x state used by this instance.
    /// </summary>
    public double originX;

    /// <summary>
    /// Stores the origin y state used by this instance.
    /// </summary>
    public double originY;

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    public double width;

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    public double height;

    /// <summary>
    /// Stores the znear state used by this instance.
    /// </summary>
    public double znear;

    /// <summary>
    /// Stores the zfar state used by this instance.
    /// </summary>
    public double zfar;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLViewport" /> type.
    /// </summary>
    /// <param name="originX">The origin x value used by this operation.</param>
    /// <param name="originY">The origin y value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="znear">The znear value used by this operation.</param>
    /// <param name="zfar">The zfar value used by this operation.</param>
    public MTLViewport(double originX, double originY, double width, double height, double znear, double zfar) {
        this.originX = originX;
        this.originY = originY;
        this.width = width;
        this.height = height;
        this.znear = znear;
        this.zfar = zfar;
    }
}