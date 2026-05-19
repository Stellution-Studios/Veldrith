using System;

namespace Veldrith.MTL;

/// <summary>
/// Defines the IMtlDisplayLink interface.
/// </summary>
internal interface IMtlDisplayLink : IDisposable {

    /// <summary>
    /// Executes the GetActualOutputVideoRefreshPeriod operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetActualOutputVideoRefreshPeriod operation.</returns>
    public double GetActualOutputVideoRefreshPeriod();

    /// <summary>
    /// Executes the UpdateActiveDisplay operation.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="w">Specifies the value of <paramref name="w" />.</param>
    /// <param name="h">Specifies the value of <paramref name="h" />.</param>
    public void UpdateActiveDisplay(int x, int y, int w, int h);

    /// <summary>
    /// Occurs when Callback.
    /// </summary>
    event Action Callback;
}