using System;

namespace Veldrith;

/// <summary>
/// Describes the layout of vertex data in a single <see cref="DeviceBuffer" /> used as a vertex buffer.
/// </summary>
public struct VertexLayoutDescription : IEquatable<VertexLayoutDescription> {

    /// <summary>
    /// The number of bytes in between successive elements in the <see cref="DeviceBuffer" />.
    /// </summary>
    public uint Stride;

    /// <summary>
    /// An array of <see cref="VertexElementDescription" /> objects, each describing a single element of vertex data.
    /// </summary>
    public VertexElementDescription[] Elements;

    /// <summary>
    /// A value controlling how often data for instances is advanced for this layout. For per-vertex elements, this value
    /// </summary>
    public uint InstanceStepRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexLayoutDescription" /> type.
    /// </summary>
    /// <param name="stride">The stride value used by this operation.</param>
    /// <param name="elements">The elements value used by this operation.</param>
    public VertexLayoutDescription(uint stride, params VertexElementDescription[] elements) {
        this.Stride = stride;
        this.Elements = elements;
        this.InstanceStepRate = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexLayoutDescription" /> type.
    /// </summary>
    /// <param name="stride">The stride value used by this operation.</param>
    /// <param name="instanceStepRate">The instance step rate value used by this operation.</param>
    /// <param name="elements">The elements value used by this operation.</param>
    public VertexLayoutDescription(uint stride, uint instanceStepRate, params VertexElementDescription[] elements) {
        this.Stride = stride;
        this.Elements = elements;
        this.InstanceStepRate = instanceStepRate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexLayoutDescription" /> type.
    /// </summary>
    /// <param name="elements">The elements value used by this operation.</param>
    public VertexLayoutDescription(params VertexElementDescription[] elements) {
        this.Elements = elements;
        uint computedStride = 0;

        for (int i = 0; i < elements.Length; i++) {
            uint elementSize = FormatSizeHelpers.GetSizeInBytes(elements[i].Format);
            if (elements[i].Offset != 0) {
                computedStride = elements[i].Offset + elementSize;
            }
            else {
                computedStride += elementSize;
            }
        }

        this.Stride = computedStride;
        this.InstanceStepRate = 0;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(VertexLayoutDescription other) {
        return this.Stride.Equals(other.Stride)
               && Util.ArrayEqualsEquatable(this.Elements, other.Elements)
               && this.InstanceStepRate.Equals(other.InstanceStepRate);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Stride.GetHashCode(), HashHelper.Array(this.Elements), this.InstanceStepRate.GetHashCode());
    }
}