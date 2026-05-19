// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal unsafe class MtlcvDisplayLink : IMtlDisplayLink
    {
        private CVDisplayLink displayLink;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly CVDisplayLinkOutputCallbackDelegate cvDisplayLinkCallbackHandler;

        public MtlcvDisplayLink()
        {
            cvDisplayLinkCallbackHandler = OnCallback;
            displayLink = CVDisplayLink.CreateWithActiveCGDisplays();
            displayLink.SetOutputCallback(cvDisplayLinkCallbackHandler, IntPtr.Zero);
            displayLink.Start();
        }

        #region Disposal

        public void Dispose()
        {
            displayLink.Release();
        }

        #endregion

        public void UpdateActiveDisplay(int x, int y, int w, int h)
        {
            displayLink.UpdateActiveMonitor(x, y, w, h);
        }

        public double GetActualOutputVideoRefreshPeriod()
        {
            return displayLink.GetActualOutputVideoRefreshPeriod();
        }

        private int OnCallback(CVDisplayLink displaylink, CVTimeStamp* innow, CVTimeStamp* inoutputtime, long flagsin, long flagsout, IntPtr userdata)
        {
            Callback?.Invoke();
            return 0;
        }

        public event Action Callback;
    }
}
