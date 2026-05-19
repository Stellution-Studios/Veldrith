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
            this._isDynamic = (description.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            this._isStaging = (description.Usage & BufferUsage.Staging) == BufferUsage.Staging;
            this._isDefault = !this._isDynamic && !this._isStaging;
            this._dynamicSnapshotEnabled = this._isDynamic
                && ((description.Usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer
                    || (description.Usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer);
            this._dynamicSnapshotCapacity = this._dynamicSnapshotEnabled
                ? CalculateDynamicSnapshotCapacity(description.SizeInBytes)
                : description.SizeInBytes;

            ResourceDescription resourceDescription = ResourceDescription.Buffer(
                this._dynamicSnapshotEnabled ? this._dynamicSnapshotCapacity : description.SizeInBytes,
                GetResourceFlags(description.Usage));
            if (this._isStaging)
            {
                this._stagingWriteBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Upload,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.GenericRead,
                    null);
                this._stagingReadBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Readback,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.CopyDest,
                    null);
                this._nativeBuffer = this._stagingWriteBuffer;

                unsafe
                {
                    void* writePtr = null;
                    this._stagingWriteBuffer.Map(0, &writePtr).CheckError();
                    this._stagingWriteMappedPointer = (IntPtr)writePtr;

                    void* readPtr = null;
                    this._stagingReadBuffer.Map(0, &readPtr).CheckError();
                    this._stagingReadMappedPointer = (IntPtr)readPtr;
                }
            }
            else if (this._isDynamic)
            {
                this._nativeBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Upload,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.GenericRead,
                    null);
                unsafe
                {
                    void* dataPointer = null;
                    this._nativeBuffer.Map(0, &dataPointer).CheckError();
                    this._dynamicMappedPointer = (IntPtr)dataPointer;
                }
            }
            else
            {
                this._nativeBuffer = gd.Device.CreateCommittedResource(
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
        internal ID3D12Resource NativeBuffer => this._nativeBuffer;
        internal ulong GpuVirtualAddress => this._nativeBuffer.GPUVirtualAddress;
        internal uint CurrentNativeSizeInBytes => (uint)Math.Min(uint.MaxValue, this._nativeBuffer.Description.Width);
        internal ulong GetGpuVirtualAddress(uint offset) => this._nativeBuffer.GPUVirtualAddress + ResolveNativeOffset(offset);
        internal uint GetBindableSize(uint offset) => offset < SizeInBytes ? SizeInBytes - offset : 0;
        internal uint ResolveNativeOffset(uint offset) => this._dynamicSnapshotEnabled ? this._dynamicSnapshotBaseOffset + offset : offset;
        internal ResourceStates CurrentState { get; set; }
        internal bool CanTransitionState => this._isDefault;
        internal ulong BindVersion => this._dynamicSnapshotEnabled ? this._dynamicBindVersion : 0UL;
        public override bool IsDisposed => this._disposed;

        public override string Name
        {
            get => this._name;
            set
            {
                this._name = value;
            }
        }

        public override void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            if (this._isStaging)
            {
                this._stagingWriteBuffer.Unmap(0);
                this._stagingReadBuffer.Unmap(0);
                this._stagingWriteBuffer.Dispose();
                this._stagingReadBuffer.Dispose();
            }
            else if (this._isDynamic)
            {
                this._nativeBuffer.Unmap(0);
                this._nativeBuffer.Dispose();
            }
            else
            {
                this._nativeBuffer.Dispose();
            }

            this._disposed = true;
        }

        internal ID3D12Resource Update(ID3D12GraphicsCommandList commandList, IntPtr source, uint destinationOffset, uint sizeInBytes)
        {
            if (destinationOffset + sizeInBytes > SizeInBytes)
            {
                throw new VeldridException("Buffer update range exceeds the destination buffer size.");
            }

            if (!this._isDefault)
            {
                if (this._dynamicSnapshotEnabled)
                {
                    UpdateDynamicSnapshot(source, destinationOffset, sizeInBytes);
                    return null;
                }

                WriteCpuData(source, destinationOffset, sizeInBytes);
                return null;
            }

            ID3D12Resource uploadBuffer = CreateUploadBuffer(source, sizeInBytes);
            ResourceStates previousState = CurrentState;
            Transition(commandList, previousState, ResourceStates.CopyDest);
            commandList.CopyBufferRegion(this._nativeBuffer, destinationOffset, uploadBuffer, 0, sizeInBytes);
            Transition(commandList, ResourceStates.CopyDest, previousState);
            CurrentState = previousState;
            return uploadBuffer;
        }

        private unsafe void UpdateDynamicSnapshot(IntPtr source, uint destinationOffset, uint copySize)
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

            if (snapshotSize > this._dynamicSnapshotCapacity)
            {
                throw new VeldridException("Dynamic snapshot update exceeds snapshot buffer capacity.");
            }

            if (this._dynamicSnapshotWriteHead + snapshotSize > this._dynamicSnapshotCapacity)
            {
                this._dynamicSnapshotWriteHead = 0;
            }

            uint newBaseOffset = this._dynamicSnapshotWriteHead;
            byte* mappedPointer = (byte*)this._dynamicMappedPointer.ToPointer();
            if (this._dynamicSnapshotInitialized && newBaseOffset != this._dynamicSnapshotBaseOffset)
            {
                // Preserve only the unchanged prefix when callers update a subrange.
                // Most high-frequency VB/IB updates write from offset 0, so this avoids
                // copying the full logical buffer every flush.
                if (destinationOffset > 0)
                {
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
            if (newBaseOffset != previousBaseOffset)
            {
                this._dynamicBindVersion++;
            }
        }

        internal void CopyTo(ID3D12GraphicsCommandList commandList, D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint sizeInBytes)
        {
            if (sourceOffset + sizeInBytes > SizeInBytes || destinationOffset + sizeInBytes > destination.SizeInBytes)
            {
                throw new VeldridException("Buffer copy range exceeds buffer bounds.");
            }

            ID3D12Resource sourceResource = GetCopySourceResource();
            ID3D12Resource destinationResource = destination.GetCopyDestinationResource();
            if (sourceResource == null || destinationResource == null)
            {
                CopyOnCpu(destination, sourceOffset, destinationOffset, sizeInBytes);
                return;
            }

            ResourceStates srcPrevious = CurrentState;
            ResourceStates dstPrevious = destination.CurrentState;
            if (CanTransitionState)
            {
                Transition(commandList, CurrentState, ResourceStates.CopySource);
                CurrentState = ResourceStates.CopySource;
            }

            if (destination.CanTransitionState)
            {
                destination.Transition(commandList, destination.CurrentState, ResourceStates.CopyDest);
                destination.CurrentState = ResourceStates.CopyDest;
            }

            commandList.CopyBufferRegion(destinationResource, destinationOffset, sourceResource, sourceOffset, sizeInBytes);

            if (CanTransitionState)
            {
                Transition(commandList, CurrentState, srcPrevious);
                CurrentState = srcPrevious;
            }

            if (destination.CanTransitionState)
            {
                destination.Transition(commandList, destination.CurrentState, dstPrevious);
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
            IntPtr pointer = GetMapPointer(mode);
            this._activeMapMode = mode;
            return new MappedResource(this, mode, pointer, sizeInBytes);
        }

        internal bool TryGetCpuReadPointer(out IntPtr pointer)
        {
            if (this._isStaging)
            {
                EnsureReadBufferIsCurrent();
                pointer = this._stagingReadMappedPointer;
                return true;
            }

            if (this._isDynamic)
            {
                pointer = this._dynamicMappedPointer;
                return true;
            }

            pointer = IntPtr.Zero;
            return false;
        }

        internal void Unmap()
        {
            if (!this._activeMapMode.HasValue)
            {
                return;
            }

            if (this._isStaging)
            {
                if (this._activeMapMode == MapMode.Write)
                {
                    this._stagingReadBufferDirtyFromWriteBuffer = true;
                    this._stagingWriteBufferDirtyFromReadBuffer = false;
                }
                else if (this._activeMapMode == MapMode.ReadWrite)
                {
                    this._stagingWriteBufferDirtyFromReadBuffer = true;
                    this._stagingReadBufferDirtyFromWriteBuffer = false;
                    SyncReadBufferToWriteBuffer();
                }
            }

            this._activeMapMode = null;
        }

        private IntPtr GetMapPointer(MapMode mode)
        {
            if (this._isDynamic)
            {
                if (mode != MapMode.Write)
                {
                    throw new VeldridException("Dynamic D3D12 buffers only support MapMode.Write.");
                }

                return this._dynamicMappedPointer;
            }

            if (this._isStaging)
            {
                if (mode == MapMode.Read || mode == MapMode.ReadWrite)
                {
                    EnsureReadBufferIsCurrent();
                    return this._stagingReadMappedPointer;
                }

                return this._stagingWriteMappedPointer;
            }

            throw new VeldridException("Only Dynamic or Staging buffers can be mapped.");
        }

        private unsafe void WriteCpuData(IntPtr source, uint destinationOffset, uint copySize)
        {
            if (this._isDynamic)
            {
                byte* dst = (byte*)this._dynamicMappedPointer + destinationOffset;
                Buffer.MemoryCopy(source.ToPointer(), dst, SizeInBytes - destinationOffset, copySize);
                return;
            }

            if (this._isStaging)
            {
                byte* dst = (byte*)this._stagingWriteMappedPointer + destinationOffset;
                Buffer.MemoryCopy(source.ToPointer(), dst, SizeInBytes - destinationOffset, copySize);
                this._stagingReadBufferDirtyFromWriteBuffer = true;
                this._stagingWriteBufferDirtyFromReadBuffer = false;
                return;
            }

            throw new VeldridException("CPU updates on default D3D12 buffers require a command-list copy.");
        }

        private ID3D12Resource CreateUploadBuffer(IntPtr source, uint copySize)
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

        private unsafe void CopyOnCpu(D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint copySize)
        {
            if (!TryGetCpuReadPointer(out IntPtr sourcePtr))
            {
                if (this._isDefault)
                {
                    CopyDefaultSourceToCpuWritableDestination(destination, sourceOffset, destinationOffset, copySize);
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

        private unsafe void CopyDefaultSourceToCpuWritableDestination(D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint copySize)
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
                        this._nativeBuffer,
                        previousState,
                        ResourceStates.CopySource,
                        Vortice.Direct3D12.D3D12.ResourceBarrierAllSubResources,
                        ResourceBarrierFlags.None);
                    commandList.ResourceBarrier(new[] { toCopySource });
                }

                commandList.CopyBufferRegion(readbackBuffer, 0, this._nativeBuffer, sourceOffset, copySize);

                if (CanTransitionState && previousState != ResourceStates.CopySource)
                {
                    ResourceBarrier fromCopySource = ResourceBarrier.BarrierTransition(
                        this._nativeBuffer,
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

        private ID3D12Resource GetCopySourceResource()
        {
            if (this._isDefault || this._isDynamic)
            {
                return this._nativeBuffer;
            }

            if (this._isStaging)
            {
                EnsureWriteBufferIsCurrent();
                return this._stagingWriteBuffer;
            }

            return null;
        }

        private ID3D12Resource GetCopyDestinationResource()
        {
            if (this._isDefault)
            {
                return this._nativeBuffer;
            }

            if (this._isStaging)
            {
                return this._stagingReadBuffer;
            }

            return null;
        }

        private void EnsureReadBufferIsCurrent()
        {
            if (!this._isStaging || !this._stagingReadBufferDirtyFromWriteBuffer)
            {
                return;
            }

            unsafe
            {
                Buffer.MemoryCopy(
                    this._stagingWriteMappedPointer.ToPointer(),
                    this._stagingReadMappedPointer.ToPointer(),
                    sizeInBytes,
                    sizeInBytes);
            }

            this._stagingReadBufferDirtyFromWriteBuffer = false;
            this._stagingWriteBufferDirtyFromReadBuffer = false;
        }

        private void EnsureWriteBufferIsCurrent()
        {
            if (!this._isStaging || !this._stagingWriteBufferDirtyFromReadBuffer)
            {
                return;
            }

            SyncReadBufferToWriteBuffer();
        }

        private void SyncReadBufferToWriteBuffer()
        {
            unsafe
            {
                Buffer.MemoryCopy(
                    this._stagingReadMappedPointer.ToPointer(),
                    this._stagingWriteMappedPointer.ToPointer(),
                    sizeInBytes,
                    sizeInBytes);
            }

            this._stagingWriteBufferDirtyFromReadBuffer = false;
            this._stagingReadBufferDirtyFromWriteBuffer = false;
        }

        private void Transition(ID3D12GraphicsCommandList commandList, ResourceStates from, ResourceStates to)
        {
            if (from == to || !CanTransitionState)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierTransition(
                this._nativeBuffer,
                from,
                to,
                Vortice.Direct3D12.D3D12.ResourceBarrierAllSubResources,
                ResourceBarrierFlags.None);
            commandList.ResourceBarrier(new[] { barrier });
        }

        private static uint CalculateDynamicSnapshotCapacity(uint logicalSize)
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

        private static uint AlignUp(uint value, uint alignment)
        {
            if (alignment == 0)
            {
                return value;
            }

            uint remainder = value % alignment;
            return remainder == 0 ? value : value + (alignment - remainder);
        }

        private static ResourceFlags GetResourceFlags(BufferUsage usage)
        {
            if ((usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite)
            {
                return ResourceFlags.AllowUnorderedAccess;
            }

            return ResourceFlags.None;
        }
    }
}
