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
    /// should be 0.
    /// For example, an InstanceStepRate of 3 indicates that 3 instances will be drawn with the same value for this layout.
    /// The
    /// next 3 instances will be drawn with the next value, and so on.
    /// </summary>
    public uint InstanceStepRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexLayoutDescription" /> type.
    /// </summary>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    /// <param name="elements">Specifies the value of <paramref name="elements" />.</param>
    public VertexLayoutDescription(uint stride, params VertexElementDescription[] elements) {
        this.Stride = stride;
        this.Elements = elements;
        this.InstanceStepRate = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexLayoutDescription" /> type.
    /// </summary>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    /// <param name="instanceStepRate">Specifies the value of <paramref name="instanceStepRate" />.</param>
    /// <param name="elements">Specifies the value of <paramref name="elements" />.</param>
    public VertexLayoutDescription(uint stride, uint instanceStepRate, params VertexElementDescription[] elements) {
        this.Stride = stride;
        this.Elements = elements;
        this.InstanceStepRate = instanceStepRate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexLayoutDescription" /> type.
    /// </summary>
    /// <param name="elements">Specifies the value of <paramref name="elements" />.</param>
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
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(VertexLayoutDescription other) {
        return this.Stride.Equals(other.Stride)
               && Util.ArrayEqualsEquatable(this.Elements, other.Elements)
               && this.InstanceStepRate.Equals(other.InstanceStepRate);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Stride.GetHashCode(), HashHelper.Array(this.Elements), this.InstanceStepRate.GetHashCode());
    }
}