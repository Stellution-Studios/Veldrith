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
    /// Tracks the required rebind scope for each changed resource set slot.
    /// </summary>
    internal D3D12ResourceSetChangeKind[] ChangeKinds = Array.Empty<D3D12ResourceSetChangeKind>();

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
    /// Stores the highest touched resource-set slot plus one.
    /// </summary>
    private int _maxTouchedSlot;

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

        D3D12ResourceSetChangeKind changeKind = ReferenceEquals(previousBinding.Set, set)
            ? D3D12ResourceSetChangeKind.RootBindingsOnly
            : D3D12ResourceSetChangeKind.Full;

        this.BoundSets[slot].Offsets.Dispose();
        this.BoundSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
        this.MarkTouched(slot);
        this.MarkChanged(slot, changeKind);
        return true;
    }

    /// <summary>
    /// Ensures the state arrays can hold the requested number of set slots.
    /// </summary>
    /// <param name="count">The minimum required slot count.</param>
    internal void EnsureCapacity(uint count) {
        Util.EnsureArrayMinimumSize(ref this.BoundSets, count);
        Util.EnsureArrayMinimumSize(ref this.Changed, count);
        Util.EnsureArrayMinimumSize(ref this.ChangeKinds, count);
    }

    /// <summary>
    /// Marks a single set slot dirty.
    /// </summary>
    /// <param name="slot">The slot to mark.</param>
    internal void MarkChanged(uint slot) {
        this.MarkChanged(slot, D3D12ResourceSetChangeKind.Full);
    }

    /// <summary>
    /// Marks a single set slot dirty with the requested rebind scope.
    /// </summary>
    /// <param name="slot">The slot to mark.</param>
    /// <param name="changeKind">The required rebind scope.</param>
    internal void MarkChanged(uint slot, D3D12ResourceSetChangeKind changeKind) {
        int index = (int)slot;
        this.Changed[index] = true;
        if (this.ChangeKinds[index] != D3D12ResourceSetChangeKind.Full) {
            this.ChangeKinds[index] = changeKind;
        }

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
        this.MarkTouched(slot);
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

            this.MarkChanged((uint)slot, D3D12ResourceSetChangeKind.RootBindingsOnly);
            anyChanged = true;
        }

        return anyChanged;
    }

    /// <summary>
    /// Clears bound sets, dirty flags, and dirty range state.
    /// </summary>
    internal void Clear() {
        int count = Math.Min(this._maxTouchedSlot, this.BoundSets.Length);
        for (int i = 0; i < count; i++) {
            this.BoundSets[i].Offsets.Dispose();
        }

        if (count != 0) {
            Array.Clear(this.BoundSets, 0, count);
            Array.Clear(this.Changed, 0, Math.Min(count, this.Changed.Length));
            Array.Clear(this.ChangeKinds, 0, Math.Min(count, this.ChangeKinds.Length));
        }

        this._maxTouchedSlot = 0;
        this.ResetDirtyRange();
    }

    /// <summary>
    /// Tracks the highest resource-set slot that may need clearing later.
    /// </summary>
    /// <param name="slot">The touched resource-set slot.</param>
    private void MarkTouched(uint slot) {
        int count = (int)slot + 1;
        if (count > this._maxTouchedSlot) {
            this._maxTouchedSlot = count;
        }
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

/// <summary>
/// Describes how much D3D12 state must be refreshed for a dirty resource-set slot.
/// </summary>
internal enum D3D12ResourceSetChangeKind {

    /// <summary>
    /// The slot is clean.
    /// </summary>
    None,

    /// <summary>
    /// Only root-buffer bindings can change; descriptor tables are reusable.
    /// </summary>
    RootBindingsOnly,

    /// <summary>
    /// The resource set or root-signature-sensitive state changed and all bindings must be refreshed.
    /// </summary>
    Full
}
