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
    /// <param name="name">The name used by this operation.</param>
    /// <param name="semantic">The semantic value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    public VertexElementDescription(string name, VertexElementSemantic semantic, VertexElementFormat format)
        : this(name, format, semantic) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexElementDescription" /> type.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="semantic">The semantic value used by this operation.</param>
    public VertexElementDescription(string name, VertexElementFormat format, VertexElementSemantic semantic) {
        this.Name = name;
        this.Format = format;
        this.Semantic = semantic;
        this.Offset = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexElementDescription" /> type.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <param name="semantic">The semantic value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    public VertexElementDescription(string name, VertexElementSemantic semantic, VertexElementFormat format, uint offset) {
        this.Name = name;
        this.Format = format;
        this.Semantic = semantic;
        this.Offset = offset;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(VertexElementDescription other) {
        return this.Name.Equals(other.Name)
               && this.Format == other.Format
               && this.Semantic == other.Semantic
               && this.Offset == other.Offset;
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Name.GetHashCode(), (int)this.Format, (int)this.Semantic, (int)this.Offset);
    }
}