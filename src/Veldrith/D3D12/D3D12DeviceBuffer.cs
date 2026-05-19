using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12
{
    internal sealed class D3D12DeviceBuffer : DeviceBuffer
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly bool _isDynamic;
        private readonly bool _isStaging;
        private readonly bool _isDefault;
        private readonly bool _dynamicSnapshotEnabled;
        private readonly uint _dynamicSnapshotCapacity;
        private readonly uint sizeInBytes;
        private ID3D12Resource _nativeBuffer;
        private readonly ID3D12Resource _stagingWriteBuffer;
        private readonly ID3D12Resource _stagingReadBuffer;
        private IntPtr _dynamicMappedPointer;
        private readonly IntPtr _stagingWriteMappedPointer;
        private readonly IntPtr _stagingReadMappedPointer;
        private bool _stagingReadBufferDirtyFromWriteBuffer;
        private bool _stagingWriteBufferDirtyFromReadBuffer;
        private uint _dynamicSnapshotWriteHead;
        private uint _dynamicSnapshotBaseOffset;
        private bool _dynamicSnapshotInitialized;
        private ulong _dynamicBindVersion;
        private MapMode? _activeMapMode;
        private bool _disposed;
        private string _name;

        public D3D12DeviceBuffer(D3D12GraphicsDevice gd, ref BufferDescription description)
        {
            this.gd = gd;
            SizeInBytes = description.SizeInBytes;
            Usage = description.Usage;
            sizeInBytes = description.SizeInBytes;
            _isDynamic = (description.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            _isStaging = (description.Usage & BufferUsage.Staging) == BufferUsage.Staging;
            _isDefault = !_isDynamic && !_isStaging;
            _dynamicSnapshotEnabled = _isDynamic
                && ((description.Usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer
                    || (description.Usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer);
            _dynamicSnapshotCapacity = _dynamicSnapshotEnabled
                ? calculateDynamicSnapshotCapacity(description.SizeInBytes)
                : description.SizeInBytes;

            ResourceDescription resourceDescription = ResourceDescription.Buffer(
                _dynamicSnapshotEnabled ? _dynamicSnapshotCapacity : description.SizeInBytes,
                getResourceFlags(description.Usage));
            if (_isStaging)
            {
                _stagingWriteBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Upload,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.GenericRead,
                    null);
                _stagingReadBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Readback,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.CopyDest,
                    null);
                _nativeBuffer = _stagingWriteBuffer;

                unsafe
                {
                    void* writePtr = null;
                    _stagingWriteBuffer.Map(0, &writePtr).CheckError();
                    _stagingWriteMappedPointer = (IntPtr)writePtr;

                    void* readPtr = null;
                    _stagingReadBuffer.Map(0, &readPtr).CheckError();
                    _stagingReadMappedPointer = (IntPtr)readPtr;
                }
            }
            else if (_isDynamic)
            {
                _nativeBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Upload,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.GenericRead,
                    null);
                unsafe
                {
                    void* dataPointer = null;
                    _nativeBuffer.Map(0, &dataPointer).CheckError();
                    _dynamicMappedPointer = (IntPtr)dataPointer;
                }
            }
            else
            {
                _nativeBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Default,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.Common,
                    null);
                CurrentState = ResourceStates.Common;
            }
        }

        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }
        internal ID3D12Resource NativeBuffer => _nativeBuffer;
        internal ulong GpuVirtualAddress => _nativeBuffer.GPUVirtualAddress;
        internal uint CurrentNativeSizeInBytes => (uint)Math.Min(uint.MaxValue, _nativeBuffer.Description.Width);
        internal ulong GetGpuVirtualAddress(uint offset) => _nativeBuffer.GPUVirtualAddress + ResolveNativeOffset(offset);
        internal uint GetBindableSize(uint offset) => offset < SizeInBytes ? SizeInBytes - offset : 0;
        internal uint ResolveNativeOffset(uint offset) => _dynamicSnapshotEnabled ? _dynamicSnapshotBaseOffset + offset : offset;
        internal ResourceStates CurrentState { get; set; }
        internal bool CanTransitionState => _isDefault;
        internal ulong BindVersion => _dynamicSnapshotEnabled ? _dynamicBindVersion : 0UL;
        public override bool IsDisposed => _disposed;

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
            }
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_isStaging)
            {
                _stagingWriteBuffer.Unmap(0);
                _stagingReadBuffer.Unmap(0);
                _stagingWriteBuffer.Dispose();
                _stagingReadBuffer.Dispose();
            }
            else if (_isDynamic)
            {
                _nativeBuffer.Unmap(0);
                _nativeBuffer.Dispose();
            }
            else
            {
                _nativeBuffer.Dispose();
            }

            _disposed = true;
        }

        internal ID3D12Resource Update(ID3D12GraphicsCommandList commandList, IntPtr source, uint destinationOffset, uint sizeInBytes)
        {
            if (destinationOffset + sizeInBytes > SizeInBytes)
            {
                throw new VeldridException("Buffer update range exceeds the destination buffer size.");
            }

            if (!_isDefault)
            {
                if (_dynamicSnapshotEnabled)
                {
                    updateDynamicSnapshot(source, destinationOffset, sizeInBytes);
                    return null;
                }

                writeCpuData(source, destinationOffset, sizeInBytes);
                return null;
            }

            ID3D12Resource uploadBuffer = createUploadBuffer(source, sizeInBytes);
            ResourceStates previousState = CurrentState;
            transition(commandList, previousState, ResourceStates.CopyDest);
            commandList.CopyBufferRegion(_nativeBuffer, destinationOffset, uploadBuffer, 0, sizeInBytes);
            transition(commandList, ResourceStates.CopyDest, previousState);
            CurrentState = previousState;
            return uploadBuffer;
        }

        private unsafe void updateDynamicSnapshot(IntPtr source, uint destinationOffset, uint copySize)
        {
            if (copySize == 0)
            {
                return;
            }

            uint snapshotSize = destinationOffset + copySize;
            if (snapshotSize == 0)
            {
                snapshotSize = 1;
            }

            if (snapshotSize > _dynamicSnapshotCapacity)
            {
                throw new VeldridException("Dynamic snapshot update exceeds snapshot buffer capacity.");
            }

            if (_dynamicSnapshotWriteHead + snapshotSize > _dynamicSnapshotCapacity)
            {
                _dynamicSnapshotWriteHead = 0;
            }

            uint newBaseOffset = _dynamicSnapshotWriteHead;
            byte* mappedPointer = (byte*)_dynamicMappedPointer.ToPointer();
            if (_dynamicSnapshotInitialized && newBaseOffset != _dynamicSnapshotBaseOffset)
            {
                // Preserve only the unchanged prefix when callers update a subrange.
                // Most high-frequency VB/IB updates write from offset 0, so this avoids
                // copying the full logical buffer every flush.
                if (destinationOffset > 0)
                {
                    uint prefixSize = destinationOffset;
                    byte* src = mappedPointer + _dynamicSnapshotBaseOffset;
                    byte* dst = mappedPointer + newBaseOffset;
                    Buffer.MemoryCopy(src, dst, snapshotSize, prefixSize);
                }
            }

            byte* destination = mappedPointer + newBaseOffset + destinationOffset;
            Buffer.MemoryCopy(source.ToPointer(), destination, snapshotSize - destinationOffset, copySize);

            uint previousBaseOffset = _dynamicSnapshotBaseOffset;
            _dynamicSnapshotBaseOffset = newBaseOffset;
            _dynamicSnapshotWriteHead = alignUp(newBaseOffset + snapshotSize, 16);
            _dynamicSnapshotInitialized = true;
            if (newBaseOffset != previousBaseOffset)
            {
                _dynamicBindVersion++;
            }
        }

        internal void CopyTo(ID3D12GraphicsCommandList commandList, D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint sizeInBytes)
        {
            if (sourceOffset + sizeInBytes > SizeInBytes || destinationOffset + sizeInBytes > destination.SizeInBytes)
            {
                throw new VeldridException("Buffer copy range exceeds buffer bounds.");
            }

            ID3D12Resource sourceResource = getCopySourceResource();
            ID3D12Resource destinationResource = destination.getCopyDestinationResource();
            if (sourceResource == null || destinationResource == null)
            {
                copyOnCpu(destination, sourceOffset, destinationOffset, sizeInBytes);
                return;
            }

            ResourceStates srcPrevious = CurrentState;
            ResourceStates dstPrevious = destination.CurrentState;
            if (CanTransitionState)
            {
                transition(commandList, CurrentState, ResourceStates.CopySource);
                CurrentState = ResourceStates.CopySource;
            }

            if (destination.CanTransitionState)
            {
                destination.transition(commandList, destination.CurrentState, ResourceStates.CopyDest);
                destination.CurrentState = ResourceStates.CopyDest;
            }

            commandList.CopyBufferRegion(destinationResource, destinationOffset, sourceResource, sourceOffset, sizeInBytes);

            if (CanTransitionState)
            {
                transition(commandList, CurrentState, srcPrevious);
                CurrentState = srcPrevious;
            }

            if (destination.CanTransitionState)
            {
                destination.transition(commandList, destination.CurrentState, dstPrevious);
                destination.CurrentState = dstPrevious;
            }

            if (destination._isStaging)
            {
                destination._stagingWriteBufferDirtyFromReadBuffer = true;
                destination._stagingReadBufferDirtyFromWriteBuffer = false;
            }
        }

        internal MappedResource Map(MapMode mode)
        {
            IntPtr pointer = getMapPointer(mode);
            _activeMapMode = mode;
            return new MappedResource(this, mode, pointer, sizeInBytes);
        }

        internal bool TryGetCpuReadPointer(out IntPtr pointer)
        {
            if (_isStaging)
            {
                ensureReadBufferIsCurrent();
                pointer = _stagingReadMappedPointer;
                return true;
            }

            if (_isDynamic)
            {
                pointer = _dynamicMappedPointer;
                return true;
            }

            pointer = IntPtr.Zero;
            return false;
        }

        internal void Unmap()
        {
            if (!_activeMapMode.HasValue)
            {
                return;
            }

            if (_isStaging)
            {
                if (_activeMapMode == MapMode.Write)
                {
                    _stagingReadBufferDirtyFromWriteBuffer = true;
                    _stagingWriteBufferDirtyFromReadBuffer = false;
                }
                else if (_activeMapMode == MapMode.ReadWrite)
                {
                    _stagingWriteBufferDirtyFromReadBuffer = true;
                    _stagingReadBufferDirtyFromWriteBuffer = false;
                    syncReadBufferToWriteBuffer();
                }
            }

            _activeMapMode = null;
        }

        private IntPtr getMapPointer(MapMode mode)
        {
            if (_isDynamic)
            {
                if (mode != MapMode.Write)
                {
                    throw new VeldridException("Dynamic D3D12 buffers only support MapMode.Write.");
                }

                return _dynamicMappedPointer;
            }

            if (_isStaging)
            {
                if (mode == MapMode.Read || mode == MapMode.ReadWrite)
                {
                    ensureReadBufferIsCurrent();
                    return _stagingReadMappedPointer;
                }

                return _stagingWriteMappedPointer;
            }

            throw new VeldridException("Only Dynamic or Staging buffers can be mapped.");
        }

        private unsafe void writeCpuData(IntPtr source, uint destinationOffset, uint copySize)
        {
            if (_isDynamic)
            {
                byte* dst = (byte*)_dynamicMappedPointer + destinationOffset;
                Buffer.MemoryCopy(source.ToPointer(), dst, SizeInBytes - destinationOffset, copySize);
                return;
            }

            if (_isStaging)
            {
                byte* dst = (byte*)_stagingWriteMappedPointer + destinationOffset;
                Buffer.MemoryCopy(source.ToPointer(), dst, SizeInBytes - destinationOffset, copySize);
                _stagingReadBufferDirtyFromWriteBuffer = true;
                _stagingWriteBufferDirtyFromReadBuffer = false;
                return;
            }

            throw new VeldridException("CPU updates on default D3D12 buffers require a command-list copy.");
        }

        private ID3D12Resource createUploadBuffer(IntPtr source, uint copySize)
        {
            ID3D12Resource uploadBuffer = gd.Device.CreateCommittedResource(
                HeapType.Upload,
                HeapFlags.None,
                ResourceDescription.Buffer(copySize),
                ResourceStates.GenericRead,
                null);

            unsafe
            {
                void* mapped = null;
                uploadBuffer.Map(0, &mapped).CheckError();
                try
                {
                    Buffer.MemoryCopy(source.ToPointer(), mapped, copySize, copySize);
                }
                finally
                {
                    uploadBuffer.Unmap(0, null);
                }
            }

            return uploadBuffer;
        }

        private unsafe void copyOnCpu(D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint copySize)
        {
            if (!TryGetCpuReadPointer(out IntPtr sourcePtr))
            {
                if (_isDefault)
                {
                    copyDefaultSourceToCpuWritableDestination(destination, sourceOffset, destinationOffset, copySize);
                    return;
                }

                throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported for GPU copy and no CPU source mapping is available.");
            }

            IntPtr destinationPtr;
            if (destination._isDynamic)
            {
                destinationPtr = destination._dynamicMappedPointer;
            }
            else if (destination._isStaging)
            {
                destinationPtr = destination._stagingWriteMappedPointer;
            }
            else
            {
                throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported because the destination cannot be CPU-written.");
            }

            byte* src = (byte*)sourcePtr + sourceOffset;
            byte* dst = (byte*)destinationPtr + destinationOffset;
            Buffer.MemoryCopy(src, dst, destination.SizeInBytes - destinationOffset, copySize);
            if (destination._isStaging)
            {
                destination._stagingReadBufferDirtyFromWriteBuffer = true;
                destination._stagingWriteBufferDirtyFromReadBuffer = false;
            }
        }

        private unsafe void copyDefaultSourceToCpuWritableDestination(D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint copySize)
        {
            IntPtr destinationPtr;
            if (destination._isDynamic)
            {
                destinationPtr = destination._dynamicMappedPointer;
            }
            else if (destination._isStaging)
            {
                destinationPtr = destination._stagingWriteMappedPointer;
            }
            else
            {
                throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported because the destination cannot be CPU-written.");
            }

            ID3D12Resource readbackBuffer = gd.Device.CreateCommittedResource(
                HeapType.Readback,
                HeapFlags.None,
                ResourceDescription.Buffer(copySize),
                ResourceStates.CopyDest,
                null);
            ID3D12CommandAllocator allocator = gd.Device.CreateCommandAllocator(CommandListType.Direct);
            ID3D12GraphicsCommandList commandList = gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator, null);
            try
            {
                ResourceStates previousState = CurrentState;
                if (CanTransitionState && previousState != ResourceStates.CopySource)
                {
                    ResourceBarrier toCopySource = ResourceBarrier.BarrierTransition(
                        _nativeBuffer,
                        previousState,
                        ResourceStates.CopySource,
                        Vortice.Direct3D12.D3D12.ResourceBarrierAllSubResources,
                        ResourceBarrierFlags.None);
                    commandList.ResourceBarrier(new[] { toCopySource });
                }

                commandList.CopyBufferRegion(readbackBuffer, 0, _nativeBuffer, sourceOffset, copySize);

                if (CanTransitionState && previousState != ResourceStates.CopySource)
                {
                    ResourceBarrier fromCopySource = ResourceBarrier.BarrierTransition(
                        _nativeBuffer,
                        ResourceStates.CopySource,
                        previousState,
                        Vortice.Direct3D12.D3D12.ResourceBarrierAllSubResources,
                        ResourceBarrierFlags.None);
                    commandList.ResourceBarrier(new[] { fromCopySource });
                }

                commandList.Close();
                gd.CommandQueue.ExecuteCommandList(commandList);
                gd.WaitForIdle();

                void* mapped = null;
                readbackBuffer.Map(0, &mapped).CheckError();
                try
                {
                    byte* src = (byte*)mapped;
                    byte* dst = (byte*)destinationPtr + destinationOffset;
                    Buffer.MemoryCopy(src, dst, destination.SizeInBytes - destinationOffset, copySize);
                }
                finally
                {
                    readbackBuffer.Unmap(0, null);
                }
            }
            finally
            {
                commandList.Dispose();
                allocator.Dispose();
                readbackBuffer.Dispose();
            }

            if (destination._isStaging)
            {
                destination._stagingReadBufferDirtyFromWriteBuffer = true;
                destination._stagingWriteBufferDirtyFromReadBuffer = false;
            }
        }

        private ID3D12Resource getCopySourceResource()
        {
            if (_isDefault || _isDynamic)
            {
                return _nativeBuffer;
            }

            if (_isStaging)
            {
                ensureWriteBufferIsCurrent();
                return _stagingWriteBuffer;
            }

            return null;
        }

        private ID3D12Resource getCopyDestinationResource()
        {
            if (_isDefault)
            {
                return _nativeBuffer;
            }

            if (_isStaging)
            {
                return _stagingReadBuffer;
            }

            return null;
        }

        private void ensureReadBufferIsCurrent()
        {
            if (!_isStaging || !_stagingReadBufferDirtyFromWriteBuffer)
            {
                return;
            }

            unsafe
            {
                Buffer.MemoryCopy(
                    _stagingWriteMappedPointer.ToPointer(),
                    _stagingReadMappedPointer.ToPointer(),
                    sizeInBytes,
                    sizeInBytes);
            }

            _stagingReadBufferDirtyFromWriteBuffer = false;
            _stagingWriteBufferDirtyFromReadBuffer = false;
        }

        private void ensureWriteBufferIsCurrent()
        {
            if (!_isStaging || !_stagingWriteBufferDirtyFromReadBuffer)
            {
                return;
            }

            syncReadBufferToWriteBuffer();
        }

        private void syncReadBufferToWriteBuffer()
        {
            unsafe
            {
                Buffer.MemoryCopy(
                    _stagingReadMappedPointer.ToPointer(),
                    _stagingWriteMappedPointer.ToPointer(),
                    sizeInBytes,
                    sizeInBytes);
            }

            _stagingWriteBufferDirtyFromReadBuffer = false;
            _stagingReadBufferDirtyFromWriteBuffer = false;
        }

        private void transition(ID3D12GraphicsCommandList commandList, ResourceStates from, ResourceStates to)
        {
            if (from == to || !CanTransitionState)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierTransition(
                _nativeBuffer,
                from,
                to,
                Vortice.Direct3D12.D3D12.ResourceBarrierAllSubResources,
                ResourceBarrierFlags.None);
            commandList.ResourceBarrier(new[] { barrier });
        }

        private static uint calculateDynamicSnapshotCapacity(uint logicalSize)
        {
            const ulong maxSnapshotBytes = 256UL * 1024UL * 1024UL;
            ulong doubled = (ulong)logicalSize * 2UL;
            ulong timesThirtyTwo = (ulong)logicalSize * 32UL;
            ulong desired = Math.Max(doubled, timesThirtyTwo);
            ulong capped = Math.Min(desired, maxSnapshotBytes);
            ulong finalSize = Math.Max((ulong)logicalSize, capped);
            if (finalSize > uint.MaxValue)
            {
                return uint.MaxValue;
            }

            return (uint)finalSize;
        }

        private static uint alignUp(uint value, uint alignment)
        {
            if (alignment == 0)
            {
                return value;
            }

            uint remainder = value % alignment;
            return remainder == 0 ? value : value + (alignment - remainder);
        }

        private static ResourceFlags getResourceFlags(BufferUsage usage)
        {
            if ((usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite)
            {
                return ResourceFlags.AllowUnorderedAccess;
            }

            return ResourceFlags.None;
        }
    }
}
