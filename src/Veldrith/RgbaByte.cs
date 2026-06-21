using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// A color stored in four 8-bit unsigned normalized integer values, in RGBA component order.
/// </summary>
public struct RgbaByte : IEquatable<RgbaByte> {

    /// <summary>
    /// The Red component.
    /// </summary>
    public readonly byte R;

    /// <summary>
    /// The Green component.
    /// </summary>
    public readonly byte G;

    /// <summary>
    /// The Blue component.
    /// </summary>
    public readonly byte B;

    /// <summary>
    /// The Alpha component.
    /// </summary>
    public readonly byte A;

    /// <summary>
    /// Defines the predefined value exposed by <c>RED</c>.
    /// </summary>
    public static readonly RgbaByte RED = new(255, 0, 0, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>DARK_RED</c>.
    /// </summary>
    public static readonly RgbaByte DARK_RED = new(153, 0, 0, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>GREEN</c>.
    /// </summary>
    public static readonly RgbaByte GREEN = new(0, 255, 0, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>BLUE</c>.
    /// </summary>
    public static readonly RgbaByte BLUE = new(0, 0, 255, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>YELLOW</c>.
    /// </summary>
    public static readonly RgbaByte YELLOW = new(255, 255, 0, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>GREY</c>.
    /// </summary>
    public static readonly RgbaByte GREY = new(64, 64, 64, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>LIGHT_GREY</c>.
    /// </summary>
    public static readonly RgbaByte LIGHT_GREY = new(166, 166, 166, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>CYAN</c>.
    /// </summary>
    public static readonly RgbaByte CYAN = new(0, 255, 255, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>WHITE</c>.
    /// </summary>
    public static readonly RgbaByte WHITE = new(255, 255, 255, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>CORNFLOWER_BLUE</c>.
    /// </summary>
    public static readonly RgbaByte CORNFLOWER_BLUE = new(100, 149, 237, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>CLEAR</c>.
    /// </summary>
    public static readonly RgbaByte CLEAR = new(0, 0, 0, 0);

    /// <summary>
    /// Defines the predefined value exposed by <c>BLACK</c>.
    /// </summary>
    public static readonly RgbaByte BLACK = new(0, 0, 0, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>PINK</c>.
    /// </summary>
    public static readonly RgbaByte PINK = new(255, 155, 191, 255);

    /// <summary>
    /// Defines the predefined value exposed by <c>ORANGE</c>.
    /// </summary>
    public static readonly RgbaByte ORANGE = new(255, 92, 0, 255);

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaByte" /> type.
    /// </summary>
    /// <param name="r">The r value used by this operation.</param>
    /// <param name="g">The g value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    public RgbaByte(byte r, byte g, byte b, byte a) {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
    }

    /// <summary>
    /// Compares this color with another <see cref="RgbaByte" /> value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RgbaByte other) {
        return this.R.Equals(other.R) && this.G.Equals(other.G) && this.B.Equals(other.B) && this.A.Equals(other.A);
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="obj">The object instance to evaluate.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object obj) {
        return obj is RgbaByte other && this.Equals(other);
    }

    /// <summary>
    /// Computes a hash code for this color value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() {
        return HashHelper.Combine(this.R.GetHashCode(), this.G.GetHashCode(), this.B.GetHashCode(), this.A.GetHashCode());
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return $"R:{this.R}, G:{this.G}, B:{this.B}, A:{this.A}";
    }

    /// <summary>
    /// Compares two <see cref="RgbaByte" /> values for component-wise equality.
    /// </summary>
    /// <param name="left">The left operand of the operation.</param>
    /// <param name="right">The right operand of the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RgbaByte left, RgbaByte right) {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="RgbaByte" /> values for component-wise inequality.
    /// </summary>
    /// <param name="left">The left operand of the operation.</param>
    /// <param name="right">The right operand of the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RgbaByte left, RgbaByte right) {
        return !left.Equals(right);
    }
}