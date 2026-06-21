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
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="minDepth">The min depth value used by this operation.</param>
    /// <param name="maxDepth">The max depth value used by this operation.</param>
    public Viewport(float x, float y, float width, float height, float minDepth, float maxDepth) {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
        this.MinDepth = minDepth;
        this.MaxDepth = maxDepth;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(Viewport other) {
        return this.X.Equals(other.X) && this.Y.Equals(other.Y)
                                      && this.Width.Equals(other.Width) && this.Height.Equals(other.Height)
                                      && this.MinDepth.Equals(other.MinDepth) && this.MaxDepth.Equals(other.MaxDepth);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.X.GetHashCode(), this.Y.GetHashCode(), this.Width.GetHashCode(), this.Height.GetHashCode(), this.MinDepth.GetHashCode(), this.MaxDepth.GetHashCode());
    }
}