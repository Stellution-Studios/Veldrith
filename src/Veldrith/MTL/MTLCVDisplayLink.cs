using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlcvDisplayLink class.
/// </summary>
internal unsafe class MtlcvDisplayLink : IMtlDisplayLink {
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Stores the value associated with <c>_cvDisplayLinkCallbackHandler</c>.
    /// </summary>
    private readonly CVDisplayLinkOutputCallbackDelegate _cvDisplayLinkCallbackHandler;

    /// <summary>
    /// Stores the value associated with <c>_displayLink</c>.
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
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose() {
        this._displayLink.Release();
    }

    #endregion

    /// <summary>
    /// Executes the UpdateActiveDisplay operation.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="w">Specifies the value of <paramref name="w" />.</param>
    /// <param name="h">Specifies the value of <paramref name="h" />.</param>
    public void UpdateActiveDisplay(int x, int y, int w, int h) {
        this._displayLink.UpdateActiveMonitor(x, y, w, h);
    }

    /// <summary>
    /// Executes the GetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetActualOutputVideoRefreshPeriod operation.</returns>
    public double GetActualOutputVideoRefreshPeriod() {
        return this._displayLink.GetActualOutputVideoRefreshPeriod();
    }

    /// <summary>
    /// Stores the value associated with <c>Callback</c>.
    /// </summary>
    public event Action Callback;

    /// <summary>
    /// Executes the OnCallback operation.
    /// </summary>
    /// <param name="displaylink">Specifies the value of <paramref name="displaylink" />.</param>
    /// <param name="innow">Specifies the value of <paramref name="innow" />.</param>
    /// <param name="inoutputtime">Specifies the value of <paramref name="inoutputtime" />.</param>
    /// <param name="flagsin">Specifies the value of <paramref name="flagsin" />.</param>
    /// <param name="flagsout">Specifies the value of <paramref name="flagsout" />.</param>
    /// <param name="userdata">Specifies the value of <paramref name="userdata" />.</param>
    /// <returns>Returns the result produced by the OnCallback operation.</returns>
    private int OnCallback(CVDisplayLink displaylink, CVTimeStamp* innow, CVTimeStamp* inoutputtime, long flagsin, long flagsout, IntPtr userdata) {
        this.Callback?.Invoke();
        return 0;
    }
}