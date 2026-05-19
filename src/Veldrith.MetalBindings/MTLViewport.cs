namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLViewport struct.
/// </summary>
public struct MTLViewport {

    /// <summary>
    /// Stores the value associated with <c>originX</c>.
    /// </summary>
    public double originX;

    /// <summary>
    /// Stores the value associated with <c>originY</c>.
    /// </summary>
    public double originY;

    /// <summary>
    /// Stores the value associated with <c>width</c>.
    /// </summary>
    public double width;

    /// <summary>
    /// Stores the value associated with <c>height</c>.
    /// </summary>
    public double height;

    /// <summary>
    /// Stores the value associated with <c>znear</c>.
    /// </summary>
    public double znear;

    /// <summary>
    /// Stores the value associated with <c>zfar</c>.
    /// </summary>
    public double zfar;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLViewport" /> type.
    /// </summary>
    /// <param name="originX">Specifies the value of <paramref name="originX" />.</param>
    /// <param name="originY">Specifies the value of <paramref name="originY" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="znear">Specifies the value of <paramref name="znear" />.</param>
    /// <param name="zfar">Specifies the value of <paramref name="zfar" />.</param>
    public MTLViewport(double originX, double originY, double width, double height, double znear, double zfar) {
        this.originX = originX;
        this.originY = originY;
        this.width = width;
        this.height = height;
        this.znear = znear;
        this.zfar = zfar;
    }
}