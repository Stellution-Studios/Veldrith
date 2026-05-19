namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLViewport struct.
/// </summary>
public struct MTLViewport {

    /// <summary>
    /// Represents the originX field.
    /// </summary>
    public double originX;

    /// <summary>
    /// Represents the originY field.
    /// </summary>
    public double originY;

    /// <summary>
    /// Represents the width field.
    /// </summary>
    public double width;

    /// <summary>
    /// Represents the height field.
    /// </summary>
    public double height;

    /// <summary>
    /// Represents the znear field.
    /// </summary>
    public double znear;

    /// <summary>
    /// Represents the zfar field.
    /// </summary>
    public double zfar;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLViewport" /> type.
    /// </summary>
    /// <param name="originX">The value of originX.</param>
    /// <param name="originY">The value of originY.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="znear">The value of znear.</param>
    /// <param name="zfar">The value of zfar.</param>
    public MTLViewport(double originX, double originY, double width, double height, double znear, double zfar) {
        this.originX = originX;
        this.originY = originY;
        this.width = width;
        this.height = height;
        this.znear = znear;
        this.zfar = zfar;
    }
}