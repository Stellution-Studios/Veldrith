using System;

namespace Veldrith.MTL;

/// <summary>
/// Defines the IMtlDisplayLink interface.
/// </summary>
internal interface IMtlDisplayLink : IDisposable {

    /// <summary>
    /// Performs the GetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <returns>The result of the GetActualOutputVideoRefreshPeriod operation.</returns>
    public double GetActualOutputVideoRefreshPeriod();

    /// <summary>
    /// Performs the UpdateActiveDisplay operation.
    /// </summary>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="w">The value of w.</param>
    /// <param name="h">The value of h.</param>
    public void UpdateActiveDisplay(int x, int y, int w, int h);

    /// <summary>
    /// Occurs when Callback.
    /// </summary>
    event Action Callback;
}