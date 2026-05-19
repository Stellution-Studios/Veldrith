namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlResourceLayout class.
/// </summary>
internal class MtlResourceLayout : ResourceLayout {

    /// <summary>
    /// Represents the _bindingInfosByVdIndex field.
    /// </summary>
    private readonly ResourceBindingInfo[] _bindingInfosByVdIndex;

    /// <summary>
    /// Represents the _disposed field.
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
    /// Executes GetBindingInfo.
    /// </summary>
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
    public MtlResourceLayout(ref ResourceLayoutDescription description, MtlGraphicsDevice gd)

        /// <summary>
        /// Executes base.
        /// </summary>
        : base(ref description) {
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
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    /// <summary>
    /// Represents the ResourceBindingInfo struct.
    /// </summary>
    internal struct ResourceBindingInfo {

        /// <summary>
        /// Represents the Slot field.
        /// </summary>
        public uint Slot;

        /// <summary>
        /// Represents the Stages field.
        /// </summary>
        public ShaderStages Stages;

        /// <summary>
        /// Represents the Kind field.
        /// </summary>
        public ResourceKind Kind;

        /// <summary>
        /// Represents the DynamicBuffer field.
        /// </summary>
        public bool DynamicBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBindingInfo" /> class.
        /// </summary>
        public ResourceBindingInfo(uint slot, ShaderStages stages, ResourceKind kind, bool dynamicBuffer) {
            this.Slot = slot;
            this.Stages = stages;
            this.Kind = kind;
            this.DynamicBuffer = dynamicBuffer;
        }
    }
}