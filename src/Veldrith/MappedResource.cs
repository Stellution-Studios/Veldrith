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
    /// </summary>
    public readonly uint Subresource;

    /// <summary>
    /// For mapped <see cref="Texture" /> resources, this is the number of bytes between each row of texels.
    /// </summary>
    public readonly uint RowPitch;

    /// <summary>
    /// For mapped <see cref="Texture" /> resources, this is the number of bytes between each depth slice of a 3D Texture.
    /// </summary>
    public readonly uint DepthPitch;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResource" /> type.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="data">The data value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <param name="rowPitch">The row pitch value used by this operation.</param>
    /// <param name="depthPitch">The depth pitch value used by this operation.</param>
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
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="data">The data value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
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
/// </summary>
public unsafe struct MappedResourceView<T> where T : struct {

    /// <summary>
    /// Stores the s sizeof t value used during command execution.
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
    /// </summary>
    public readonly int Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedResourceView" /> type.
    /// </summary>
    /// <param name="rawResource">The raw resource value used by this operation.</param>
    public MappedResourceView(MappedResource rawResource) {
        this.MappedResource = rawResource;
        this.SizeInBytes = rawResource.SizeInBytes;
        this.Count = (int)(this.SizeInBytes / _s_sizeof_t);
    }

    /// <summary>
    /// Gets a reference to the structure value at the given index.
    /// </summary>
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
    public ref T this[int x, int y] {
        get {
            byte* ptr = (byte*)this.MappedResource.Data + y * this.MappedResource.RowPitch + x * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }

    /// <summary>
    /// Gets a reference to the structure at the given 2-dimensional texture coordinates.
    /// </summary>
    public ref T this[uint x, uint y] {
        get {
            byte* ptr = (byte*)this.MappedResource.Data + y * this.MappedResource.RowPitch + x * _s_sizeof_t;
            return ref Unsafe.AsRef<T>(ptr);
        }
    }

    /// <summary>
    /// Gets a reference to the structure at the given 3-dimensional texture coordinates.
    /// </summary>
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