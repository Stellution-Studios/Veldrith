using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Describes a single shader specialization constant. Used to substitute new values into Shaders when constructing a
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
    /// </summary>
    public ulong Data;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="data">The data value used by this operation.</param>
    public SpecializationConstant(uint id, ShaderConstantType type, ulong data) {
        this.ID = id;
        this.Type = type;
        this.Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, bool value)
        : this(id, ShaderConstantType.Bool, Store(value ? (byte)1u : (byte)0u)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, ushort value)
        : this(id, ShaderConstantType.UInt16, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, short value)
        : this(id, ShaderConstantType.Int16, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, uint value)
        : this(id, ShaderConstantType.UInt32, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, int value)
        : this(id, ShaderConstantType.Int32, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, ulong value)
        : this(id, ShaderConstantType.UInt64, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, long value)
        : this(id, ShaderConstantType.Int64, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, float value)
        : this(id, ShaderConstantType.Float, Store(value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializationConstant" /> type.
    /// </summary>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public SpecializationConstant(uint id, double value)
        : this(id, ShaderConstantType.Double, Store(value)) { }

    /// <summary>
    /// Packs a specialization constant value into a 64-bit storage slot.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    internal static unsafe ulong Store<T>(T value) {
        ulong ret;
        Unsafe.Write(&ret, value);
        return ret;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(SpecializationConstant other) {
        return this.ID.Equals(other.ID) && this.Type == other.Type && this.Data.Equals(other.Data);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.ID.GetHashCode(), (int)this.Type, this.Data.GetHashCode());
    }
}