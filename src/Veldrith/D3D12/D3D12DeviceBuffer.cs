using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12DeviceBuffer.
/// </summary>
internal sealed class D3D12DeviceBuffer : DeviceBuffer {

    /// <summary>
    /// Stores the dynamic mapped pointer state used by this instance.
    /// </summary>
    private readonly IntPtr _dynamicMappedPointer;

    /// <summary>
    /// Stores the dynamic snapshot capacity state used by this instance.
    /// </summary>
    private readonly uint _dynamicSnapshotCapacity;

    /// <summary>
    /// Stores the alignment used between dynamic snapshots.
    /// </summary>
    private readonly uint _dynamicSnapshotAlignment;

    /// <summary>
    /// Stores the dynamic snapshot enabled state used by this instance.
    /// </summary>
    private readonly bool _dynamicSnapshotEnabled;

    /// <summary>
    /// Tracks whether is dynamic is currently enabled.
    /// </summary>
    private readonly bool _isDynamic;

    /// <summary>
    /// Tracks whether is staging is currently enabled.
    /// </summary>
    private readonly bool _isStaging;

    /// <summary>
    /// Stores the staging read buffer state used by this instance.
    /// </summary>
    private readonly ID3D12Resource _stagingReadBuffer;

    /// <summary>
    /// Stores the staging read mapped pointer state used by this instance.
    /// </summary>
    private readonly IntPtr _stagingReadMappedPointer;

    /// <summary>
    /// Stores the staging write buffer state used by this instance.
    /// </summary>
    private readonly ID3D12Resource _stagingWriteBuffer;

    /// <summary>
    /// Reuses a single barrier array for buffer state transitions.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Stores the placed-resource allocation block when this buffer is allocated from the D3D12 memory manager.
    /// </summary>
    private D3D12ResourceAllocation _allocation;

    /// <summary>
    /// Stores the staging write allocation when this buffer is backed by staging resources.
    /// </summary>
    private D3D12ResourceAllocation _stagingWriteAllocation;

    /// <summary>
    /// Stores the staging read allocation when this buffer is backed by staging resources.
    /// </summary>
    private D3D12ResourceAllocation _stagingReadAllocation;

    /// <summary>
    /// Stores the staging write mapped pointer state used by this instance.
    /// </summary>
    private readonly IntPtr _stagingWriteMappedPointer;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

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
    /// Stores the dynamic bind version state used by this instance.
    /// </summary>
    private ulong _dynamicBindVersion;

    /// <summary>
    /// Stores the dynamic snapshot base offset value used during command execution.
    /// </summary>
    private uint _dynamicSnapshotBaseOffset;

    /// <summary>
    /// Stores the dynamic snapshot initialized state used by this instance.
    /// </summary>
    private bool _dynamicSnapshotInitialized;

    /// <summary>
    /// Stores the dynamic snapshot write head state used by this instance.
    /// </summary>
    private uint _dynamicSnapshotWriteHead;

    /// <summary>
    /// Stores the staging read buffer dirty from write buffer state used by this instance.
    /// </summary>
    private bool _stagingReadBufferDirtyFromWriteBuffer;

    /// <summary>
    /// Stores the staging write buffer dirty from read buffer state used by this instance.
    /// </summary>
    private bool _stagingWriteBufferDirtyFromReadBuffer;

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
        this._dynamicSnapshotEnabled = this._isDynamic
                                       && (((description.Usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer)
                                           || ((description.Usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer)
                                           || ((description.Usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer)
                                           || ((description.Usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly));
        bool isUniformBuffer = (description.Usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer;
        this._dynamicSnapshotAlignment = isUniformBuffer ? 256u : 16u;
        uint minimumSnapshotCount = isUniformBuffer ? 1024u : 8u;
        this._dynamicSnapshotCapacity = this._dynamicSnapshotEnabled ? CalculateDynamicSnapshotCapacity(description.SizeInBytes, this._dynamicSnapshotAlignment, minimumSnapshotCount) : description.SizeInBytes;

        ResourceDescription resourceDescription = ResourceDescription.Buffer(this._dynamicSnapshotEnabled ? this._dynamicSnapshotCapacity : description.SizeInBytes, GetResourceFlags(description.Usage));

        if (this._isStaging) {
            this._stagingWriteAllocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
            this._stagingReadAllocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.CopyDest, HeapType.Readback, HeapFlags.AllowOnlyBuffers);
            this._stagingWriteBuffer = this._stagingWriteAllocation.Resource;
            this._stagingReadBuffer = this._stagingReadAllocation.Resource;
            this.NativeBuffer = this._stagingWriteBuffer;

            this._stagingWriteMappedPointer = this._stagingWriteAllocation.MappedPointer;
            this._stagingReadMappedPointer = this._stagingReadAllocation.MappedPointer;
        }
        else if (this._isDynamic) {
            this._allocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
            this.NativeBuffer = this._allocation.Resource;
            this._dynamicMappedPointer = this._allocation.MappedPointer;
        }
        else {
            this._allocation = gd.MemoryManager.CreateResource(ref resourceDescription, ResourceStates.Common, HeapType.Default, HeapFlags.AllowOnlyBuffers);
            this.NativeBuffer = this._allocation.Resource;
            this.CurrentState = ResourceStates.Common;
        }
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
    internal ulong GpuVirtualAddress => this.NativeBuffer.GPUVirtualAddress;

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
    internal ulong BindVersion => this._dynamicSnapshotEnabled ? this._dynamicBindVersion : 0UL;

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
        return this.NativeBuffer.GPUVirtualAddress + this.ResolveNativeOffset(offset);
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
        return this._dynamicSnapshotEnabled ? this._dynamicSnapshotBaseOffset + offset : offset;
    }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        if (this._isStaging) {
            this.gd.ReleaseAfterLastSubmission(this._stagingWriteAllocation);
            this.gd.ReleaseAfterLastSubmission(this._stagingReadAllocation);
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
            if (this._dynamicSnapshotEnabled) {
                this.UpdateDynamicSnapshot(source, destinationOffset, sizeInBytes);
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
    /// Updates the dynamic snapshot state for this command sequence.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="copySize">The copy size value used by this operation.</param>
    private unsafe void UpdateDynamicSnapshot(IntPtr source, uint destinationOffset, uint copySize) {
        if (copySize == 0) {
            return;
        }

        uint snapshotSize = destinationOffset + copySize;
        if (snapshotSize == 0) {
            snapshotSize = 1;
        }

        if (snapshotSize > this._dynamicSnapshotCapacity) {
            throw new VeldridException("Dynamic snapshot update exceeds snapshot buffer capacity.");
        }

        if (this._dynamicSnapshotWriteHead + snapshotSize > this._dynamicSnapshotCapacity) {
            this._dynamicSnapshotWriteHead = 0;
        }

        uint newBaseOffset = this._dynamicSnapshotWriteHead;
        byte* mappedPointer = (byte*)this._dynamicMappedPointer.ToPointer();
        if (this._dynamicSnapshotInitialized && newBaseOffset != this._dynamicSnapshotBaseOffset) {
            // Preserve only the unchanged prefix when callers update a subrange.
            // Most high-frequency VB/IB updates write from offset 0, so this avoids
            // copying the full logical buffer every flush.
            if (destinationOffset > 0) {
                uint prefixSize = destinationOffset;
                byte* src = mappedPointer + this._dynamicSnapshotBaseOffset;
                byte* dst = mappedPointer + newBaseOffset;
                Buffer.MemoryCopy(src, dst, snapshotSize, prefixSize);
            }
        }

        byte* destination = mappedPointer + newBaseOffset + destinationOffset;
        Buffer.MemoryCopy(source.ToPointer(), destination, snapshotSize - destinationOffset, copySize);

        uint previousBaseOffset = this._dynamicSnapshotBaseOffset;
        this._dynamicSnapshotBaseOffset = newBaseOffset;
        this._dynamicSnapshotWriteHead = AlignUp(newBaseOffset + snapshotSize, this._dynamicSnapshotAlignment);
        this._dynamicSnapshotInitialized = true;
        if (newBaseOffset != previousBaseOffset) {
            this._dynamicBindVersion++;
        }
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
            destination._stagingWriteBufferDirtyFromReadBuffer = true;
            destination._stagingReadBufferDirtyFromWriteBuffer = false;
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
            this.EnsureReadBufferIsCurrent();
            pointer = this._stagingReadMappedPointer;
            return true;
        }

        if (this._isDynamic) {
            pointer = this._dynamicMappedPointer;
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
                this._stagingReadBufferDirtyFromWriteBuffer = true;
                this._stagingWriteBufferDirtyFromReadBuffer = false;
            }
            else if (this._activeMapMode == MapMode.ReadWrite) {
                this._stagingWriteBufferDirtyFromReadBuffer = true;
                this._stagingReadBufferDirtyFromWriteBuffer = false;
                this.SyncReadBufferToWriteBuffer();
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

            return this._dynamicMappedPointer;
        }

        if (this._isStaging) {
            if (mode == MapMode.Read || mode == MapMode.ReadWrite) {
                this.EnsureReadBufferIsCurrent();
                return this._stagingReadMappedPointer;
            }

            return this._stagingWriteMappedPointer;
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
            byte* dst = (byte*)this._dynamicMappedPointer + destinationOffset;
            Buffer.MemoryCopy(source.ToPointer(), dst, this.SizeInBytes - destinationOffset, copySize);
            return;
        }

        if (this._isStaging) {
            byte* dst = (byte*)this._stagingWriteMappedPointer + destinationOffset;
            Buffer.MemoryCopy(source.ToPointer(), dst, this.SizeInBytes - destinationOffset, copySize);
            this._stagingReadBufferDirtyFromWriteBuffer = true;
            this._stagingWriteBufferDirtyFromReadBuffer = false;
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
                Buffer.MemoryCopy(source.ToPointer(), uploadBuffer.MappedPointer.ToPointer(), copySize, copySize);
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
            destinationPtr = destination._dynamicMappedPointer;
        }
        else if (destination._isStaging) {
            destinationPtr = destination._stagingWriteMappedPointer;
        }
        else {
            throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported because the destination cannot be CPU-written.");
        }

        byte* src = (byte*)sourcePtr + sourceOffset;
        byte* dst = (byte*)destinationPtr + destinationOffset;
        Buffer.MemoryCopy(src, dst, destination.SizeInBytes - destinationOffset, copySize);
        if (destination._isStaging) {
            destination._stagingReadBufferDirtyFromWriteBuffer = true;
            destination._stagingWriteBufferDirtyFromReadBuffer = false;
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
            destinationPtr = destination._dynamicMappedPointer;
        }
        else if (destination._isStaging) {
            destinationPtr = destination._stagingWriteMappedPointer;
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
            destination._stagingReadBufferDirtyFromWriteBuffer = true;
            destination._stagingWriteBufferDirtyFromReadBuffer = false;
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
            this.EnsureWriteBufferIsCurrent();
            return this._stagingWriteBuffer;
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
            return this._stagingReadBuffer;
        }

        return null;
    }

    /// <summary>
    /// Executes the ensure read buffer is current logic for this backend.
    /// </summary>
    private void EnsureReadBufferIsCurrent() {
        if (!this._isStaging || !this._stagingReadBufferDirtyFromWriteBuffer) {
            return;
        }

        unsafe {
            Buffer.MemoryCopy(this._stagingWriteMappedPointer.ToPointer(), this._stagingReadMappedPointer.ToPointer(), this.sizeInBytes, this.sizeInBytes);
        }

        this._stagingReadBufferDirtyFromWriteBuffer = false;
        this._stagingWriteBufferDirtyFromReadBuffer = false;
    }

    /// <summary>
    /// Executes the ensure write buffer is current logic for this backend.
    /// </summary>
    private void EnsureWriteBufferIsCurrent() {
        if (!this._isStaging || !this._stagingWriteBufferDirtyFromReadBuffer) {
            return;
        }

        this.SyncReadBufferToWriteBuffer();
    }

    /// <summary>
    /// Executes the sync read buffer to write buffer logic for this backend.
    /// </summary>
    private void SyncReadBufferToWriteBuffer() {
        unsafe {
            Buffer.MemoryCopy(this._stagingReadMappedPointer.ToPointer(), this._stagingWriteMappedPointer.ToPointer(), this.sizeInBytes, this.sizeInBytes);
        }

        this._stagingWriteBufferDirtyFromReadBuffer = false;
        this._stagingReadBufferDirtyFromWriteBuffer = false;
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
    /// Executes the calculate dynamic snapshot capacity logic for this backend.
    /// </summary>
    /// <param name="logicalSize">The logical size value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static uint CalculateDynamicSnapshotCapacity(uint logicalSize, uint alignment, uint minimumSnapshotCount) {
        // Keep enough history to avoid write-after-read hazards across a few in-flight frames,
        // but avoid very large per-buffer overallocation spikes when many dynamic meshes stream in.
        const ulong maxSnapshotBytes = 64UL * 1024UL * 1024UL;
        ulong alignedLogicalSize = AlignUp(logicalSize, alignment);
        ulong minimumSize = alignedLogicalSize * minimumSnapshotCount;
        ulong desired;
        if (logicalSize <= 256UL * 1024UL) {
            desired = logicalSize * 8UL;
        }
        else if (logicalSize <= 2UL * 1024UL * 1024UL) {
            desired = logicalSize * 4UL;
        }
        else {
            desired = logicalSize * 3UL;
        }

        ulong capped = Math.Min(Math.Max(desired, minimumSize), maxSnapshotBytes);
        ulong finalSize = Math.Max(alignedLogicalSize, capped);
        if (finalSize > uint.MaxValue) {
            return uint.MaxValue;
        }

        return (uint)finalSize;
    }

    /// <summary>
    /// Executes the align up logic for this backend.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    /// <param name="alignment">The alignment value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static uint AlignUp(uint value, uint alignment) {
        if (alignment == 0) {
            return value;
        }

        uint remainder = value % alignment;
        return remainder == 0 ? value : value + (alignment - remainder);
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
