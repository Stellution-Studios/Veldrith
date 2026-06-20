using System;
using System.Runtime.CompilerServices;

namespace Veldrith.D3D12;

/// <summary>
/// Tracks the native upload-ring snapshots used by frequently updated dynamic D3D12 buffers.
/// </summary>
internal sealed class D3D12DynamicBufferSnapshotState {

    /// <summary>
    /// Stores the maximum backing allocation size used by the dynamic snapshot ring.
    /// </summary>
    private const ulong MaxSnapshotBytes = 256UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the persistently mapped upload memory for the dynamic buffer allocation.
    /// </summary>
    private readonly IntPtr _mappedPointer;

    /// <summary>
    /// Stores the total native byte capacity of the snapshot ring.
    /// </summary>
    private readonly uint _capacity;

    /// <summary>
    /// Stores the byte alignment used between snapshot slots.
    /// </summary>
    private readonly uint _alignment;

    /// <summary>
    /// Stores the reserved byte size of one logical snapshot slot.
    /// </summary>
    private readonly uint _slotSize;

    /// <summary>
    /// Stores the version observed by bind caches when the native base offset changes.
    /// </summary>
    private ulong _bindVersion;

    /// <summary>
    /// Stores the native byte offset of the currently visible logical buffer snapshot.
    /// </summary>
    private uint _baseOffset;

    /// <summary>
    /// Stores whether a snapshot slot has been initialized.
    /// </summary>
    private bool _initialized;

    /// <summary>
    /// Stores the native byte offset considered for the next snapshot allocation.
    /// </summary>
    private uint _writeHead;

    /// <summary>
    /// Stores the logical end of writes recorded into the current snapshot.
    /// </summary>
    private uint _writtenEnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DynamicBufferSnapshotState" /> class.
    /// </summary>
    /// <param name="mappedPointer">The persistently mapped upload memory.</param>
    /// <param name="logicalSize">The logical buffer size in bytes.</param>
    /// <param name="usage">The buffer usage flags.</param>
    internal D3D12DynamicBufferSnapshotState(IntPtr mappedPointer, uint logicalSize, BufferUsage usage) {
        this._mappedPointer = mappedPointer;
        this._alignment = GetSnapshotAlignment(usage);
        this._slotSize = AlignUp(logicalSize, this._alignment);
        this._capacity = CalculateCapacity(logicalSize, usage);
    }

    /// <summary>
    /// Gets the version used by command-list bind caches.
    /// </summary>
    internal ulong BindVersion => this._bindVersion;

    /// <summary>
    /// Gets the source-copy byte count from the most recent update.
    /// </summary>
    internal uint LastCopyBytes { get; private set; }

    /// <summary>
    /// Gets the prefix-copy byte count from the most recent update.
    /// </summary>
    internal uint LastPrefixCopyBytes { get; private set; }

    /// <summary>
    /// Gets whether the most recent update moved to a different native snapshot slot.
    /// </summary>
    internal bool LastRotated { get; private set; }

    /// <summary>
    /// Gets whether the supplied usage should use dynamic snapshots.
    /// </summary>
    /// <param name="usage">The buffer usage flags.</param>
    /// <returns><see langword="true" /> when native snapshots should be used.</returns>
    internal static bool IsSupported(BufferUsage usage) {
        return (usage & BufferUsage.Dynamic) == BufferUsage.Dynamic
               && (((usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer)
                   || ((usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer)
                   || ((usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer)
                   || ((usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly));
    }

    /// <summary>
    /// Calculates the native backing allocation size for a dynamic buffer.
    /// </summary>
    /// <param name="logicalSize">The logical buffer size in bytes.</param>
    /// <param name="usage">The buffer usage flags.</param>
    /// <returns>The native byte capacity to allocate.</returns>
    internal static uint CalculateCapacity(uint logicalSize, BufferUsage usage) {
        uint alignment = GetSnapshotAlignment(usage);
        uint minimumSnapshotCount = GetMinimumSnapshotCount(logicalSize, usage);
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

        ulong capped = Math.Min(Math.Max(desired, minimumSize), MaxSnapshotBytes);
        ulong finalSize = Math.Max(alignedLogicalSize, capped);
        if (finalSize > uint.MaxValue) {
            return uint.MaxValue;
        }

        return (uint)finalSize;
    }

    /// <summary>
    /// Gets the minimum number of logical snapshots reserved in a dynamic buffer.
    /// </summary>
    /// <param name="logicalSize">The logical buffer size in bytes.</param>
    /// <param name="usage">The buffer usage flags.</param>
    /// <returns>The minimum snapshot count.</returns>
    private static uint GetMinimumSnapshotCount(uint logicalSize, BufferUsage usage) {
        if ((usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer) {
            return 1024u;
        }

        if (logicalSize <= 64UL * 1024UL) {
            return 1024u;
        }

        if (logicalSize <= 1UL * 1024UL * 1024UL) {
            return 1024u;
        }

        return 128u;
    }

    /// <summary>
    /// Resolves a logical byte offset to the current native snapshot byte offset.
    /// </summary>
    /// <param name="offset">The logical byte offset.</param>
    /// <returns>The native byte offset.</returns>
    internal uint ResolveNativeOffset(uint offset) {
        return this._baseOffset + offset;
    }

    /// <summary>
    /// Updates the current snapshot and rotates to a new slot when overwriting earlier bytes.
    /// </summary>
    /// <param name="source">The source data.</param>
    /// <param name="destinationOffset">The logical destination byte offset.</param>
    /// <param name="copySize">The byte count to copy.</param>
    internal unsafe void Update(IntPtr source, uint destinationOffset, uint copySize) {
        this.LastCopyBytes = 0;
        this.LastPrefixCopyBytes = 0;
        this.LastRotated = false;

        if (copySize == 0) {
            return;
        }

        uint writtenEnd = destinationOffset + copySize;
        if (writtenEnd > this._slotSize || this._slotSize > this._capacity) {
            throw new VeldridException("Dynamic snapshot update exceeds snapshot buffer capacity.");
        }

        byte* mappedPointer = (byte*)this._mappedPointer.ToPointer();
        if (!this._initialized || destinationOffset < this._writtenEnd) {
            uint previousBaseOffset = this._baseOffset;
            uint newBaseOffset = this.AllocateSlot();
            if (this._initialized && newBaseOffset != previousBaseOffset && destinationOffset > 0) {
                byte* src = mappedPointer + previousBaseOffset;
                byte* dst = mappedPointer + newBaseOffset;
                CopyMemory(src, dst, destinationOffset);
                this.LastPrefixCopyBytes = destinationOffset;
            }

            this._baseOffset = newBaseOffset;
            this._writeHead = AlignUp((uint)((ulong)newBaseOffset + this._slotSize), this._alignment);
            this._writtenEnd = 0;
            this._initialized = true;
            this.LastRotated = newBaseOffset != previousBaseOffset;
            if (newBaseOffset != previousBaseOffset) {
                this._bindVersion++;
            }
        }

        byte* destination = mappedPointer + this._baseOffset + destinationOffset;
        CopyMemory(source.ToPointer(), destination, copySize);
        this._writtenEnd = Math.Max(this._writtenEnd, writtenEnd);
        this.LastCopyBytes = copySize;
    }

    /// <summary>
    /// Reserves a logical snapshot slot in the native ring.
    /// </summary>
    /// <returns>The native byte offset of the reserved slot.</returns>
    private uint AllocateSlot() {
        if ((ulong)this._writeHead + this._slotSize > this._capacity) {
            this._writeHead = 0;
        }

        return this._writeHead;
    }

    /// <summary>
    /// Gets the byte alignment used by the dynamic snapshot ring.
    /// </summary>
    /// <param name="usage">The buffer usage flags.</param>
    /// <returns>The alignment in bytes.</returns>
    private static uint GetSnapshotAlignment(BufferUsage usage) {
        return (usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer ? 256u : 16u;
    }

    /// <summary>
    /// Aligns a byte count up to the requested alignment.
    /// </summary>
    /// <param name="value">The value to align.</param>
    /// <param name="alignment">The requested alignment.</param>
    /// <returns>The aligned value.</returns>
    private static uint AlignUp(uint value, uint alignment) {
        if (alignment == 0) {
            return value;
        }

        uint remainder = value % alignment;
        return remainder == 0 ? value : value + (alignment - remainder);
    }

    /// <summary>
    /// Aligns a byte count up to the requested alignment.
    /// </summary>
    /// <param name="value">The value to align.</param>
    /// <param name="alignment">The requested alignment.</param>
    /// <returns>The aligned value.</returns>
    private static ulong AlignUp(ulong value, uint alignment) {
        if (alignment == 0) {
            return value;
        }

        ulong remainder = value % alignment;
        return remainder == 0 ? value : value + (alignment - remainder);
    }

    /// <summary>
    /// Copies an update payload without alignment requirements.
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
}
