using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// A color stored in four 8-bit unsigned normalized integer values, in RGBA component order.
/// </summary>
public struct RgbaByte : IEquatable<RgbaByte> {

    /// <summary>
    /// The red component.
    /// </summary>
    public readonly byte R;

    /// <summary>
    /// The green component.
    /// </summary>
    public readonly byte G;

    /// <summary>
    /// The blue component.
    /// </summary>
    public readonly byte B;

    /// <summary>
    /// The alpha component.
    /// </summary>
    public readonly byte A;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte RED = new(255, 0, 0, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte DARK_RED = new(153, 0, 0, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte GREEN = new(0, 255, 0, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte BLUE = new(0, 0, 255, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte YELLOW = new(255, 255, 0, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte GREY = new(64, 64, 64, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte LIGHT_GREY = new(166, 166, 166, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte CYAN = new(0, 255, 255, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte WHITE = new(255, 255, 255, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte CORNFLOWER_BLUE = new(100, 149, 237, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte CLEAR = new(0, 0, 0, 0);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte BLACK = new(0, 0, 0, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte PINK = new(255, 155, 191, 255);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaByte ORANGE = new(255, 92, 0, 255);

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaByte" /> type.
    /// </summary>
    /// <param name="r">The value of r.</param>
    /// <param name="g">The value of g.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="a">The value of a.</param>
    public RgbaByte(byte r, byte g, byte b, byte a) {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
    }

    /// <summary>
    /// Compares this color with another <see cref="RgbaByte" /> value.
    /// </summary>
    /// <param name="other">The color to compare against.</param>
    /// <returns><see langword="true" /> if all RGBA components are equal; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RgbaByte other) {
        return this.R.Equals(other.R) && this.G.Equals(other.G) && this.B.Equals(other.B) && this.A.Equals(other.A);
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="obj">The value of obj.</param>
    /// <returns>The result of the Equals operation.</returns>
    public override bool Equals(object obj) {
        return obj is RgbaByte other && this.Equals(other);
    }

    /// <summary>
    /// Computes a hash code for this color value.
    /// </summary>
    /// <returns>A hash code that combines all RGBA components.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() {
        return HashHelper.Combine(this.R.GetHashCode(), this.G.GetHashCode(), this.B.GetHashCode(), this.A.GetHashCode());
    }

    /// <summary>
    /// Performs the ToString operation.
    /// </summary>
    /// <returns>The result of the ToString operation.</returns>
    public override string ToString() {
        return $"R:{this.R}, G:{this.G}, B:{this.B}, A:{this.A}";
    }

    /// <summary>
    /// Compares two <see cref="RgbaByte" /> values for component-wise equality.
    /// </summary>
    /// <param name="left">The first color value.</param>
    /// <param name="right">The second color value.</param>
    /// <returns><see langword="true" /> if both values are equal; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RgbaByte left, RgbaByte right) {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="RgbaByte" /> values for component-wise inequality.
    /// </summary>
    /// <param name="left">The first color value.</param>
    /// <param name="right">The second color value.</param>
    /// <returns><see langword="true" /> if at least one component differs; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RgbaByte left, RgbaByte right) {
        return !left.Equals(right);
    }
}