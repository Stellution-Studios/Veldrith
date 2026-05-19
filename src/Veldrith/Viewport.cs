using System;

namespace Veldrith;

/// <summary>
/// Describes a 3-dimensional region.
/// </summary>
public struct Viewport : IEquatable<Viewport> {

    /// <summary>
    /// The minimum X value.
    /// </summary>
    public float X;

    /// <summary>
    /// The minimum Y value.
    /// </summary>
    public float Y;

    /// <summary>
    /// The width.
    /// </summary>
    public float Width;

    /// <summary>
    /// The height.
    /// </summary>
    public float Height;

    /// <summary>
    /// The minimum depth.
    /// </summary>
    public float MinDepth;

    /// <summary>
    /// The maximum depth.
    /// </summary>
    public float MaxDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="Viewport" /> type.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="minDepth">Specifies the value of <paramref name="minDepth" />.</param>
    /// <param name="maxDepth">Specifies the value of <paramref name="maxDepth" />.</param>
    public Viewport(float x, float y, float width, float height, float minDepth, float maxDepth) {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
        this.MinDepth = minDepth;
        this.MaxDepth = maxDepth;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(Viewport other) {
        return this.X.Equals(other.X) && this.Y.Equals(other.Y)
                                      && this.Width.Equals(other.Width) && this.Height.Equals(other.Height)
                                      && this.MinDepth.Equals(other.MinDepth) && this.MaxDepth.Equals(other.MaxDepth);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.X.GetHashCode(), this.Y.GetHashCode(), this.Width.GetHashCode(), this.Height.GetHashCode(), this.MinDepth.GetHashCode(), this.MaxDepth.GetHashCode());
    }
}