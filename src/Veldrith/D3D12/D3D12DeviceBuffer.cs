using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Represents the D3D12DeviceBuffer class.
/// </summary>
internal sealed class D3D12DeviceBuffer : DeviceBuffer {

    /// <summary>
    /// Represents the _dynamicMappedPointer field.
    /// </summary>
    private readonly IntPtr _dynamicMappedPointer;

    /// <summary>
    /// Represents the _dynamicSnapshotCapacity field.
    /// </summary>
    private readonly uint _dynamicSnapshotCapacity;

    /// <summary>
    /// Represents the _dynamicSnapshotEnabled field.
    /// </summary>
    private readonly bool _dynamicSnapshotEnabled;

    /// <summary>
    /// Represents the _isDynamic field.
    /// </summary>
    private readonly bool _isDynamic;

    /// <summary>
    /// Represents the _isStaging field.
    /// </summary>
    private readonly bool _isStaging;

    /// <summary>
    /// Represents the _stagingReadBuffer field.
    /// </summary>
    private readonly ID3D12Resource _stagingReadBuffer;

    /// <summary>
    /// Represents the _stagingReadMappedPointer field.
    /// </summary>
    private readonly IntPtr _stagingReadMappedPointer;

    /// <summary>
    /// Represents the _stagingWriteBuffer field.
    /// </summary>
    private readonly ID3D12Resource _stagingWriteBuffer;

    /// <summary>
    /// Represents the _stagingWriteMappedPointer field.
    /// </summary>
    private readonly IntPtr _stagingWriteMappedPointer;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Represents the sizeInBytes field.
    /// </summary>
    private readonly uint sizeInBytes;

    /// <summary>
    /// Represents the _activeMapMode field.
    /// </summary>
    private MapMode? _activeMapMode;

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Represents the _dynamicBindVersion field.
    /// </summary>
    private ulong _dynamicBindVersion;

    /// <summary>
    /// Represents the _dynamicSnapshotBaseOffset field.
    /// </summary>
    private uint _dynamicSnapshotBaseOffset;

    /// <summary>
    /// Represents the _dynamicSnapshotInitialized field.
    /// </summary>
    private bool _dynamicSnapshotInitialized;

    /// <summary>
    /// Represents the _dynamicSnapshotWriteHead field.
    /// </summary>
    private uint _dynamicSnapshotWriteHead;

    /// <summary>
    /// Represents the _stagingReadBufferDirtyFromWriteBuffer field.
    /// </summary>
    private bool _stagingReadBufferDirtyFromWriteBuffer;

    /// <summary>
    /// Represents the _stagingWriteBufferDirtyFromReadBuffer field.
    /// </summary>
    private bool _stagingWriteBufferDirtyFromReadBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DeviceBuffer" /> type.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    /// <param name="description">The value of description.</param>
    public D3D12DeviceBuffer(D3D12GraphicsDevice gd, ref BufferDescription description) {
        this.gd = gd;
        this.SizeInBytes = description.SizeInBytes;
        this.Usage = description.Usage;
        this.sizeInBytes = description.SizeInBytes;
        this._isDynamic = (description.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        this._isStaging = (description.Usage & BufferUsage.Staging) == BufferUsage.Staging;
        this.CanTransitionState = !this._isDynamic && !this._isStaging;
        this._dynamicSnapshotEnabled = this._isDynamic && ((description.Usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer || (description.Usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer);
        this._dynamicSnapshotCapacity = this._dynamicSnapshotEnabled ? CalculateDynamicSnapshotCapacity(description.SizeInBytes) : description.SizeInBytes;

        ResourceDescription resourceDescription = ResourceDescription.Buffer(this._dynamicSnapshotEnabled ? this._dynamicSnapshotCapacity : description.SizeInBytes, GetResourceFlags(description.Usage));

        if (this._isStaging) {
            this._stagingWriteBuffer = gd.Device.CreateCommittedResource(HeapType.Upload, HeapFlags.None, resourceDescription, ResourceStates.GenericRead);
            this._stagingReadBuffer = gd.Device.CreateCommittedResource(HeapType.Readback, HeapFlags.None, resourceDescription, ResourceStates.CopyDest);
            this.NativeBuffer = this._stagingWriteBuffer;

            unsafe {
                void* writePtr = null;
                this._stagingWriteBuffer.Map(0, &writePtr).CheckError();
                this._stagingWriteMappedPointer = (IntPtr)writePtr;

                void* readPtr = null;
                this._stagingReadBuffer.Map(0, &readPtr).CheckError();
                this._stagingReadMappedPointer = (IntPtr)readPtr;
            }
        }
        else if (this._isDynamic) {
            this.NativeBuffer = gd.Device.CreateCommittedResource(HeapType.Upload, HeapFlags.None, resourceDescription, ResourceStates.GenericRead);
            unsafe {
                void* dataPointer = null;
                this.NativeBuffer.Map(0, &dataPointer).CheckError();
                this._dynamicMappedPointer = (IntPtr)dataPointer;
            }
        }
        else {
            this.NativeBuffer = gd.Device.CreateCommittedResource(HeapType.Default, HeapFlags.None, resourceDescription, ResourceStates.Common);
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
    /// Represents the GpuVirtualAddress field.
    /// </summary>
    internal ulong GpuVirtualAddress => this.NativeBuffer.GPUVirtualAddress;

    /// <summary>
    /// Performs the Min operation.
    /// </summary>
    /// <param name="uint">The value of uint.</param>
    /// <returns>The result of the Min operation.</returns>
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
    /// Represents the BindVersion field.
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
    /// Performs the GetGpuVirtualAddress operation.
    /// </summary>
    /// <param name="offset">The value of offset.</param>
    /// <returns>The result of the GetGpuVirtualAddress operation.</returns>
    internal ulong GetGpuVirtualAddress(uint offset) {
        return this.NativeBuffer.GPUVirtualAddress + this.ResolveNativeOffset(offset);
    }

    /// <summary>
    /// Performs the GetBindableSize operation.
    /// </summary>
    /// <param name="offset">The value of offset.</param>
    /// <returns>The result of the GetBindableSize operation.</returns>
    internal uint GetBindableSize(uint offset) {
        return offset < this.SizeInBytes ? this.SizeInBytes - offset : 0;
    }

    /// <summary>
    /// Performs the ResolveNativeOffset operation.
    /// </summary>
    /// <param name="offset">The value of offset.</param>
    /// <returns>The result of the ResolveNativeOffset operation.</returns>
    internal uint ResolveNativeOffset(uint offset) {
        return this._dynamicSnapshotEnabled ? this._dynamicSnapshotBaseOffset + offset : offset;
    }

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        if (this._isStaging) {
            this._stagingWriteBuffer.Unmap(0);
            this._stagingReadBuffer.Unmap(0);
            this._stagingWriteBuffer.Dispose();
            this._stagingReadBuffer.Dispose();
        }
        else if (this._isDynamic) {
            this.NativeBuffer.Unmap(0);
            this.NativeBuffer.Dispose();
        }
        else {
            this.NativeBuffer.Dispose();
        }

        this._disposed = true;
    }

    /// <summary>
    /// Performs the Update operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <returns>The result of the Update operation.</returns>
    internal ID3D12Resource Update(ID3D12GraphicsCommandList commandList, IntPtr source, uint destinationOffset, uint sizeInBytes) {
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

        ID3D12Resource uploadBuffer = this.CreateUploadBuffer(source, sizeInBytes);
        ResourceStates previousState = this.CurrentState;
        this.Transition(commandList, previousState, ResourceStates.CopyDest);
        commandList.CopyBufferRegion(this.NativeBuffer, destinationOffset, uploadBuffer, 0, sizeInBytes);
        this.Transition(commandList, ResourceStates.CopyDest, previousState);
        this.CurrentState = previousState;
        return uploadBuffer;
    }

    /// <summary>
    /// Performs the UpdateDynamicSnapshot operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="copySize">The value of copySize.</param>
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
        this._dynamicSnapshotWriteHead = AlignUp(newBaseOffset + snapshotSize, 16);
        this._dynamicSnapshotInitialized = true;
        if (newBaseOffset != previousBaseOffset) {
            this._dynamicBindVersion++;
        }
    }

    /// <summary>
    /// Performs the CopyTo operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
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
    /// Performs the Map operation.
    /// </summary>
    /// <param name="mode">The value of mode.</param>
    /// <returns>The result of the Map operation.</returns>
    internal MappedResource Map(MapMode mode) {
        IntPtr pointer = this.GetMapPointer(mode);
        this._activeMapMode = mode;
        return new MappedResource(this, mode, pointer, this.sizeInBytes);
    }

    /// <summary>
    /// Performs the TryGetCpuReadPointer operation.
    /// </summary>
    /// <param name="pointer">The value of pointer.</param>
    /// <returns>The result of the TryGetCpuReadPointer operation.</returns>
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
    /// Performs the Unmap operation.
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
    /// Performs the GetMapPointer operation.
    /// </summary>
    /// <param name="mode">The value of mode.</param>
    /// <returns>The result of the GetMapPointer operation.</returns>
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
    /// Performs the WriteCpuData operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="copySize">The value of copySize.</param>
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
    /// Performs the CreateUploadBuffer operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="copySize">The value of copySize.</param>
    /// <returns>The result of the CreateUploadBuffer operation.</returns>
    private ID3D12Resource CreateUploadBuffer(IntPtr source, uint copySize) {
        ID3D12Resource uploadBuffer = this.gd.Device.CreateCommittedResource(HeapType.Upload, HeapFlags.None, ResourceDescription.Buffer(copySize), ResourceStates.GenericRead);

        unsafe {
            void* mapped = null;
            uploadBuffer.Map(0, &mapped).CheckError();
            try {
                Buffer.MemoryCopy(source.ToPointer(), mapped, copySize, copySize);
            }
            finally {
                uploadBuffer.Unmap(0);
            }
        }

        return uploadBuffer;
    }

    /// <summary>
    /// Performs the CopyOnCpu operation.
    /// </summary>
    /// <param name="destination">The value of destination.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="copySize">The value of copySize.</param>
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
    /// Performs the CopyDefaultSourceToCpuWritableDestination operation.
    /// </summary>
    /// <param name="destination">The value of destination.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="copySize">The value of copySize.</param>
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

        ID3D12Resource readbackBuffer = this.gd.Device.CreateCommittedResource(HeapType.Readback, HeapFlags.None, ResourceDescription.Buffer(copySize), ResourceStates.CopyDest);
        ID3D12CommandAllocator allocator = this.gd.Device.CreateCommandAllocator(CommandListType.Direct);
        ID3D12GraphicsCommandList commandList = this.gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator);
        try {
            ResourceStates previousState = this.CurrentState;
            if (this.CanTransitionState && previousState != ResourceStates.CopySource) {
                ResourceBarrier toCopySource = ResourceBarrier.BarrierTransition(this.NativeBuffer, previousState, ResourceStates.CopySource);
                commandList.ResourceBarrier(new[] { toCopySource });
            }

            commandList.CopyBufferRegion(readbackBuffer, 0, this.NativeBuffer, sourceOffset, copySize);

            if (this.CanTransitionState && previousState != ResourceStates.CopySource) {
                ResourceBarrier fromCopySource = ResourceBarrier.BarrierTransition(this.NativeBuffer, ResourceStates.CopySource, previousState);
                commandList.ResourceBarrier(new[] { fromCopySource });
            }

            commandList.Close();
            this.gd.CommandQueue.ExecuteCommandList(commandList);
            this.gd.WaitForIdle();

            void* mapped = null;
            readbackBuffer.Map(0, &mapped).CheckError();
            try {
                byte* src = (byte*)mapped;
                byte* dst = (byte*)destinationPtr + destinationOffset;
                Buffer.MemoryCopy(src, dst, destination.SizeInBytes - destinationOffset, copySize);
            }
            finally {
                readbackBuffer.Unmap(0);
            }
        }
        finally {
            commandList.Dispose();
            allocator.Dispose();
            readbackBuffer.Dispose();
        }

        if (destination._isStaging) {
            destination._stagingReadBufferDirtyFromWriteBuffer = true;
            destination._stagingWriteBufferDirtyFromReadBuffer = false;
        }
    }

    /// <summary>
    /// Performs the GetCopySourceResource operation.
    /// </summary>
    /// <returns>The result of the GetCopySourceResource operation.</returns>
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
    /// Performs the GetCopyDestinationResource operation.
    /// </summary>
    /// <returns>The result of the GetCopyDestinationResource operation.</returns>
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
    /// Performs the EnsureReadBufferIsCurrent operation.
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
    /// Performs the EnsureWriteBufferIsCurrent operation.
    /// </summary>
    private void EnsureWriteBufferIsCurrent() {
        if (!this._isStaging || !this._stagingWriteBufferDirtyFromReadBuffer) {
            return;
        }

        this.SyncReadBufferToWriteBuffer();
    }

    /// <summary>
    /// Performs the SyncReadBufferToWriteBuffer operation.
    /// </summary>
    private void SyncReadBufferToWriteBuffer() {
        unsafe {
            Buffer.MemoryCopy(this._stagingReadMappedPointer.ToPointer(), this._stagingWriteMappedPointer.ToPointer(), this.sizeInBytes, this.sizeInBytes);
        }

        this._stagingWriteBufferDirtyFromReadBuffer = false;
        this._stagingReadBufferDirtyFromWriteBuffer = false;
    }

    /// <summary>
    /// Performs the Transition operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    /// <param name="from">The value of from.</param>
    /// <param name="to">The value of to.</param>
    private void Transition(ID3D12GraphicsCommandList commandList, ResourceStates from, ResourceStates to) {
        if (from == to || !this.CanTransitionState) {
            return;
        }

        ResourceBarrier barrier = ResourceBarrier.BarrierTransition(this.NativeBuffer, from, to);
        commandList.ResourceBarrier(new[] { barrier });
    }

    /// <summary>
    /// Performs the CalculateDynamicSnapshotCapacity operation.
    /// </summary>
    /// <param name="logicalSize">The value of logicalSize.</param>
    /// <returns>The result of the CalculateDynamicSnapshotCapacity operation.</returns>
    private static uint CalculateDynamicSnapshotCapacity(uint logicalSize) {
        const ulong maxSnapshotBytes = 256UL * 1024UL * 1024UL;
        ulong doubled = logicalSize * 2UL;
        ulong timesThirtyTwo = logicalSize * 32UL;
        ulong desired = Math.Max(doubled, timesThirtyTwo);
        ulong capped = Math.Min(desired, maxSnapshotBytes);
        ulong finalSize = Math.Max(logicalSize, capped);
        if (finalSize > uint.MaxValue) {
            return uint.MaxValue;
        }

        return (uint)finalSize;
    }

    /// <summary>
    /// Performs the AlignUp operation.
    /// </summary>
    /// <param name="value">The value of value.</param>
    /// <param name="alignment">The value of alignment.</param>
    /// <returns>The result of the AlignUp operation.</returns>
    private static uint AlignUp(uint value, uint alignment) {
        if (alignment == 0) {
            return value;
        }

        uint remainder = value % alignment;
        return remainder == 0 ? value : value + (alignment - remainder);
    }

    /// <summary>
    /// Performs the GetResourceFlags operation.
    /// </summary>
    /// <param name="usage">The value of usage.</param>
    /// <returns>The result of the GetResourceFlags operation.</returns>
    private static ResourceFlags GetResourceFlags(BufferUsage usage) {
        if ((usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite) {
            return ResourceFlags.AllowUnorderedAccess;
        }

        return ResourceFlags.None;
    }
}