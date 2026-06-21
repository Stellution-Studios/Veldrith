using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlcvDisplayLink.
/// </summary>
internal unsafe class MtlcvDisplayLink : IMtlDisplayLink {

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    /// <summary>
    /// Stores the cv display link callback handler state used by this instance.
    /// </summary>
    private readonly CVDisplayLinkOutputCallbackDelegate _cvDisplayLinkCallbackHandler;

    /// <summary>
    /// Stores the display link state used by this instance.
    /// </summary>
    private CVDisplayLink _displayLink;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlcvDisplayLink" /> type.
    /// </summary>
    public MtlcvDisplayLink() {
        this._cvDisplayLinkCallbackHandler = this.OnCallback;
        this._displayLink = CVDisplayLink.CreateWithActiveCGDisplays();
        this._displayLink.SetOutputCallback(this._cvDisplayLinkCallbackHandler, IntPtr.Zero);
        this._displayLink.Start();
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public void Dispose() {
        this._displayLink.Release();
    }

    #endregion

    /// <summary>
    /// Updates the active display state for this command sequence.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="w">The w value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
    public void UpdateActiveDisplay(int x, int y, int w, int h) {
        this._displayLink.UpdateActiveMonitor(x, y, w, h);
    }

    /// <summary>
    /// Gets the actual output video refresh period value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public double GetActualOutputVideoRefreshPeriod() {
        return this._displayLink.GetActualOutputVideoRefreshPeriod();
    }

    /// <summary>
    /// Stores the callback state used by this instance.
    /// </summary>
    public event Action Callback;

    /// <summary>
    /// Executes the on callback logic for this backend.
    /// </summary>
    /// <param name="displaylink">The displaylink value used by this operation.</param>
    /// <param name="innow">The innow value used by this operation.</param>
    /// <param name="inoutputtime">The inoutputtime value used by this operation.</param>
    /// <param name="flagsin">The flagsin value used by this operation.</param>
    /// <param name="flagsout">The flagsout value used by this operation.</param>
    /// <param name="userdata">The userdata value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private int OnCallback(CVDisplayLink displaylink, CVTimeStamp* innow, CVTimeStamp* inoutputtime, long flagsin, long flagsout, IntPtr userdata) {
        this.Callback?.Invoke();
        return 0;
    }
}