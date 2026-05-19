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
    /// Red (255, 0, 0, 255)
    /// </summary>
    public static readonly RgbaByte RED = new(255, 0, 0, 255);

    /// <summary>
    /// Dark Red (153, 0, 0, 255)
    /// </summary>
    public static readonly RgbaByte DARK_RED = new(153, 0, 0, 255);

    /// <summary>
    /// Green (0, 255, 0, 255)
    /// </summary>
    public static readonly RgbaByte GREEN = new(0, 255, 0, 255);

    /// <summary>
    /// Blue (0, 0, 255, 255)
    /// </summary>
    public static readonly RgbaByte BLUE = new(0, 0, 255, 255);

    /// <summary>
    /// Yellow (255, 255, 0, 255)
    /// </summary>
    public static readonly RgbaByte YELLOW = new(255, 255, 0, 255);

    /// <summary>
    /// Grey (64, 64, 64, 255)
    /// </summary>
    public static readonly RgbaByte GREY = new(64, 64, 64, 255);

    /// <summary>
    /// Light Grey (166, 166, 166, 255)
    /// </summary>
    public static readonly RgbaByte LIGHT_GREY = new(166, 166, 166, 255);

    /// <summary>
    /// Cyan (0, 255, 255, 255)
    /// </summary>
    public static readonly RgbaByte CYAN = new(0, 255, 255, 255);

    /// <summary>
    /// White (255, 255, 255, 255)
    /// </summary>
    public static readonly RgbaByte WHITE = new(255, 255, 255, 255);

    /// <summary>
    /// Cornflower Blue (100, 149, 237, 255)
    /// </summary>
    public static readonly RgbaByte CORNFLOWER_BLUE = new(100, 149, 237, 255);

    /// <summary>
    /// Clear (0, 0, 0, 0)
    /// </summary>
    public static readonly RgbaByte CLEAR = new(0, 0, 0, 0);

    /// <summary>
    /// Black (0, 0, 0, 255)
    /// </summary>
    public static readonly RgbaByte BLACK = new(0, 0, 0, 255);

    /// <summary>
    /// Pink (255, 155, 191, 255)
    /// </summary>
    public static readonly RgbaByte PINK = new(255, 155, 191, 255);

    /// <summary>
    /// Orange (255, 92, 0, 255)
    /// </summary>
    public static readonly RgbaByte ORANGE = new(255, 92, 0, 255);

    /// <summary>
    /// Constructs a new RgbaByte from the given components.
    /// </summary>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="a">The alpha component.</param>
    public RgbaByte(byte r, byte g, byte b, byte a) {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
    }

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements are equal; false otherswise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RgbaByte other) {
        return this.R.Equals(other.R) && this.G.Equals(other.G) && this.B.Equals(other.B) && this.A.Equals(other.A);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object obj) {
        return obj is RgbaByte other && this.Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() {
        return HashHelper.Combine(this.R.GetHashCode(), this.G.GetHashCode(), this.B.GetHashCode(), this.A.GetHashCode());
    }

    /// <summary>
    /// Returns a string representation of this color.
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return $"R:{this.R}, G:{this.G}, B:{this.B}, A:{this.A}";
    }

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RgbaByte left, RgbaByte right) {
        return left.Equals(right);
    }

    /// <summary>
    /// Element-wise inequality.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RgbaByte left, RgbaByte right) {
        return !left.Equals(right);
    }
}