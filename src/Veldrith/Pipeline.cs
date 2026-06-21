using System;

namespace Veldrith;

/// <summary>
/// Represents the Pipeline type used by the graphics runtime.
/// </summary>
public abstract class Pipeline : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="Pipeline" /> class.
    /// </summary>
    /// <param name="graphicsDescription">The graphics description value used by this operation.</param>
    internal Pipeline(ref GraphicsPipelineDescription graphicsDescription)

        /// <summary>
        /// Executes the this logic for this backend.
        /// </summary>
        : this(graphicsDescription.ResourceLayouts) {
#if VALIDATE_USAGE
        this.GraphicsOutputDescription = graphicsDescription.Outputs;
#endif
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pipeline" /> type.
    /// </summary>
    /// <param name="computeDescription">The compute description value used by this operation.</param>
    internal Pipeline(ref ComputePipelineDescription computeDescription)
        : this(computeDescription.ResourceLayouts) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pipeline" /> type.
    /// </summary>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    internal Pipeline(ResourceLayout[] resourceLayouts) {
#if VALIDATE_USAGE
        this.ResourceLayouts = Util.ShallowClone(resourceLayouts);
#endif
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents a compute Pipeline.
    /// </summary>
    public abstract bool IsComputePipeline { get; }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public abstract void Dispose();

    #endregion

#if VALIDATE_USAGE

    /// <summary>
    /// Gets or sets GraphicsOutputDescription.
    /// </summary>
    internal OutputDescription GraphicsOutputDescription { get; }

    /// <summary>
    /// Gets or sets ResourceLayouts.
    /// </summary>
    internal ResourceLayout[] ResourceLayouts { get; }
#endif
}