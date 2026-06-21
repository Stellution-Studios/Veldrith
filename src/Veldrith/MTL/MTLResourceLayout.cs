namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlResourceLayout.
/// </summary>
internal class MtlResourceLayout : ResourceLayout {

    /// <summary>
    /// Stores the binding infos by vd index value used during command execution.
    /// </summary>
    private readonly ResourceBindingInfo[] _bindingInfosByVdIndex;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Gets or sets BufferCount.
    /// </summary>
    public uint BufferCount { get; }

    /// <summary>
    /// Gets or sets TextureCount.
    /// </summary>
    public uint TextureCount { get; }

    /// <summary>
    /// Gets or sets SamplerCount.
    /// </summary>
    public uint SamplerCount { get; }
#if !VALIDATE_USAGE

        /// <summary>
        /// Gets or sets ResourceKinds.
        /// </summary>
        public ResourceKind[] ResourceKinds { get; }
#endif

    /// <summary>
    /// Gets the binding info value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <returns>The value produced by this operation.</returns>
    public ResourceBindingInfo GetBindingInfo(int index) {
        return this._bindingInfosByVdIndex[index];
    }

#if !VALIDATE_USAGE

        /// <summary>
        /// Gets or sets Description.
        /// </summary>
        public ResourceLayoutDescription Description { get; }
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlResourceLayout" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlResourceLayout(ref ResourceLayoutDescription description, MtlGraphicsDevice gd) : base(ref description) {
#if !VALIDATE_USAGE
            Description = description;
#endif

        ResourceLayoutElementDescription[] elements = description.Elements;
#if !VALIDATE_USAGE
            ResourceKinds = new ResourceKind[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                ResourceKinds[i] = elements[i].Kind;
            }
#endif

        this._bindingInfosByVdIndex = new ResourceBindingInfo[elements.Length];

        uint bufferIndex = 0;
        uint texIndex = 0;
        uint samplerIndex = 0;

        for (int i = 0; i < this._bindingInfosByVdIndex.Length; i++) {
            uint slot;

            switch (elements[i].Kind) {
                case ResourceKind.UniformBuffer:
                    slot = bufferIndex++;
                    break;

                case ResourceKind.StructuredBufferReadOnly:
                    slot = bufferIndex++;
                    break;

                case ResourceKind.StructuredBufferReadWrite:
                    slot = bufferIndex++;
                    break;

                case ResourceKind.TextureReadOnly:
                    slot = texIndex++;
                    break;

                case ResourceKind.TextureReadWrite:
                    slot = texIndex++;
                    break;

                case ResourceKind.Sampler:
                    slot = samplerIndex++;
                    break;

                default: throw Illegal.Value<ResourceKind>();
            }

            this._bindingInfosByVdIndex[i] = new ResourceBindingInfo(slot, elements[i].Stages, elements[i].Kind, (elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0);
        }

        this.BufferCount = bufferIndex;
        this.TextureCount = texIndex;
        this.SamplerCount = samplerIndex;
    }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    /// <summary>
    /// Represents the ResourceBindingInfo data structure used by the graphics runtime.
    /// </summary>
    internal struct ResourceBindingInfo {

        /// <summary>
        /// Stores the slot state used by this instance.
        /// </summary>
        public uint Slot;

        /// <summary>
        /// Stores the stages state used by this instance.
        /// </summary>
        public ShaderStages Stages;

        /// <summary>
        /// Stores the kind state used by this instance.
        /// </summary>
        public ResourceKind Kind;

        /// <summary>
        /// Stores the dynamic buffer state used by this instance.
        /// </summary>
        public bool DynamicBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBindingInfo" /> type.
        /// </summary>
        /// <param name="slot">The slot value used by this operation.</param>
        /// <param name="stages">The stages value used by this operation.</param>
        /// <param name="kind">The kind value used by this operation.</param>
        /// <param name="dynamicBuffer">The dynamic buffer value used by this operation.</param>
        public ResourceBindingInfo(uint slot, ShaderStages stages, ResourceKind kind, bool dynamicBuffer) {
            this.Slot = slot;
            this.Stages = stages;
            this.Kind = kind;
            this.DynamicBuffer = dynamicBuffer;
        }
    }
}