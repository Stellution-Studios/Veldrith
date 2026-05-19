using System;

namespace Veldrith;

/// <summary>
/// Describes a single element of a vertex.
/// </summary>
public struct VertexElementDescription : IEquatable<VertexElementDescription> {

    /// <summary>
    /// The name of the element.
    /// </summary>
    public string Name;

    /// <summary>
    /// The semantic type of the element.
    /// NOTE: When using Veldrith.SPIRV, all vertex elements will use
    /// <see cref="VertexElementSemantic.TextureCoordinate" />.
    /// </summary>
    public VertexElementSemantic Semantic;

    /// <summary>
    /// The format of the element.
    /// </summary>
    public VertexElementFormat Format;

    /// <summary>
    /// The offset in bytes from the beginning of the vertex.
    /// </summary>
    public uint Offset;

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexElementDescription" /> type.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    /// <param name="semantic">Specifies the value of <paramref name="semantic" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    public VertexElementDescription(string name, VertexElementSemantic semantic, VertexElementFormat format)
        : this(name, format, semantic) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexElementDescription" /> type.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="semantic">Specifies the value of <paramref name="semantic" />.</param>
    public VertexElementDescription(string name, VertexElementFormat format, VertexElementSemantic semantic) {
        this.Name = name;
        this.Format = format;
        this.Semantic = semantic;
        this.Offset = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexElementDescription" /> type.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    /// <param name="semantic">Specifies the value of <paramref name="semantic" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    public VertexElementDescription(string name, VertexElementSemantic semantic, VertexElementFormat format, uint offset) {
        this.Name = name;
        this.Format = format;
        this.Semantic = semantic;
        this.Offset = offset;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(VertexElementDescription other) {
        return this.Name.Equals(other.Name)
               && this.Format == other.Format
               && this.Semantic == other.Semantic
               && this.Offset == other.Offset;
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Name.GetHashCode(), (int)this.Format, (int)this.Semantic, (int)this.Offset);
    }
}