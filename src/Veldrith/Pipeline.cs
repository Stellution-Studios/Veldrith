using System;

namespace Veldrith;

public abstract class Pipeline : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="Pipeline" /> class.
    /// </summary>
    internal Pipeline(ref GraphicsPipelineDescription graphicsDescription)
        : this(graphicsDescription.ResourceLayouts) {
#if VALIDATE_USAGE
        this.GraphicsOutputDescription = graphicsDescription.Outputs;
#endif
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pipeline" /> class.
    /// </summary>
    internal Pipeline(ref ComputePipelineDescription computeDescription)
        : this(computeDescription.ResourceLayouts) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pipeline" /> class.
    /// </summary>
    internal Pipeline(ResourceLayout[] resourceLayouts) {
#if VALIDATE_USAGE
        this.ResourceLayouts = Util.ShallowClone(resourceLayouts);
#endif
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents a compute Pipeline.
    /// If false, this instance is a graphics pipeline.
    /// </summary>
    public abstract bool IsComputePipeline { get; }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// tools.
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Frees unmanaged device resources controlled by this instance.
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