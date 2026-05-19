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
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="minDepth">The value of minDepth.</param>
    /// <param name="maxDepth">The value of maxDepth.</param>
    public Viewport(float x, float y, float width, float height, float minDepth, float maxDepth) {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
        this.MinDepth = minDepth;
        this.MaxDepth = maxDepth;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(Viewport other) {
        return this.X.Equals(other.X) && this.Y.Equals(other.Y)
                                      && this.Width.Equals(other.Width) && this.Height.Equals(other.Height)
                                      && this.MinDepth.Equals(other.MinDepth) && this.MaxDepth.Equals(other.MaxDepth);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.X.GetHashCode(), this.Y.GetHashCode(), this.Width.GetHashCode(), this.Height.GetHashCode(), this.MinDepth.GetHashCode(), this.MaxDepth.GetHashCode());
    }
}