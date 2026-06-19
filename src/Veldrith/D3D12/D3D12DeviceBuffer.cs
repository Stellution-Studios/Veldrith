using System;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12DeviceBuffer.
/// </summary>
internal sealed class D3D12DeviceBuffer : DeviceBuffer {

    /// <summary>
    /// Tracks dynamic upload-ring snapshots for buffer usages that are rebound frequently.
    /// </summary>
    private readonly D3D12DynamicBufferSnapshotState _dynamicSnapshot;

    /// <summary>
    /// Tracks whether is dynamic is currently enabled.
    /// </summary>
    private readonly bool _isDynamic;

    /// <summary>
    /// Tracks whether is staging is currently enabled.
    /// </summary>
    private readonly bool _isStaging;

    /// <summary>
    /// Tracks the upload/readback resource pair used by staging buffers.
    /// </summary>
    private readonly D3D12StagingBufferState _staging;

    /// <summary>
    /// Reuses a single barrier array for buffer state transitions.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Stores the placed-resource allocation block when this buffer is allocated from the D3D12 memory manager.
    /// </summary>
    private D3D12ResourceAllocation _allocation;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the stable GPU virtual address for this buffer resource.
    /// </summary>
    private readonly ulong _gpuVirtualAddress;

    /// <summary>
    /// Stores the size in bytes value used during command execution.
    /// </summary>
    private readonly uint sizeInBytes;

    /// <summary>
    /// Stores the active map mode state used by this instance.
    /// </summary>
    private MapMode? _activeMapMode;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;


    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DeviceBuffer" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12DeviceBuffer(D3D12GraphicsDevice gd, ref BufferDescription description) {
        this.gd = gd;
        this.SizeInBytes = description.SizeInBytes;
        this.Usage = description.Usage;
        this.sizeInBytes = description.SizeInBytes;
        this._isDynamic = (description.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        this._isStaging = (description.Usage & BufferUsage.Staging) == BufferUsage.Staging;
        this.CanTransitionState = !this._isDynamic && !this._isStaging;
        bool dynamicSnapshotEnabled = D3D12DynamicBufferSnapshotState.IsSupported(description.Usage);
        uint nativeSizeInBytes = dynamicSnapshotEnabled ? D3D12DynamicBufferSnapshotState.CalculateCapacity(description.SizeInBytes, description.Usage) : description.SizeInBytes;

        ResourceDescription resourceDescription = ResourceDescription.Buffer(nativeSizeInBytes, GetResourceFlags(description.Usage));

        if (this._isStaging) {
            D3D12ResourceAllocation stagingWriteAllocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
            D3D12ResourceAllocation stagingReadAllocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.CopyDest, HeapType.Readback, HeapFlags.AllowOnlyBuffers);
            this._staging = new D3D12StagingBufferState(stagingWriteAllocation, stagingReadAllocation);
            this.NativeBuffer = this._staging.WriteBuffer;
        }
        else if (this._isDynamic) {
            this._allocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
            this.NativeBuffer = this._allocation.Resource;
            if (dynamicSnapshotEnabled) {
                this._dynamicSnapshot = new D3D12DynamicBufferSnapshotState(this._allocation.MappedPointer, description.SizeInBytes, description.Usage);
            }
        }
        else {
            this._allocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.Common, HeapType.Default, HeapFlags.AllowOnlyBuffers);
            this.NativeBuffer = this._allocation.Resource;
            this.CurrentState = ResourceStates.Common;
        }

        this._gpuVirtualAddress = this.NativeBuffer.GPUVirtualAddress;
    }

    /// <summary>
    /// Gets or sets SizeInBytes.
    /// </summary>
    public override uint SizeInBytes { get; }

    /// <summary>
    /// Gets or sets Usage.
    /// </summary>
    public override BufferUsage Usage { get; }

    /// <summary>
    /// Gets or sets NativeBuffer.
    /// </summary>
    internal ID3D12Resource NativeBuffer { get; }

    /// <summary>
    /// Stores the gpu virtual address state used by this instance.
    /// </summary>
    internal ulong GpuVirtualAddress => this._gpuVirtualAddress;

    /// <summary>
    /// Executes the min logic for this backend.
    /// </summary>

    internal uint CurrentNativeSizeInBytes => (uint)Math.Min(uint.MaxValue, this.NativeBuffer.Description.Width);

    /// <summary>
    /// Gets or sets CurrentState.
    /// </summary>
    internal ResourceStates CurrentState { get; set; }

    /// <summary>
    /// Gets or sets CanTransitionState.
    /// </summary>
    internal bool CanTransitionState { get; }

    /// <summary>
    /// Stores the bind version state used by this instance.
    /// </summary>
    internal ulong BindVersion => this._dynamicSnapshot?.BindVersion ?? 0UL;

    /// <summary>
    /// Gets the source-copy byte count from the most recent dynamic snapshot update.
    /// </summary>
    internal uint LastDynamicSnapshotCopyBytes => this._dynamicSnapshot?.LastCopyBytes ?? 0;

    /// <summary>
    /// Gets the prefix-copy byte count from the most recent dynamic snapshot update.
    /// </summary>
    internal uint LastDynamicSnapshotPrefixCopyBytes => this._dynamicSnapshot?.LastPrefixCopyBytes ?? 0;

    /// <summary>
    /// Gets whether the most recent dynamic snapshot update moved to a new slot.
    /// </summary>
    internal bool LastDynamicSnapshotRotated => this._dynamicSnapshot?.LastRotated ?? false;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Gets the gpu virtual address value.
    /// </summary>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal ulong GetGpuVirtualAddress(uint offset) {
        return this._gpuVirtualAddress + this.ResolveNativeOffset(offset);
    }

    /// <summary>
    /// Gets the bindable size value.
    /// </summary>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal uint GetBindableSize(uint offset) {
        return offset < this.SizeInBytes ? this.SizeInBytes - offset : 0;
    }

    /// <summary>
    /// Executes the resolve native offset logic for this backend.
    /// </summary>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal uint ResolveNativeOffset(uint offset) {
        return this._dynamicSnapshot?.ResolveNativeOffset(offset) ?? offset;
    }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        if (this._isStaging) {
            this._staging.ReleaseAfterLastSubmission(this.gd);
        }
        else if (this._isDynamic) {
            this.gd.ReleaseAfterLastSubmission(this._allocation);
        }
        else {
            this.gd.ReleaseAfterLastSubmission(this._allocation);
        }

        this._disposed = true;
    }

    /// <summary>
    /// Updates the value state for this command sequence.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal D3D12ResourceAllocation Update(ID3D12GraphicsCommandList commandList, IntPtr source, uint destinationOffset, uint sizeInBytes) {
        if (destinationOffset + sizeInBytes > this.SizeInBytes) {
            throw new VeldridException("Buffer update range exceeds the destination buffer size.");
        }

        if (!this.CanTransitionState) {
            if (this._dynamicSnapshot != null) {
                this._dynamicSnapshot.Update(source, destinationOffset, sizeInBytes);
                return null;
            }

            this.WriteCpuData(source, destinationOffset, sizeInBytes);
            return null;
        }

        D3D12ResourceAllocation uploadBuffer = this.CreateUploadBuffer(source, sizeInBytes);
        if (commandList == null) {
            throw new VeldridException("A command list is required when updating GPU-local D3D12 buffers.");
        }

        ResourceStates previousState = this.CurrentState;
        this.Transition(commandList, previousState, ResourceStates.CopyDest);
        commandList.CopyBufferRegion(this.NativeBuffer, destinationOffset, uploadBuffer.Resource, uploadBuffer.Offset, sizeInBytes);
        this.Transition(commandList, ResourceStates.CopyDest, previousState);
        this.CurrentState = previousState;
        return uploadBuffer;
    }

    /// <summary>
    /// Copies to data between resources.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    internal void CopyTo(ID3D12GraphicsCommandList commandList, D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint sizeInBytes) {
        if (sourceOffset + sizeInBytes > this.SizeInBytes || destinationOffset + sizeInBytes > destination.SizeInBytes) {
            throw new VeldridException("Buffer copy range exceeds buffer bounds.");
        }

        ID3D12Resource sourceResource = this.GetCopySourceResource();
        ID3D12Resource destinationResource = destination.GetCopyDestinationResource();
        if (sourceResource == null || destinationResource == null) {
            this.CopyOnCpu(destination, sourceOffset, destinationOffset, sizeInBytes);
            return;
        }

        ResourceStates srcPrevious = this.CurrentState;
        ResourceStates dstPrevious = destination.CurrentState;
        if (this.CanTransitionState) {
            this.Transition(commandList, this.CurrentState, ResourceStates.CopySource);
            this.CurrentState = ResourceStates.CopySource;
        }

        if (destination.CanTransitionState) {
            destination.Transition(commandList, destination.CurrentState, ResourceStates.CopyDest);
            destination.CurrentState = ResourceStates.CopyDest;
        }

        commandList.CopyBufferRegion(destinationResource, destinationOffset, sourceResource, sourceOffset, sizeInBytes);

        if (this.CanTransitionState) {
            this.Transition(commandList, this.CurrentState, srcPrevious);
            this.CurrentState = srcPrevious;
        }

        if (destination.CanTransitionState) {
            destination.Transition(commandList, destination.CurrentState, dstPrevious);
            destination.CurrentState = dstPrevious;
        }

        if (destination._isStaging) {
            destination._staging.MarkReadBufferChanged(destinationOffset, sizeInBytes, destination.sizeInBytes);
        }
    }

    /// <summary>
    /// Maps the value resource for CPU access.
    /// </summary>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal MappedResource Map(MapMode mode) {
        IntPtr pointer = this.GetMapPointer(mode);
        this._activeMapMode = mode;
        return new MappedResource(this, mode, pointer, this.sizeInBytes);
    }

    /// <summary>
    /// Attempts to get cpu read pointer and reports whether it succeeded.
    /// </summary>
    /// <param name="pointer">The pointer value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool TryGetCpuReadPointer(out IntPtr pointer) {
        if (this._isStaging) {
            this._staging.EnsureReadBufferIsCurrent(this.sizeInBytes);
            pointer = this._staging.ReadMappedPointer;
            return true;
        }

        if (this._isDynamic) {
            pointer = this._allocation.MappedPointer;
            return true;
        }

        pointer = IntPtr.Zero;
        return false;
    }

    /// <summary>
    /// Unmaps the value resource from CPU access.
    /// </summary>
    internal void Unmap() {
        if (!this._activeMapMode.HasValue) {
            return;
        }

        if (this._isStaging) {
            if (this._activeMapMode == MapMode.Write) {
                this._staging.MarkWriteBufferChanged(0, this.sizeInBytes, this.sizeInBytes);
            }
            else if (this._activeMapMode == MapMode.ReadWrite) {
                this._staging.MarkReadBufferChanged(0, this.sizeInBytes, this.sizeInBytes);
                this._staging.SyncReadBufferToWriteBuffer(this.sizeInBytes);
            }
        }

        this._activeMapMode = null;
    }

    /// <summary>
    /// Gets the map pointer value.
    /// </summary>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private IntPtr GetMapPointer(MapMode mode) {
        if (this._isDynamic) {
            if (mode != MapMode.Write) {
                throw new VeldridException("Dynamic D3D12 buffers only support MapMode.Write.");
            }

            return this._allocation.MappedPointer;
        }

        if (this._isStaging) {
            if (mode == MapMode.Read || mode == MapMode.ReadWrite) {
                this._staging.EnsureReadBufferIsCurrent(this.sizeInBytes);
                return this._staging.ReadMappedPointer;
            }

            return this._staging.WriteMappedPointer;
        }

        throw new VeldridException("Only Dynamic or Staging buffers can be mapped.");
    }

    /// <summary>
    /// Executes the write cpu data logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="copySize">The copy size value used by this operation.</param>
    private unsafe void WriteCpuData(IntPtr source, uint destinationOffset, uint copySize) {
        if (this._isDynamic) {
            byte* dst = (byte*)this._allocation.MappedPointer + destinationOffset;
            CopyMemory(source.ToPointer(), dst, copySize);
            return;
        }

        if (this._isStaging) {
            byte* dst = (byte*)this._staging.WriteMappedPointer + destinationOffset;
            CopyMemory(source.ToPointer(), dst, copySize);
            this._staging.MarkWriteBufferChanged(destinationOffset, copySize, this.sizeInBytes);
            return;
        }

        throw new VeldridException("CPU updates on default D3D12 buffers require a command-list copy.");
    }

    /// <summary>
    /// Creates the upload buffer instance used by this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="copySize">The copy size value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private D3D12ResourceAllocation CreateUploadBuffer(IntPtr source, uint copySize) {
        D3D12ResourceAllocation uploadBuffer = this.gd.RentUploadBuffer(copySize);
        try {
            unsafe {
                CopyMemory(source.ToPointer(), uploadBuffer.MappedPointer.ToPointer(), copySize);
            }

            return uploadBuffer;
        }
        catch {
            this.gd.ReturnUploadBuffer(uploadBuffer);
            throw;
        }
    }

    /// <summary>
    /// Copies on cpu data between resources.
    /// </summary>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="copySize">The copy size value used by this operation.</param>
    private unsafe void CopyOnCpu(D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint copySize) {
        if (!this.TryGetCpuReadPointer(out IntPtr sourcePtr)) {
            if (this.CanTransitionState) {
                this.CopyDefaultSourceToCpuWritableDestination(destination, sourceOffset, destinationOffset, copySize);
                return;
            }

            throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported for GPU copy and no CPU source mapping is available.");
        }

        IntPtr destinationPtr;
        if (destination._isDynamic) {
            destinationPtr = destination._allocation.MappedPointer;
        }
        else if (destination._isStaging) {
            destinationPtr = destination._staging.WriteMappedPointer;
        }
        else {
            throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported because the destination cannot be CPU-written.");
        }

        byte* src = (byte*)sourcePtr + sourceOffset;
        byte* dst = (byte*)destinationPtr + destinationOffset;
        Buffer.MemoryCopy(src, dst, destination.SizeInBytes - destinationOffset, copySize);
        if (destination._isStaging) {
            destination._staging.MarkWriteBufferChanged(destinationOffset, copySize, destination.sizeInBytes);
        }
    }

    /// <summary>
    /// Copies default source to cpu writable destination data between resources.
    /// </summary>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="copySize">The copy size value used by this operation.</param>
    private unsafe void CopyDefaultSourceToCpuWritableDestination(D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint copySize) {
        IntPtr destinationPtr;
        if (destination._isDynamic) {
            destinationPtr = destination._allocation.MappedPointer;
        }
        else if (destination._isStaging) {
            destinationPtr = destination._staging.WriteMappedPointer;
        }
        else {
            throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported because the destination cannot be CPU-written.");
        }

        ResourceDescription readbackDescription = ResourceDescription.Buffer(copySize);
        D3D12ResourceAllocation readbackAllocation = this.gd.MemoryManager.CreateResource(ref readbackDescription, ResourceStates.CopyDest, HeapType.Readback, HeapFlags.AllowOnlyBuffers);
        ID3D12Resource readbackBuffer = readbackAllocation.Resource;
        try {
            this.gd.ExecuteImmediateCommand(commandList => {
                ResourceStates previousState = this.CurrentState;
                if (this.CanTransitionState && previousState != ResourceStates.CopySource) {
                    ResourceBarrier toCopySource = ResourceBarrier.BarrierTransition(this.NativeBuffer, previousState, ResourceStates.CopySource);
                    this._singleBarrier[0] = toCopySource;
                    commandList.ResourceBarrier(this._singleBarrier);
                }

                commandList.CopyBufferRegion(readbackBuffer, 0, this.NativeBuffer, sourceOffset, copySize);

                if (this.CanTransitionState && previousState != ResourceStates.CopySource) {
                    ResourceBarrier fromCopySource = ResourceBarrier.BarrierTransition(this.NativeBuffer, ResourceStates.CopySource, previousState);
                    this._singleBarrier[0] = fromCopySource;
                    commandList.ResourceBarrier(this._singleBarrier);
                }
            }, waitForCompletion: true);

            byte* src = (byte*)readbackAllocation.MappedPointer.ToPointer();
            byte* dst = (byte*)destinationPtr + destinationOffset;
            Buffer.MemoryCopy(src, dst, destination.SizeInBytes - destinationOffset, copySize);
        }
        finally {
            readbackAllocation.Dispose();
        }

        if (destination._isStaging) {
            destination._staging.MarkWriteBufferChanged(destinationOffset, copySize, destination.sizeInBytes);
        }
    }

    /// <summary>
    /// Gets the copy source resource value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private ID3D12Resource GetCopySourceResource() {
        if (this.CanTransitionState || this._isDynamic) {
            return this.NativeBuffer;
        }

        if (this._isStaging) {
            this._staging.EnsureWriteBufferIsCurrent(this.sizeInBytes);
            return this._staging.WriteBuffer;
        }

        return null;
    }

    /// <summary>
    /// Gets the copy destination resource value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private ID3D12Resource GetCopyDestinationResource() {
        if (this.CanTransitionState) {
            return this.NativeBuffer;
        }

        if (this._isStaging) {
            return this._staging.ReadBuffer;
        }

        return null;
    }

    /// <summary>
    /// Executes the transition logic for this backend.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    /// <param name="from">The from value used by this operation.</param>
    /// <param name="to">The to value used by this operation.</param>
    private void Transition(ID3D12GraphicsCommandList commandList, ResourceStates from, ResourceStates to) {
        if (from == to || !this.CanTransitionState) {
            return;
        }

        ResourceBarrier barrier = ResourceBarrier.BarrierTransition(this.NativeBuffer, from, to);
        this._singleBarrier[0] = barrier;
        commandList.ResourceBarrier(this._singleBarrier);
    }

    /// <summary>
    /// Copies a bounds-checked buffer update payload.
    /// </summary>
    /// <param name="source">The source memory.</param>
    /// <param name="destination">The destination memory.</param>
    /// <param name="byteCount">The byte count.</param>
    private static unsafe void CopyMemory(void* source, void* destination, uint byteCount) {
        if (byteCount == 0) {
            return;
        }

        Unsafe.CopyBlockUnaligned(destination, source, byteCount);
    }

    /// <summary>
    /// Gets the resource flags value.
    /// </summary>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceFlags GetResourceFlags(BufferUsage usage) {
        if ((usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite) {
            return ResourceFlags.AllowUnorderedAccess;
        }

        return ResourceFlags.None;
    }
}
