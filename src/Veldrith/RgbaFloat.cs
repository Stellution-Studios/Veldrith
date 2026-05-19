using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// A color stored in four 32-bit floating-point values, in RGBA component order.
/// </summary>
public struct RgbaFloat : IEquatable<RgbaFloat> {

    /// <summary>
    /// Represents the channels field.
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
    /// <param name="r">The value of r.</param>
    /// <param name="g">The value of g.</param>
    /// <param name="b">The value of b.</param>
    /// <param name="a">The value of a.</param>
    public RgbaFloat(float r, float g, float b, float a) {
        this.channels = new Vector4(r, g, b, a);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaFloat" /> type.
    /// </summary>
    /// <param name="channels">The value of channels.</param>
    public RgbaFloat(Vector4 channels) {
        this.channels = channels;
    }

    /// <summary>
    /// The total size, in bytes, of an RgbaFloat value.
    /// </summary>
    public static readonly int SIZE_IN_BYTES = 16;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat RED = new(1, 0, 0, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="f">The value of f.</param>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat DARK_RED = new(0.6f, 0, 0, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat GREEN = new(0, 1, 0, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat BLUE = new(0, 0, 1, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat YELLOW = new(1, 1, 0, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat GREY = new(.25f, .25f, .25f, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat LIGHT_GREY = new(.65f, .65f, .65f, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat CYAN = new(0, 1, 1, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat WHITE = new(1, 1, 1, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat CORNFLOWER_BLUE = new(0.3921f, 0.5843f, 0.9294f, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat CLEAR = new(0, 0, 0, 0);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat BLACK = new(0, 0, 0, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <returns>The result of the new operation.</returns>
    public static readonly RgbaFloat PINK = new(1f, 0.45f, 0.75f, 1);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <param name="f">The value of f.</param>
    /// <returns>The result of the new operation.</returns>
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
    /// <param name="other">The color to compare against.</param>
    /// <returns><see langword="true" /> if all RGBA components are equal; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RgbaFloat other) {
        return this.channels.Equals(other.channels);
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="obj">The value of obj.</param>
    /// <returns>The result of the Equals operation.</returns>
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
    /// Performs the ToString operation.
    /// </summary>
    /// <returns>The result of the ToString operation.</returns>
    public override string ToString() {
        return $"R:{this.R}, G:{this.G}, B:{this.B}, A:{this.A}";
    }

    /// <summary>
    /// Compares two <see cref="RgbaFloat" /> values for component-wise equality.
    /// </summary>
    /// <param name="left">The first color value.</param>
    /// <param name="right">The second color value.</param>
    /// <returns><see langword="true" /> if both values are equal; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RgbaFloat left, RgbaFloat right) {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="RgbaFloat" /> values for component-wise inequality.
    /// </summary>
    /// <param name="left">The first color value.</param>
    /// <param name="right">The second color value.</param>
    /// <returns><see langword="true" /> if at least one component differs; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RgbaFloat left, RgbaFloat right) {
        return !left.Equals(right);
    }
}