using System;

namespace Veldrith.MTL;

/// <summary>
/// Defines the IMtlDisplayLink interface.
/// </summary>
internal interface IMtlDisplayLink : IDisposable {

    /// <summary>
    /// Executes GetActualOutputVideoRefreshPeriod.
    /// </summary>
    public double GetActualOutputVideoRefreshPeriod();

    /// <summary>
    /// Executes UpdateActiveDisplay.
    /// </summary>
    public void UpdateActiveDisplay(int x, int y, int w, int h);

    /// <summary>
    /// Occurs when Callback.
    /// </summary>
    event Action Callback;
}