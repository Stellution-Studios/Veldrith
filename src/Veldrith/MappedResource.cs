using System;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// A structure describing the layout of a mapped <see cref="IMappableResource" /> object.
/// </summary>
public struct MappedResource {

    /// <summary>
    /// The resource which has been mapped.
    /// </summary>
    public readonly IMappableResource Resource;

    /// <summary>
    /// Identifies the <see cref="MapMode" /> that was used to map the resource.
    /// </summary>
    public readonly MapMode Mode;

    /// <summary>
    /// A pointer to the start of the mapped data region.
    /// </summary>
    public readonly IntPtr Data;

    /// <summary>
    /// The total size, in bytes, of the mapped data region.
    /// </summary>
    public readonly uint SizeInBytes;

    /// <summary>
    /// For mapped <see cref="Texture" /> resources, this is the subresource which is mapped.
    /// For <see cref="DeviceBuffer" /> resources, this field has no meaning.
    /// </summary>
    public readonly uint Subresource;

    /// <summary>
    /// For mapped <see cref="Texture" /> resources, this is the number of bytes between each row of texels.
    /// For <see cref="DeviceBuffer" /> resources, this field has no meaning.
    /// </summary>
    public readonly uint RowPitch;

    /// <summary>
    /// For mapped <see cref="Texture" /> resources, this is the number of bytes between each depth slice of a 3D Texture.
    /// For <see cref="DeviceBuffer" /> resources or 2D Textures, this field has no meaning.
    /// </summary>
    public readonly uint DepthPitch;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResource" /> type.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <param name="data">Specifies the value of <paramref name="data" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <param name="rowPitch">Specifies the value of <paramref name="rowPitch" />.</param>
    /// <param name="depthPitch">Specifies the value of <paramref name="depthPitch" />.</param>
    internal MappedResource(IMappableResource resource, MapMode mode, IntPtr data, uint sizeInBytes, uint subresource, uint rowPitch, uint depthPitch) {
        this.Resource = resource;
        this.Mode = mode;
        this.Data = data;
        this.SizeInBytes = sizeInBytes;
        this.Subresource = subresource;
        this.RowPitch = rowPitch;
        this.DepthPitch = depthPitch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResource" /> type.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <param name="data">Specifies the value of <paramref name="data" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    internal MappedResource(IMappableResource resource, MapMode mode, IntPtr data, uint sizeInBytes) {
        this.Resource = resource;
        this.Mode = mode;
        this.Data = data;
        this.SizeInBytes = sizeInBytes;

        this.Subresource = 0;
        this.RowPitch = 0;
        this.DepthPitch = 0;
    }
}

/// <summary>
/// A typed view of a <see cref="MappedResource" />. Provides by-reference structured access to individual elements in
/// the
/// mapped resource.
/// </summary>
/// <typeparam name="T">The blittable value type which mapped data is viewed as.</typeparam>
public unsafe struct MappedResourceView<T> where T : struct {

    /// <summary>
    /// Stores the value associated with <c>_s_sizeof_t</c>.
    /// </summary>
    private static readonly int _s_sizeof_t = Unsafe.SizeOf<T>();

    /// <summary>
    /// The <see cref="MappedResource" /> that this instance views.
    /// </summary>
    public readonly MappedResource MappedResource;

    /// <summary>
    /// The total size in bytes of the mapped resource.
    /// </summary>
    public readonly uint SizeInBytes;

    /// <summary>
    /// The total number of structures that is contained in the resource. This is effectively the total number of bytes
    /// divided by the size of the structure type.
    /// </summary>
    public readonly int Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResourceView" /> type.
    /// </summary>
    /// <param name="rawResource">Specifies the value of <paramref name="rawResource" />.</param>
    public MappedResourceView(MappedResource rawResource) {
        this.MappedResource = rawResource;
        this.SizeInBytes = rawResource.SizeInBytes;
        this.Count = (int)(this.SizeInBytes / _s_sizeof_t);
    }

    /// <summary>
    /// Gets a reference to the structure value at the given index.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <returns>A reference to the value at the given index.</returns>
    public ref T this[int index] {
        get {
            if (index >= this.Count || index < 0) {
                throw new IndexOutOfRangeException($"Given index ({index}) must be non-negative and less than Count ({this.Count}).");
            }

            byte* ptr = (byte*)this.MappedResource.Data + index * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }

    /// <summary>
    /// Gets a reference to the structure value at the given index.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <returns>A reference to the value at the given index.</returns>
    public ref T this[uint index] {
        get {
            if (index >= this.Count) {
                throw new IndexOutOfRangeException($"Given index ({index}) must be less than Count ({this.Count}).");
            }

            byte* ptr = (byte*)this.MappedResource.Data + index * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }

    /// <summary>
    /// Gets a reference to the structure at the given 2-dimensional texture coordinates.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <returns>A reference to the value at the given coordinates.</returns>
    public ref T this[int x, int y] {
        get {
            byte* ptr = (byte*)this.MappedResource.Data + y * this.MappedResource.RowPitch + x * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }

    /// <summary>
    /// Gets a reference to the structure at the given 2-dimensional texture coordinates.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <returns>A reference to the value at the given coordinates.</returns>
    public ref T this[uint x, uint y] {
        get {
            byte* ptr = (byte*)this.MappedResource.Data + y * this.MappedResource.RowPitch + x * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }

    /// <summary>
    /// Gets a reference to the structure at the given 3-dimensional texture coordinates.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <returns>A reference to the value at the given coordinates.</returns>
    public ref T this[int x, int y, int z] {
        get {
            byte* ptr = (byte*)this.MappedResource.Data
                        + z * this.MappedResource.DepthPitch
                        + y * this.MappedResource.RowPitch
                        + x * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }

    /// <summary>
    /// Gets a reference to the structure at the given 3-dimensional texture coordinates.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <returns>A reference to the value at the given coordinates.</returns>
    public ref T this[uint x, uint y, uint z] {
        get {
            byte* ptr = (byte*)this.MappedResource.Data
                        + z * this.MappedResource.DepthPitch
                        + y * this.MappedResource.RowPitch
                        + x * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }
}