using System;

namespace Veldrith.MTL;

internal interface IMtlDisplayLink : IDisposable {
    public double GetActualOutputVideoRefreshPeriod();
    public void UpdateActiveDisplay(int x, int y, int w, int h);
    event Action Callback;
}