using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Describes a single shader specialization constant. Used to substitute new values into Shaders when constructing a
/// <see cref="Pipeline" />.
/// </summary>
public struct SpecializationConstant : IEquatable<SpecializationConstant> {

    /// <summary>
    /// The constant variable ID, as defined in the <see cref="Shader" />.
    /// </summary>
    public uint ID;

    /// <summary>
    /// The type of data stored in this instance. Must be a scalar numeric type.
    /// </summary>
    public ShaderConstantType Type;

    /// <summary>
    /// An 8-byte block storing the contents of the specialization value. This is treated as an untyped buffer and is
    /// interepreted according to <see cref="Type" />.
    /// </summary>
    public ulong Data;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="type">Specifies the value of <paramref name="type" />.</param>
    /// <param name="data">Specifies the value of <paramref name="data" />.</param>
    public SpecializationConstant(uint id, ShaderConstantType type, ulong data) {
        this.ID = id;
        this.Type = type;
        this.Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, bool value)
        : this(id, ShaderConstantType.Bool, Store(value ? (byte)1u : (byte)0u)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, ushort value)
        : this(id, ShaderConstantType.UInt16, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, short value)
        : this(id, ShaderConstantType.Int16, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, uint value)
        : this(id, ShaderConstantType.UInt32, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, int value)
        : this(id, ShaderConstantType.Int32, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, ulong value)
        : this(id, ShaderConstantType.UInt64, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, long value)
        : this(id, ShaderConstantType.Int64, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, float value)
        : this(id, ShaderConstantType.Float, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public SpecializationConstant(uint id, double value)
        : this(id, ShaderConstantType.Double, Store(value)) { }

    /// <summary>
    /// Packs a specialization constant value into a 64-bit storage slot.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    /// <typeparam name="T">The value type to pack.</typeparam>
    /// <returns>The packed 64-bit representation.</returns>
    internal static unsafe ulong Store<T>(T value) {
        ulong ret;
        Unsafe.Write(&ret, value);
        return ret;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(SpecializationConstant other) {
        return this.ID.Equals(other.ID) && this.Type == other.Type && this.Data.Equals(other.Data);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.ID.GetHashCode(), (int)this.Type, this.Data.GetHashCode());
    }
}