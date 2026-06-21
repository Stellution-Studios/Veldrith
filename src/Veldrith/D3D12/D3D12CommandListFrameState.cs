using System;
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
    /// Stores the default number of command allocators rotated by one command list.
    /// </summary>
    private const int DefaultAllocatorSlotCount = 3;

    /// <summary>
    /// Stores the minimum supported allocator slot count.
    /// </summary>
    private const int MinAllocatorSlotCount = 3;

    /// <summary>
    /// Stores the maximum supported allocator slot count.
    /// </summary>
    private const int MaxAllocatorSlotCount = 32;

    /// <summary>
    /// Stores the number of command allocators rotated by one command list.
    /// </summary>
    private static readonly int AllocatorSlotCount = ReadAllocatorSlotCount();

    /// <summary>
    /// Stores the graphics device used for allocator creation and fence waiting.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores command allocator slots retained by this command list.
    /// </summary>
    private readonly AllocatorSlot[] _allocatorSlots = new AllocatorSlot[AllocatorSlotCount];

    /// <summary>
    /// Stores the current frame slot index.
    /// </summary>
    private int _currentFrameSlot = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CommandListFrameState" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns the command allocators.</param>
    internal D3D12CommandListFrameState(D3D12GraphicsDevice gd) {
        this._gd = gd;
        for (int i = 0; i < this._allocatorSlots.Length; i++) {
            this._allocatorSlots[i] = new AllocatorSlot(gd.Device.CreateCommandAllocator(CommandListType.Direct));
        }
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
        this._currentFrameSlot = (this._currentFrameSlot + 1) % this._allocatorSlots.Length;
        AllocatorSlot slot = this._allocatorSlots[this._currentFrameSlot];
        this.WaitForFrameSlot(slot, perf);
        slot.Reset();
        return slot.Allocator;
    }

    /// <summary>
    /// Records the submission fence protecting the allocator used by the last recording.
    /// </summary>
    /// <param name="signalValue">The fence value signaled for the submitted command list.</param>
    internal void MarkSubmitted(ulong signalValue) {
        if (this._currentFrameSlot >= 0) {
            this._allocatorSlots[this._currentFrameSlot].FenceValue = signalValue;
        }
    }

    /// <summary>
    /// Waits for all submissions that can still reference command allocators owned by this state object.
    /// </summary>
    internal void WaitForSubmittedFrameSlots() {
        for (int i = 0; i < this._allocatorSlots.Length; i++) {
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
        for (int i = 0; i < this._allocatorSlots.Length; i++) {
            this._allocatorSlots[i].Allocator?.Dispose();
        }
    }

    /// <summary>
    /// Waits until a fixed frame slot can be safely reused.
    /// </summary>
    /// <param name="slot">The allocator slot to wait for.</param>
    /// <param name="perf">The optional performance tracker receiving wait timing.</param>
    private void WaitForFrameSlot(AllocatorSlot slot, D3D12CommandListPerfTracker perf) {
        ulong fenceValue = slot.FenceValue;
        if (fenceValue == 0) {
            return;
        }

        if (this._gd.IsSubmissionFenceComplete(fenceValue)) {
            slot.FenceValue = 0;
            return;
        }

        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this._gd.WaitForSubmissionFence(fenceValue);
        slot.FenceValue = 0;

        if (D3D12CommandListPerfTracker.Enabled) {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
            perf.BeginWaitMs += D3D12CommandListPerfTracker.TicksToMilliseconds(elapsedTicks);
            perf.BeginWaitCount++;
        }
    }

    /// <summary>
    /// Reads the fixed command allocator slot count from the environment.
    /// </summary>
    /// <returns>The configured allocator slot count.</returns>
    private static int ReadAllocatorSlotCount() {
        string value = Environment.GetEnvironmentVariable("VELDRID_D3D12_COMMAND_ALLOCATOR_SLOTS");
        if (int.TryParse(value, out int parsed)) {
            return Math.Clamp(parsed, MinAllocatorSlotCount, MaxAllocatorSlotCount);
        }

        return DefaultAllocatorSlotCount;
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
