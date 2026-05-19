using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// A color stored in four 32-bit floating-point values, in RGBA component order.
/// </summary>
public struct RgbaFloat : IEquatable<RgbaFloat> {

    /// <summary>
    /// Stores the value associated with <c>channels</c>.
    /// </summary>
    private readonly Vector4 channels;

    /// <summary>
    /// The red component.
    /// </summary>
    public float R => this.channels.X;

    /// <summary>
    /// The green component.
    /// </summary>
    public float G => this.channels.Y;

    /// <summary>
    /// The blue component.
    /// </summary>
    public float B => this.channels.Z;

    /// <summary>
    /// The alpha component.
    /// </summary>
    public float A => this.channels.W;

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaFloat" /> type.
    /// </summary>
    /// <param name="r">Specifies the value of <paramref name="r" />.</param>
    /// <param name="g">Specifies the value of <paramref name="g" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public RgbaFloat(float r, float g, float b, float a) {
        this.channels = new Vector4(r, g, b, a);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaFloat" /> type.
    /// </summary>
    /// <param name="channels">Specifies the value of <paramref name="channels" />.</param>
    public RgbaFloat(Vector4 channels) {
        this.channels = channels;
    }

    /// <summary>
    /// The total size, in bytes, of an RgbaFloat value.
    /// </summary>
    public static readonly int SIZE_IN_BYTES = 16;

    /// <summary>
    /// Defines the predefined value exposed by <c>RED</c>.
    /// </summary>
    public static readonly RgbaFloat RED = new(1, 0, 0, 1);

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    public static readonly RgbaFloat DARK_RED = new(0.6f, 0, 0, 1);

    /// <summary>
    /// Defines the predefined value exposed by <c>GREEN</c>.
    /// </summary>
    public static readonly RgbaFloat GREEN = new(0, 1, 0, 1);

    /// <summary>
    /// Defines the predefined value exposed by <c>BLUE</c>.
    /// </summary>
    public static readonly RgbaFloat BLUE = new(0, 0, 1, 1);

    /// <summary>
    /// Defines the predefined value exposed by <c>YELLOW</c>.
    /// </summary>
    public static readonly RgbaFloat YELLOW = new(1, 1, 0, 1);

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    public static readonly RgbaFloat GREY = new(.25f, .25f, .25f, 1);

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    public static readonly RgbaFloat LIGHT_GREY = new(.65f, .65f, .65f, 1);

    /// <summary>
    /// Defines the predefined value exposed by <c>CYAN</c>.
    /// </summary>
    public static readonly RgbaFloat CYAN = new(0, 1, 1, 1);

    /// <summary>
    /// Defines the predefined value exposed by <c>WHITE</c>.
    /// </summary>
    public static readonly RgbaFloat WHITE = new(1, 1, 1, 1);

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    public static readonly RgbaFloat CORNFLOWER_BLUE = new(0.3921f, 0.5843f, 0.9294f, 1);

    /// <summary>
    /// Defines the predefined value exposed by <c>CLEAR</c>.
    /// </summary>
    public static readonly RgbaFloat CLEAR = new(0, 0, 0, 0);

    /// <summary>
    /// Defines the predefined value exposed by <c>BLACK</c>.
    /// </summary>
    public static readonly RgbaFloat BLACK = new(0, 0, 0, 1);

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    public static readonly RgbaFloat PINK = new(1f, 0.45f, 0.75f, 1);

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <param name="f">Specifies the value of <paramref name="f" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    public static readonly RgbaFloat ORANGE = new(1f, 0.36f, 0f, 1);

    /// <summary>
    /// Converts this color to a <see cref="Vector4" /> in RGBA component order.
    /// </summary>
    /// <returns>The underlying RGBA vector value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 ToVector4() {
        return this.channels;
    }

    /// <summary>
    /// Compares this color with another <see cref="RgbaFloat" /> value.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns><see langword="true" /> if all RGBA components are equal; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RgbaFloat other) {
        return this.channels.Equals(other.channels);
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="obj">Specifies the value of <paramref name="obj" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public override bool Equals(object obj) {
        return obj is RgbaFloat other && this.Equals(other);
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
    /// Executes the ToString operation.
    /// </summary>
    /// <returns>Returns the result produced by the ToString operation.</returns>
    public override string ToString() {
        return $"R:{this.R}, G:{this.G}, B:{this.B}, A:{this.A}";
    }

    /// <summary>
    /// Compares two <see cref="RgbaFloat" /> values for component-wise equality.
    /// </summary>
    /// <param name="left">Specifies the value of <paramref name="left" />.</param>
    /// <param name="right">Specifies the value of <paramref name="right" />.</param>
    /// <returns><see langword="true" /> if both values are equal; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RgbaFloat left, RgbaFloat right) {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="RgbaFloat" /> values for component-wise inequality.
    /// </summary>
    /// <param name="left">Specifies the value of <paramref name="left" />.</param>
    /// <param name="right">Specifies the value of <paramref name="right" />.</param>
    /// <returns><see langword="true" /> if at least one component differs; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RgbaFloat left, RgbaFloat right) {
        return !left.Equals(right);
    }
}
