// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal unsafe class MtlcvDisplayLink : IMtlDisplayLink
    {
        private CVDisplayLink _displayLink;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly CVDisplayLinkOutputCallbackDelegate _cvDisplayLinkCallbackHandler;

        public MtlcvDisplayLink()
        {
            _cvDisplayLinkCallbackHandler = OnCallback;
            _displayLink = CVDisplayLink.CreateWithActiveCGDisplays();
            _displayLink.SetOutputCallback(_cvDisplayLinkCallbackHandler, IntPtr.Zero);
            _displayLink.Start();
        }

        #region Disposal

        public void Dispose()
        {
            _displayLink.Release();
        }

        #endregion

        public void UpdateActiveDisplay(int x, int y, int w, int h)
        {
            _displayLink.UpdateActiveMonitor(x, y, w, h);
        }

        public double GetActualOutputVideoRefreshPeriod()
        {
            return _displayLink.GetActualOutputVideoRefreshPeriod();
        }

        private int OnCallback(CVDisplayLink displaylink, CVTimeStamp* innow, CVTimeStamp* inoutputtime, long flagsin, long flagsout, IntPtr userdata)
        {
            Callback?.Invoke();
            return 0;
        }

        public event Action Callback;
    }
}
