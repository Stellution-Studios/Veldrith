using System;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpGen.Runtime;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Fence.
/// </summary>
internal sealed class D3D12Fence : Fence {

    /// <summary>
    /// Stores the native fence state used by this instance.
    /// </summary>
    private readonly ID3D12Fence _nativeFence;

    /// <summary>
    /// Stores the graphics device that owns the queue used to signal this fence.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores the wait event state used by this instance.
    /// </summary>
    private readonly AutoResetEvent _waitEvent;

    /// <summary>
    /// Stores the native wait event handle.
    /// </summary>
    private readonly nint _waitEventHandle;

    /// <summary>
    /// Stores the native fence pointer.
    /// </summary>
    private readonly nint _nativeFencePointer;

    private readonly unsafe delegate* unmanaged[Stdcall]<void*, ulong> _getCompletedValue;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, ulong, nint, int> _setEventOnCompletion;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the fence value state used by this instance.
    /// </summary>
    private ulong _fenceValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Fence" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="signaled">The signaled value used by this operation.</param>
    public D3D12Fence(D3D12GraphicsDevice gd, bool signaled) {
        ulong initialValue = signaled ? 1UL : 0UL;
        this._gd = gd;
        this._nativeFence = gd.Device.CreateFence(initialValue);
        this._waitEvent = new AutoResetEvent(false);
        this._waitEventHandle = this._waitEvent.SafeWaitHandle.DangerousGetHandle();
        unsafe {
            this._nativeFencePointer = this._nativeFence.NativePointer;
            void** vtbl = *(void***)this._nativeFencePointer;
            this._getCompletedValue = (delegate* unmanaged[Stdcall]<void*, ulong>)vtbl[8];
            this._setEventOnCompletion = (delegate* unmanaged[Stdcall]<void*, ulong, nint, int>)vtbl[9];
        }

        this._fenceValue = initialValue;
    }

    /// <summary>
    /// Gets or sets Signaled.
    /// </summary>
    public override bool Signaled => this.GetCompletedValueNoAlloc() >= this._fenceValue;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this._disposed = true;
        this._waitEvent.Dispose();
        this._nativeFence.Dispose();
    }

    /// <summary>
    /// Resets this instance to its initial state.
    /// </summary>
    public override void Reset() {
        this._fenceValue = this.GetCompletedValueNoAlloc() + 1;
    }

    /// <summary>
    /// Executes the signal logic for this backend.
    /// </summary>
    internal void Signal() {
        ulong completedValue = this.GetCompletedValueNoAlloc();
        if (this._fenceValue <= completedValue) {
            this._fenceValue = completedValue + 1;
        }

        this._gd.SignalQueueFenceNoAlloc(this._nativeFence, this._fenceValue);
    }

    /// <summary>
    /// Executes the wait logic for this backend.
    /// </summary>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool Wait(ulong nanosecondTimeout) {
        if (this.Signaled) {
            return true;
        }

        this.SetEventOnCompletionNoAlloc(this._fenceValue);

        if (nanosecondTimeout == ulong.MaxValue) {
            this._waitEvent.WaitOne();
            return true;
        }

        int milliseconds = nanosecondTimeout > 0 ? (int)Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000) : 0;
        return this._waitEvent.WaitOne(milliseconds);
    }

    /// <summary>
    /// Reads the completed fence value without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ulong GetCompletedValueNoAlloc() {
        return this._getCompletedValue((void*)this._nativeFencePointer);
    }

    /// <summary>
    /// Registers the wait event without going through the managed COM wrapper.
    /// </summary>
    /// <param name="value">The fence value that should signal the event.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetEventOnCompletionNoAlloc(ulong value) {
        Result result = new(this._setEventOnCompletion((void*)this._nativeFencePointer, value, this._waitEventHandle));
        result.CheckError();
    }
}
