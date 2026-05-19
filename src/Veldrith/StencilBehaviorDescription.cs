using System;

namespace Veldrith;

/// <summary>
/// Describes how stencil tests are performed in a <see cref="Pipeline" />'s depth-stencil state.
/// </summary>
public struct StencilBehaviorDescription : IEquatable<StencilBehaviorDescription> {

    /// <summary>
    /// The operation performed on samples that fail the stencil test.
    /// </summary>
    public StencilOperation Fail;

    /// <summary>
    /// The operation performed on samples that pass the stencil test.
    /// </summary>
    public StencilOperation Pass;

    /// <summary>
    /// The operation performed on samples that pass the stencil test but fail the depth test.
    /// </summary>
    public StencilOperation DepthFail;

    /// <summary>
    /// The comparison operator used in the stencil test.
    /// </summary>
    public ComparisonKind Comparison;

    /// <summary>
    /// Initializes a new instance of the <see cref="StencilBehaviorDescription" /> type.
    /// </summary>
    /// <param name="fail">Specifies the value of <paramref name="fail" />.</param>
    /// <param name="pass">Specifies the value of <paramref name="pass" />.</param>
    /// <param name="depthFail">Specifies the value of <paramref name="depthFail" />.</param>
    /// <param name="comparison">Specifies the value of <paramref name="comparison" />.</param>
    public StencilBehaviorDescription(StencilOperation fail, StencilOperation pass, StencilOperation depthFail, ComparisonKind comparison) {
        this.Fail = fail;
        this.Pass = pass;
        this.DepthFail = depthFail;
        this.Comparison = comparison;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(StencilBehaviorDescription other) {
        return this.Fail == other.Fail && this.Pass == other.Pass && this.DepthFail == other.DepthFail && this.Comparison == other.Comparison;
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine((int)this.Fail, (int)this.Pass, (int)this.DepthFail, (int)this.Comparison);
    }
}