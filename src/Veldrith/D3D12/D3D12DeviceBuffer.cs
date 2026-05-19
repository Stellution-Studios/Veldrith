using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12
{
    internal sealed class D3D12DeviceBuffer : DeviceBuffer
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly bool isDynamic;
        private readonly bool isStaging;
        private readonly bool isDefault;
        private readonly bool dynamicSnapshotEnabled;
        private readonly uint dynamicSnapshotCapacity;
        private readonly uint sizeInBytes;
        private ID3D12Resource nativeBuffer;
        private readonly ID3D12Resource stagingWriteBuffer;
        private readonly ID3D12Resource stagingReadBuffer;
        private IntPtr dynamicMappedPointer;
        private readonly IntPtr stagingWriteMappedPointer;
        private readonly IntPtr stagingReadMappedPointer;
        private bool stagingReadBufferDirtyFromWriteBuffer;
        private bool stagingWriteBufferDirtyFromReadBuffer;
        private uint dynamicSnapshotWriteHead;
        private uint dynamicSnapshotBaseOffset;
        private bool dynamicSnapshotInitialized;
        private ulong dynamicBindVersion;
        private MapMode? activeMapMode;
        private bool disposed;
        private string name;

        public D3D12DeviceBuffer(D3D12GraphicsDevice gd, ref BufferDescription description)
        {
            this.gd = gd;
            SizeInBytes = description.SizeInBytes;
            Usage = description.Usage;
            sizeInBytes = description.SizeInBytes;
            isDynamic = (description.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            isStaging = (description.Usage & BufferUsage.Staging) == BufferUsage.Staging;
            isDefault = !isDynamic && !isStaging;
            dynamicSnapshotEnabled = isDynamic
                && ((description.Usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer
                    || (description.Usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer);
            dynamicSnapshotCapacity = dynamicSnapshotEnabled
                ? calculateDynamicSnapshotCapacity(description.SizeInBytes)
                : description.SizeInBytes;

            ResourceDescription resourceDescription = ResourceDescription.Buffer(
                dynamicSnapshotEnabled ? dynamicSnapshotCapacity : description.SizeInBytes,
                getResourceFlags(description.Usage));
            if (isStaging)
            {
                stagingWriteBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Upload,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.GenericRead,
                    null);
                stagingReadBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Readback,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.CopyDest,
                    null);
                nativeBuffer = stagingWriteBuffer;

                unsafe
                {
                    void* writePtr = null;
                    stagingWriteBuffer.Map(0, &writePtr).CheckError();
                    stagingWriteMappedPointer = (IntPtr)writePtr;

                    void* readPtr = null;
                    stagingReadBuffer.Map(0, &readPtr).CheckError();
                    stagingReadMappedPointer = (IntPtr)readPtr;
                }
            }
            else if (isDynamic)
            {
                nativeBuffer = gd.Device.CreateCommittedResource(
                    HeapType.Upload,
                    HeapFlags.None,
                    resourceDescription,
                    ResourceStates.GenericRead,
                    null);
                unsafe
                {
                    void* dataPointer = null;
                    nativeBuffer.Map(0, &dataPointer).CheckError();
                    dynamicMappedPointer = (IntPtr)dataPointer;
                }
            }
            else
            {
                nativeBuffer = gd.Device.CreateCommittedResource(
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
        internal ID3D12Resource NativeBuffer => nativeBuffer;
        internal ulong GpuVirtualAddress => nativeBuffer.GPUVirtualAddress;
        internal uint CurrentNativeSizeInBytes => (uint)Math.Min(uint.MaxValue, nativeBuffer.Description.Width);
        internal ulong GetGpuVirtualAddress(uint offset) => nativeBuffer.GPUVirtualAddress + ResolveNativeOffset(offset);
        internal uint GetBindableSize(uint offset) => offset < SizeInBytes ? SizeInBytes - offset : 0;
        internal uint ResolveNativeOffset(uint offset) => dynamicSnapshotEnabled ? dynamicSnapshotBaseOffset + offset : offset;
        internal ResourceStates CurrentState { get; set; }
        internal bool CanTransitionState => isDefault;
        internal ulong BindVersion => dynamicSnapshotEnabled ? dynamicBindVersion : 0UL;
        public override bool IsDisposed => disposed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
            }
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (isStaging)
            {
                stagingWriteBuffer.Unmap(0);
                stagingReadBuffer.Unmap(0);
                stagingWriteBuffer.Dispose();
                stagingReadBuffer.Dispose();
            }
            else if (isDynamic)
            {
                nativeBuffer.Unmap(0);
                nativeBuffer.Dispose();
            }
            else
            {
                nativeBuffer.Dispose();
            }

            disposed = true;
        }

        internal ID3D12Resource Update(ID3D12GraphicsCommandList commandList, IntPtr source, uint destinationOffset, uint sizeInBytes)
        {
            if (destinationOffset + sizeInBytes > SizeInBytes)
            {
                throw new VeldridException("Buffer update range exceeds the destination buffer size.");
            }

            if (!isDefault)
            {
                if (dynamicSnapshotEnabled)
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
            commandList.CopyBufferRegion(nativeBuffer, destinationOffset, uploadBuffer, 0, sizeInBytes);
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

            if (snapshotSize > dynamicSnapshotCapacity)
            {
                throw new VeldridException("Dynamic snapshot update exceeds snapshot buffer capacity.");
            }

            if (dynamicSnapshotWriteHead + snapshotSize > dynamicSnapshotCapacity)
            {
                dynamicSnapshotWriteHead = 0;
            }

            uint newBaseOffset = dynamicSnapshotWriteHead;
            byte* mappedPointer = (byte*)dynamicMappedPointer.ToPointer();
            if (dynamicSnapshotInitialized && newBaseOffset != dynamicSnapshotBaseOffset)
            {
                // Preserve only the unchanged prefix when callers update a subrange.
                // Most high-frequency VB/IB updates write from offset 0, so this avoids
                // copying the full logical buffer every flush.
                if (destinationOffset > 0)
                {
                    uint prefixSize = destinationOffset;
                    byte* src = mappedPointer + dynamicSnapshotBaseOffset;
                    byte* dst = mappedPointer + newBaseOffset;
                    Buffer.MemoryCopy(src, dst, snapshotSize, prefixSize);
                }
            }

            byte* destination = mappedPointer + newBaseOffset + destinationOffset;
            Buffer.MemoryCopy(source.ToPointer(), destination, snapshotSize - destinationOffset, copySize);

            uint previousBaseOffset = dynamicSnapshotBaseOffset;
            dynamicSnapshotBaseOffset = newBaseOffset;
            dynamicSnapshotWriteHead = alignUp(newBaseOffset + snapshotSize, 16);
            dynamicSnapshotInitialized = true;
            if (newBaseOffset != previousBaseOffset)
            {
                dynamicBindVersion++;
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

            if (destination.isStaging)
            {
                destination.stagingWriteBufferDirtyFromReadBuffer = true;
                destination.stagingReadBufferDirtyFromWriteBuffer = false;
            }
        }

        internal MappedResource Map(MapMode mode)
        {
            IntPtr pointer = getMapPointer(mode);
            activeMapMode = mode;
            return new MappedResource(this, mode, pointer, sizeInBytes);
        }

        internal bool TryGetCpuReadPointer(out IntPtr pointer)
        {
            if (isStaging)
            {
                ensureReadBufferIsCurrent();
                pointer = stagingReadMappedPointer;
                return true;
            }

            if (isDynamic)
            {
                pointer = dynamicMappedPointer;
                return true;
            }

            pointer = IntPtr.Zero;
            return false;
        }

        internal void Unmap()
        {
            if (!activeMapMode.HasValue)
            {
                return;
            }

            if (isStaging)
            {
                if (activeMapMode == MapMode.Write)
                {
                    stagingReadBufferDirtyFromWriteBuffer = true;
                    stagingWriteBufferDirtyFromReadBuffer = false;
                }
                else if (activeMapMode == MapMode.ReadWrite)
                {
                    stagingWriteBufferDirtyFromReadBuffer = true;
                    stagingReadBufferDirtyFromWriteBuffer = false;
                    syncReadBufferToWriteBuffer();
                }
            }

            activeMapMode = null;
        }

        private IntPtr getMapPointer(MapMode mode)
        {
            if (isDynamic)
            {
                if (mode != MapMode.Write)
                {
                    throw new VeldridException("Dynamic D3D12 buffers only support MapMode.Write.");
                }

                return dynamicMappedPointer;
            }

            if (isStaging)
            {
                if (mode == MapMode.Read || mode == MapMode.ReadWrite)
                {
                    ensureReadBufferIsCurrent();
                    return stagingReadMappedPointer;
                }

                return stagingWriteMappedPointer;
            }

            throw new VeldridException("Only Dynamic or Staging buffers can be mapped.");
        }

        private unsafe void writeCpuData(IntPtr source, uint destinationOffset, uint copySize)
        {
            if (isDynamic)
            {
                byte* dst = (byte*)dynamicMappedPointer + destinationOffset;
                Buffer.MemoryCopy(source.ToPointer(), dst, SizeInBytes - destinationOffset, copySize);
                return;
            }

            if (isStaging)
            {
                byte* dst = (byte*)stagingWriteMappedPointer + destinationOffset;
                Buffer.MemoryCopy(source.ToPointer(), dst, SizeInBytes - destinationOffset, copySize);
                stagingReadBufferDirtyFromWriteBuffer = true;
                stagingWriteBufferDirtyFromReadBuffer = false;
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
                if (isDefault)
                {
                    copyDefaultSourceToCpuWritableDestination(destination, sourceOffset, destinationOffset, copySize);
                    return;
                }

                throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported for GPU copy and no CPU source mapping is available.");
            }

            IntPtr destinationPtr;
            if (destination.isDynamic)
            {
                destinationPtr = destination.dynamicMappedPointer;
            }
            else if (destination.isStaging)
            {
                destinationPtr = destination.stagingWriteMappedPointer;
            }
            else
            {
                throw new PlatformNotSupportedException("This D3D12 buffer copy direction is unsupported because the destination cannot be CPU-written.");
            }

            byte* src = (byte*)sourcePtr + sourceOffset;
            byte* dst = (byte*)destinationPtr + destinationOffset;
            Buffer.MemoryCopy(src, dst, destination.SizeInBytes - destinationOffset, copySize);
            if (destination.isStaging)
            {
                destination.stagingReadBufferDirtyFromWriteBuffer = true;
                destination.stagingWriteBufferDirtyFromReadBuffer = false;
            }
        }

        private unsafe void copyDefaultSourceToCpuWritableDestination(D3D12DeviceBuffer destination, uint sourceOffset, uint destinationOffset, uint copySize)
        {
            IntPtr destinationPtr;
            if (destination.isDynamic)
            {
                destinationPtr = destination.dynamicMappedPointer;
            }
            else if (destination.isStaging)
            {
                destinationPtr = destination.stagingWriteMappedPointer;
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
                        nativeBuffer,
                        previousState,
                        ResourceStates.CopySource,
                        Vortice.Direct3D12.D3D12.ResourceBarrierAllSubResources,
                        ResourceBarrierFlags.None);
                    commandList.ResourceBarrier(new[] { toCopySource });
                }

                commandList.CopyBufferRegion(readbackBuffer, 0, nativeBuffer, sourceOffset, copySize);

                if (CanTransitionState && previousState != ResourceStates.CopySource)
                {
                    ResourceBarrier fromCopySource = ResourceBarrier.BarrierTransition(
                        nativeBuffer,
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

            if (destination.isStaging)
            {
                destination.stagingReadBufferDirtyFromWriteBuffer = true;
                destination.stagingWriteBufferDirtyFromReadBuffer = false;
            }
        }

        private ID3D12Resource getCopySourceResource()
        {
            if (isDefault || isDynamic)
            {
                return nativeBuffer;
            }

            if (isStaging)
            {
                ensureWriteBufferIsCurrent();
                return stagingWriteBuffer;
            }

            return null;
        }

        private ID3D12Resource getCopyDestinationResource()
        {
            if (isDefault)
            {
                return nativeBuffer;
            }

            if (isStaging)
            {
                return stagingReadBuffer;
            }

            return null;
        }

        private void ensureReadBufferIsCurrent()
        {
            if (!isStaging || !stagingReadBufferDirtyFromWriteBuffer)
            {
                return;
            }

            unsafe
            {
                Buffer.MemoryCopy(
                    stagingWriteMappedPointer.ToPointer(),
                    stagingReadMappedPointer.ToPointer(),
                    sizeInBytes,
                    sizeInBytes);
            }

            stagingReadBufferDirtyFromWriteBuffer = false;
            stagingWriteBufferDirtyFromReadBuffer = false;
        }

        private void ensureWriteBufferIsCurrent()
        {
            if (!isStaging || !stagingWriteBufferDirtyFromReadBuffer)
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
                    stagingReadMappedPointer.ToPointer(),
                    stagingWriteMappedPointer.ToPointer(),
                    sizeInBytes,
                    sizeInBytes);
            }

            stagingWriteBufferDirtyFromReadBuffer = false;
            stagingReadBufferDirtyFromWriteBuffer = false;
        }

        private void transition(ID3D12GraphicsCommandList commandList, ResourceStates from, ResourceStates to)
        {
            if (from == to || !CanTransitionState)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierTransition(
                nativeBuffer,
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
