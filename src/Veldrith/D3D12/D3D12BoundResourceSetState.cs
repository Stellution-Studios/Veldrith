using System;

namespace Veldrith.D3D12;

/// <summary>
/// Tracks bound D3D12 resource sets and dirty ranges for one pipeline bind point.
/// </summary>
internal sealed class D3D12BoundResourceSetState {

    /// <summary>
    /// Stores the bound resource sets for this bind point.
    /// </summary>
    internal BoundResourceSetInfo[] BoundSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Tracks which bound resource set slots must be rebound.
    /// </summary>
    internal bool[] Changed = Array.Empty<bool>();

    /// <summary>
    /// Gets whether any resource set slot is dirty.
    /// </summary>
    internal bool Dirty { get; private set; }

    /// <summary>
    /// Gets the first dirty slot, or -1 when no slot is dirty.
    /// </summary>
    internal int ChangedStart { get; private set; } = -1;

    /// <summary>
    /// Gets the last dirty slot, or -1 when no slot is dirty.
    /// </summary>
    internal int ChangedEnd { get; private set; } = -1;

    /// <summary>
    /// Updates a resource set slot and marks it dirty when the binding changed.
    /// </summary>
    /// <param name="slot">The resource set slot.</param>
    /// <param name="set">The resource set to bind.</param>
    /// <param name="dynamicOffsetsCount">The number of dynamic offsets supplied for the set.</param>
    /// <param name="dynamicOffsets">The first dynamic offset value.</param>
    /// <returns><see langword="true" /> when the slot changed.</returns>
    internal bool TrySet(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        this.EnsureCapacity(slot + 1);
        BoundResourceSetInfo previousBinding = this.BoundSets[slot];
        if (previousBinding.Equals(set, dynamicOffsetsCount, ref dynamicOffsets)) {
            return false;
        }

        this.BoundSets[slot].Offsets.Dispose();
        this.BoundSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
        this.MarkChanged(slot);
        return true;
    }

    /// <summary>
    /// Ensures the state arrays can hold the requested number of set slots.
    /// </summary>
    /// <param name="count">The minimum required slot count.</param>
    internal void EnsureCapacity(uint count) {
        Util.EnsureArrayMinimumSize(ref this.BoundSets, count);
        Util.EnsureArrayMinimumSize(ref this.Changed, count);
    }

    /// <summary>
    /// Marks a single set slot dirty.
    /// </summary>
    /// <param name="slot">The slot to mark.</param>
    internal void MarkChanged(uint slot) {
        int index = (int)slot;
        this.Changed[index] = true;
        this.Dirty = true;
        if (this.ChangedStart < 0 || index < this.ChangedStart) {
            this.ChangedStart = index;
        }

        if (index > this.ChangedEnd) {
            this.ChangedEnd = index;
        }
    }

    /// <summary>
    /// Captures a copy of one bound resource-set slot so temporary bindings can be restored later.
    /// </summary>
    /// <param name="slot">The slot to capture.</param>
    /// <returns>A copied binding whose offset storage is owned by the caller.</returns>
    internal BoundResourceSetInfo CaptureSlot(uint slot) {
        if (slot >= this.BoundSets.Length || this.BoundSets[slot].Set == null) {
            uint dummyOffset = 0;
            return new BoundResourceSetInfo(null, 0, ref dummyOffset);
        }

        BoundResourceSetInfo source = this.BoundSets[slot];
        uint offsetCount = source.Offsets.Count;
        if (offsetCount == 0) {
            uint dummyOffset = 0;
            return new BoundResourceSetInfo(source.Set, 0, ref dummyOffset);
        }

        uint[] offsets = new uint[offsetCount];
        for (uint i = 0; i < offsetCount; i++) {
            offsets[i] = source.Offsets.Get(i);
        }

        return new BoundResourceSetInfo(source.Set, offsetCount, ref offsets[0]);
    }

    /// <summary>
    /// Restores one bound resource-set slot and marks it dirty for the next flush.
    /// </summary>
    /// <param name="slot">The slot to restore.</param>
    /// <param name="binding">The binding captured by <see cref="CaptureSlot" />.</param>
    internal void RestoreSlot(uint slot, BoundResourceSetInfo binding) {
        this.EnsureCapacity(slot + 1);
        this.BoundSets[slot].Offsets.Dispose();
        this.BoundSets[slot] = binding;
        this.MarkChanged(slot);
    }

    /// <summary>
    /// Marks all currently bound slots dirty up to the active pipeline's set count.
    /// </summary>
    /// <param name="resourceSetCount">The active pipeline resource set count.</param>
    internal void MarkBoundChanged(uint resourceSetCount) {
        int count = Math.Min(Math.Min(this.BoundSets.Length, this.Changed.Length), GetClampedResourceSetCount(resourceSetCount));
        for (uint slot = 0; slot < count; slot++) {
            if (this.BoundSets[slot].Set != null) {
                this.MarkChanged(slot);
            }
        }
    }

    /// <summary>
    /// Gets the final slot index that can be flushed for the active pipeline.
    /// </summary>
    /// <param name="resourceSetCount">The active pipeline resource set count.</param>
    /// <returns>The final flushable slot index, or -1 when no slot can be flushed.</returns>
    internal int GetFlushEnd(uint resourceSetCount) {
        int count = Math.Min(Math.Min(this.BoundSets.Length, this.Changed.Length), GetClampedResourceSetCount(resourceSetCount));
        return count - 1;
    }

    /// <summary>
    /// Clears the dirty range after changed slots have been flushed.
    /// </summary>
    internal void ResetDirtyRange() {
        this.Dirty = false;
        this.ChangedStart = -1;
        this.ChangedEnd = -1;
    }

    /// <summary>
    /// Marks bound resource sets dirty when they reference a dynamic buffer whose GPU address changed.
    /// </summary>
    /// <param name="resourceSetCount">The active pipeline resource set count.</param>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    /// <returns><see langword="true" /> when at least one set was marked dirty.</returns>
    internal bool MarkSetsReferencingBufferDirty(uint resourceSetCount, D3D12DeviceBuffer buffer) {
        int count = Math.Min(Math.Min(this.BoundSets.Length, this.Changed.Length), GetClampedResourceSetCount(resourceSetCount));
        bool anyChanged = false;
        for (int slot = 0; slot < count; slot++) {
            if (this.BoundSets[slot].Set is not D3D12ResourceSet resourceSet) {
                continue;
            }

            if (!ResourceSetReferencesBuffer(resourceSet, buffer)) {
                continue;
            }

            this.MarkChanged((uint)slot);
            anyChanged = true;
        }

        return anyChanged;
    }

    /// <summary>
    /// Clears bound sets, dirty flags, and dirty range state.
    /// </summary>
    internal void Clear() {
        for (int i = 0; i < this.BoundSets.Length; i++) {
            this.BoundSets[i].Offsets.Dispose();
        }

        Util.ClearArray(this.BoundSets);
        if (this.Changed.Length != 0) {
            Array.Clear(this.Changed, 0, this.Changed.Length);
        }

        this.ResetDirtyRange();
    }

    /// <summary>
    /// Checks whether a resource set references a specific buffer.
    /// </summary>
    /// <param name="resourceSet">The resource set to inspect.</param>
    /// <param name="buffer">The buffer to find.</param>
    /// <returns><see langword="true" /> when the set references the buffer.</returns>
    private static bool ResourceSetReferencesBuffer(D3D12ResourceSet resourceSet, D3D12DeviceBuffer buffer) {
        D3D12DeviceBuffer[] referencedBuffers = resourceSet.ReferencedBuffers;
        for (int i = 0; i < referencedBuffers.Length; i++) {
            if (ReferenceEquals(referencedBuffers[i], buffer)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a pipeline resource set count to an array-indexable count.
    /// </summary>
    /// <param name="count">The pipeline resource set count.</param>
    /// <returns>The clamped set count.</returns>
    private static int GetClampedResourceSetCount(uint count) {
        return count > int.MaxValue ? int.MaxValue : (int)count;
    }
}
