using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice;
using Vortice.Direct3D12;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Owns fixed D3D12 command allocator rotation and submission-fence waits for a command list.
/// </summary>
internal sealed class D3D12CommandListFrameState : IDisposable {

    /// <summary>
    /// Stores the graphics device used for allocator creation and fence waiting.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores command allocator slots retained by this command list.
    /// </summary>
    private readonly List<AllocatorSlot> _allocatorSlots = new();

    /// <summary>
    /// Stores the allocator slot used by the current recording.
    /// </summary>
    private AllocatorSlot _currentAllocatorSlot;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CommandListFrameState" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns the command allocators.</param>
    internal D3D12CommandListFrameState(D3D12GraphicsDevice gd) {
        this._gd = gd;
        this._allocatorSlots.Add(new AllocatorSlot(gd.Device.CreateCommandAllocator(CommandListType.Direct)));
    }

    /// <summary>
    /// Gets the first allocator used to create the native command list.
    /// </summary>
    internal ID3D12CommandAllocator InitialAllocator => this._allocatorSlots[0].Allocator;

    /// <summary>
    /// Rotates to the next fixed frame allocator, waits if needed, resets it, and returns it for command-list reset.
    /// </summary>
    /// <param name="perf">The optional performance tracker receiving begin-wait timing.</param>
    /// <returns>The allocator that is ready for the next recording.</returns>
    internal ID3D12CommandAllocator BeginRecording(D3D12CommandListPerfTracker perf) {
        AllocatorSlot slot = this.AcquireAllocatorSlot();
        this._currentAllocatorSlot = slot;
        slot.Reset();
        return slot.Allocator;
    }

    /// <summary>
    /// Records the submission fence protecting the allocator used by the last recording.
    /// </summary>
    /// <param name="signalValue">The fence value signaled for the submitted command list.</param>
    internal void MarkSubmitted(ulong signalValue) {
        if (this._currentAllocatorSlot != null) {
            this._currentAllocatorSlot.FenceValue = signalValue;
        }
    }

    /// <summary>
    /// Waits for all submissions that can still reference command allocators owned by this state object.
    /// </summary>
    internal void WaitForSubmittedFrameSlots() {
        for (int i = 0; i < this._allocatorSlots.Count; i++) {
            AllocatorSlot slot = this._allocatorSlots[i];
            ulong fenceValue = slot.FenceValue;
            if (fenceValue == 0) {
                continue;
            }

            this._gd.WaitForSubmissionFence(fenceValue);
            slot.FenceValue = 0;
        }
    }

    /// <summary>
    /// Releases all command allocators after outstanding submissions have been drained by the owner.
    /// </summary>
    public void Dispose() {
        for (int i = 0; i < this._allocatorSlots.Count; i++) {
            this._allocatorSlots[i].Allocator?.Dispose();
        }
    }

    /// <summary>
    /// Acquires a reusable command allocator slot or creates a new one when all existing slots are still in flight.
    /// </summary>
    /// <returns>The allocator slot that is ready for recording.</returns>
    private AllocatorSlot AcquireAllocatorSlot() {
        for (int i = 0; i < this._allocatorSlots.Count; i++) {
            AllocatorSlot slot = this._allocatorSlots[i];
            ulong fenceValue = slot.FenceValue;
            if (fenceValue == 0) {
                return slot;
            }

            if (this._gd.IsSubmissionFenceComplete(fenceValue)) {
                slot.FenceValue = 0;
                return slot;
            }
        }

        AllocatorSlot created = new(this._gd.Device.CreateCommandAllocator(CommandListType.Direct));
        this._allocatorSlots.Add(created);
        return created;
    }

    /// <summary>
    /// Stores one command allocator and the submission fence value that protects its reuse.
    /// </summary>
    private sealed class AllocatorSlot {

        /// <summary>
        /// Stores the native command allocator pointer.
        /// </summary>
        private readonly nint _allocatorPointer;

        private readonly unsafe delegate* unmanaged[Stdcall]<void*, int> _reset;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllocatorSlot" /> class.
        /// </summary>
        /// <param name="allocator">The command allocator owned by this slot.</param>
        public AllocatorSlot(ID3D12CommandAllocator allocator) {
            this.Allocator = allocator;
            unsafe {
                this._allocatorPointer = allocator.NativePointer;
                void** vtbl = *(void***)this._allocatorPointer;
                this._reset = (delegate* unmanaged[Stdcall]<void*, int>)vtbl[8];
            }
        }

        /// <summary>
        /// Gets the command allocator owned by this slot.
        /// </summary>
        public ID3D12CommandAllocator Allocator { get; }

        /// <summary>
        /// Gets or sets the submission fence value that must complete before reuse.
        /// </summary>
        public ulong FenceValue { get; set; }

        /// <summary>
        /// Resets the allocator without going through the managed COM wrapper.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Reset() {
            Result result = new(this._reset((void*)this._allocatorPointer));
            result.CheckError();
        }
    }
}
