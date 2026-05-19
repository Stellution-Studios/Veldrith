// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal unsafe class MtlcvDisplayLink : IMtlDisplayLink {
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly CVDisplayLinkOutputCallbackDelegate _cvDisplayLinkCallbackHandler;
    private CVDisplayLink _displayLink;

    public MtlcvDisplayLink() {
        this._cvDisplayLinkCallbackHandler = this.OnCallback;
        this._displayLink = CVDisplayLink.CreateWithActiveCGDisplays();
        this._displayLink.SetOutputCallback(this._cvDisplayLinkCallbackHandler, IntPtr.Zero);
        this._displayLink.Start();
    }

    #region Disposal

    public void Dispose() {
        this._displayLink.Release();
    }

    #endregion

    public void UpdateActiveDisplay(int x, int y, int w, int h) {
        this._displayLink.UpdateActiveMonitor(x, y, w, h);
    }

    public double GetActualOutputVideoRefreshPeriod() {
        return this._displayLink.GetActualOutputVideoRefreshPeriod();
    }

    public event Action Callback;

    private int OnCallback(CVDisplayLink displaylink, CVTimeStamp* innow, CVTimeStamp* inoutputtime, long flagsin,
        long flagsout, IntPtr userdata) {
        this.Callback?.Invoke();
        return 0;
    }
}