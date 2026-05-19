using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlcvDisplayLink class.
/// </summary>
internal unsafe class MtlcvDisplayLink : IMtlDisplayLink {
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Represents the _cvDisplayLinkCallbackHandler field.
    /// </summary>
    private readonly CVDisplayLinkOutputCallbackDelegate _cvDisplayLinkCallbackHandler;

    /// <summary>
    /// Represents the _displayLink field.
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
    /// Performs the Dispose operation.
    /// </summary>
    public void Dispose() {
        this._displayLink.Release();
    }

    #endregion

    /// <summary>
    /// Performs the UpdateActiveDisplay operation.
    /// </summary>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="w">The value of w.</param>
    /// <param name="h">The value of h.</param>
    public void UpdateActiveDisplay(int x, int y, int w, int h) {
        this._displayLink.UpdateActiveMonitor(x, y, w, h);
    }

    /// <summary>
    /// Performs the GetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <returns>The result of the GetActualOutputVideoRefreshPeriod operation.</returns>
    public double GetActualOutputVideoRefreshPeriod() {
        return this._displayLink.GetActualOutputVideoRefreshPeriod();
    }

    /// <summary>
    /// Represents the Callback field.
    /// </summary>
    public event Action Callback;

    /// <summary>
    /// Performs the OnCallback operation.
    /// </summary>
    /// <param name="displaylink">The value of displaylink.</param>
    /// <param name="innow">The value of innow.</param>
    /// <param name="inoutputtime">The value of inoutputtime.</param>
    /// <param name="flagsin">The value of flagsin.</param>
    /// <param name="flagsout">The value of flagsout.</param>
    /// <param name="userdata">The value of userdata.</param>
    /// <returns>The result of the OnCallback operation.</returns>
    private int OnCallback(CVDisplayLink displaylink, CVTimeStamp* innow, CVTimeStamp* inoutputtime, long flagsin, long flagsout, IntPtr userdata) {
        this.Callback?.Invoke();
        return 0;
    }
}