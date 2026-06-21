using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Owns the upload/readback resource pair and dirty-range synchronization for a D3D12 staging buffer.
/// </summary>
internal sealed class D3D12StagingBufferState {

    /// <summary>
    /// Stores the CPU-writable upload allocation.
    /// </summary>
    private readonly D3D12ResourceAllocation _writeAllocation;

    /// <summary>
    /// Stores the CPU-readable readback allocation.
    /// </summary>
    private readonly D3D12ResourceAllocation _readAllocation;

    /// <summary>
    /// Stores whether the readback resource needs data copied from the upload resource.
    /// </summary>
    private bool _readDirtyFromWrite;

    /// <summary>
    /// Stores whether the upload resource needs data copied from the readback resource.
    /// </summary>
    private bool _writeDirtyFromRead;

    /// <summary>
    /// Stores the first byte of the upload-to-readback dirty range.
    /// </summary>
    private uint _readDirtyStart;

    /// <summary>
    /// Stores the byte after the last byte of the upload-to-readback dirty range.
    /// </summary>
    private uint _readDirtyEnd;

    /// <summary>
    /// Stores the first byte of the readback-to-upload dirty range.
    /// </summary>
    private uint _writeDirtyStart;

    /// <summary>
    /// Stores the byte after the last byte of the readback-to-upload dirty range.
    /// </summary>
    private uint _writeDirtyEnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12StagingBufferState" /> class.
    /// </summary>
    /// <param name="writeAllocation">The CPU-writable upload allocation.</param>
    /// <param name="readAllocation">The CPU-readable readback allocation.</param>
    internal D3D12StagingBufferState(D3D12ResourceAllocation writeAllocation, D3D12ResourceAllocation readAllocation) {
        this._writeAllocation = writeAllocation;
        this._readAllocation = readAllocation;
    }

    /// <summary>
    /// Gets the CPU-writable upload resource used as the public native buffer.
    /// </summary>
    internal ID3D12Resource WriteBuffer => this._writeAllocation.Resource;

    /// <summary>
    /// Gets the CPU-readable readback resource used as GPU copy destination.
    /// </summary>
    internal ID3D12Resource ReadBuffer => this._readAllocation.Resource;

    /// <summary>
    /// Gets the persistently mapped CPU-writable upload pointer.
    /// </summary>
    internal IntPtr WriteMappedPointer => this._writeAllocation.MappedPointer;

    /// <summary>
    /// Gets the persistently mapped CPU-readable readback pointer.
    /// </summary>
    internal IntPtr ReadMappedPointer => this._readAllocation.MappedPointer;

    /// <summary>
    /// Defers release of both staging allocations until submitted work can no longer reference them.
    /// </summary>
    /// <param name="gd">The graphics device that owns deferred release.</param>
    internal void ReleaseAfterLastSubmission(D3D12GraphicsDevice gd) {
        gd.ReleaseAfterLastSubmission(this._writeAllocation);
        gd.ReleaseAfterLastSubmission(this._readAllocation);
    }

    /// <summary>
    /// Marks bytes written through the upload side and invalidates pending readback-to-upload synchronization.
    /// </summary>
    /// <param name="offset">The first changed byte.</param>
    /// <param name="sizeInBytes">The changed byte count.</param>
    /// <param name="bufferSize">The logical buffer size.</param>
    internal void MarkWriteBufferChanged(uint offset, uint sizeInBytes, uint bufferSize) {
        this.MarkDirtyRange(ref this._readDirtyFromWrite, ref this._readDirtyStart, ref this._readDirtyEnd, offset, sizeInBytes, bufferSize);
        this.ClearWriteDirtyFromRead();
    }

    /// <summary>
    /// Marks bytes written through the readback side and invalidates pending upload-to-readback synchronization.
    /// </summary>
    /// <param name="offset">The first changed byte.</param>
    /// <param name="sizeInBytes">The changed byte count.</param>
    /// <param name="bufferSize">The logical buffer size.</param>
    internal void MarkReadBufferChanged(uint offset, uint sizeInBytes, uint bufferSize) {
        this.MarkDirtyRange(ref this._writeDirtyFromRead, ref this._writeDirtyStart, ref this._writeDirtyEnd, offset, sizeInBytes, bufferSize);
        this.ClearReadDirtyFromWrite();
    }

    /// <summary>
    /// Ensures the CPU-readable readback resource contains the latest staging bytes.
    /// </summary>
    /// <param name="bufferSize">The logical buffer size.</param>
    internal void EnsureReadBufferIsCurrent(uint bufferSize) {
        if (!this._readDirtyFromWrite) {
            return;
        }

        this.CopyWriteToRead(this._readDirtyStart, this.GetDirtyByteCount(this._readDirtyStart, this._readDirtyEnd, bufferSize));
        this.ClearReadDirtyFromWrite();
        this.ClearWriteDirtyFromRead();
    }

    /// <summary>
    /// Ensures the CPU-writable upload resource contains the latest staging bytes.
    /// </summary>
    /// <param name="bufferSize">The logical buffer size.</param>
    internal void EnsureWriteBufferIsCurrent(uint bufferSize) {
        if (!this._writeDirtyFromRead) {
            return;
        }

        this.SyncReadBufferToWriteBuffer(bufferSize);
    }

    /// <summary>
    /// Copies the dirty readback range into the upload resource.
    /// </summary>
    /// <param name="bufferSize">The logical buffer size.</param>
    internal void SyncReadBufferToWriteBuffer(uint bufferSize) {
        if (!this._writeDirtyFromRead) {
            return;
        }

        this.CopyReadToWrite(this._writeDirtyStart, this.GetDirtyByteCount(this._writeDirtyStart, this._writeDirtyEnd, bufferSize));
        this.ClearWriteDirtyFromRead();
        this.ClearReadDirtyFromWrite();
    }

    /// <summary>
    /// Copies bytes from the CPU-writable upload resource to the CPU-readable readback resource.
    /// </summary>
    /// <param name="offset">The first byte to copy.</param>
    /// <param name="sizeInBytes">The byte count to copy.</param>
    private unsafe void CopyWriteToRead(uint offset, uint sizeInBytes) {
        if (sizeInBytes == 0) {
            return;
        }

        byte* source = (byte*)this.WriteMappedPointer.ToPointer() + offset;
        byte* destination = (byte*)this.ReadMappedPointer.ToPointer() + offset;
        Buffer.MemoryCopy(source, destination, sizeInBytes, sizeInBytes);
    }

    /// <summary>
    /// Copies bytes from the CPU-readable readback resource to the CPU-writable upload resource.
    /// </summary>
    /// <param name="offset">The first byte to copy.</param>
    /// <param name="sizeInBytes">The byte count to copy.</param>
    private unsafe void CopyReadToWrite(uint offset, uint sizeInBytes) {
        if (sizeInBytes == 0) {
            return;
        }

        byte* source = (byte*)this.ReadMappedPointer.ToPointer() + offset;
        byte* destination = (byte*)this.WriteMappedPointer.ToPointer() + offset;
        Buffer.MemoryCopy(source, destination, sizeInBytes, sizeInBytes);
    }

    /// <summary>
    /// Adds a changed byte range to a dirty range.
    /// </summary>
    /// <param name="isDirty">The dirty flag to update.</param>
    /// <param name="dirtyStart">The dirty range start to update.</param>
    /// <param name="dirtyEnd">The dirty range end to update.</param>
    /// <param name="offset">The first changed byte.</param>
    /// <param name="sizeInBytes">The changed byte count.</param>
    /// <param name="bufferSize">The logical buffer size.</param>
    private void MarkDirtyRange(ref bool isDirty, ref uint dirtyStart, ref uint dirtyEnd, uint offset, uint sizeInBytes, uint bufferSize) {
        if (sizeInBytes == 0) {
            return;
        }

        uint end = offset + sizeInBytes;
        if (offset > bufferSize || end > bufferSize || end < offset) {
            throw new VeldridException("Staging buffer dirty range exceeds buffer bounds.");
        }

        if (!isDirty) {
            dirtyStart = offset;
            dirtyEnd = end;
            isDirty = true;
            return;
        }

        dirtyStart = Math.Min(dirtyStart, offset);
        dirtyEnd = Math.Max(dirtyEnd, end);
    }

    /// <summary>
    /// Gets a bounded dirty byte count.
    /// </summary>
    /// <param name="start">The first dirty byte.</param>
    /// <param name="end">The byte after the last dirty byte.</param>
    /// <param name="bufferSize">The logical buffer size.</param>
    /// <returns>The byte count to copy.</returns>
    private uint GetDirtyByteCount(uint start, uint end, uint bufferSize) {
        if (start >= bufferSize || end <= start) {
            return 0;
        }

        return Math.Min(end, bufferSize) - start;
    }

    /// <summary>
    /// Clears upload-to-readback dirty tracking.
    /// </summary>
    private void ClearReadDirtyFromWrite() {
        this._readDirtyFromWrite = false;
        this._readDirtyStart = 0;
        this._readDirtyEnd = 0;
    }

    /// <summary>
    /// Clears readback-to-upload dirty tracking.
    /// </summary>
    private void ClearWriteDirtyFromRead() {
        this._writeDirtyFromRead = false;
        this._writeDirtyStart = 0;
        this._writeDirtyEnd = 0;
    }
}
