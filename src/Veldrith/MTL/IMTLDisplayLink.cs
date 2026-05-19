using System;

namespace Veldrith.MTL;

/// <summary>
/// Defines the IMtlDisplayLink interface.
/// </summary>
internal interface IMtlDisplayLink : IDisposable {

    /// <summary>
    /// Gets the actual output video refresh period value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public double GetActualOutputVideoRefreshPeriod();

    /// <summary>
    /// Updates the active display state for this command sequence.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="w">The w value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
    public void UpdateActiveDisplay(int x, int y, int w, int h);

    /// <summary>
    /// Occurs when Callback.
    /// </summary>
    event Action Callback;
}