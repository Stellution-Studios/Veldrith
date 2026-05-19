namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlResourceLayout class.
/// </summary>
internal class MtlResourceLayout : ResourceLayout {

    /// <summary>
    /// Stores the value associated with <c>_bindingInfosByVdIndex</c>.
    /// </summary>
    private readonly ResourceBindingInfo[] _bindingInfosByVdIndex;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
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
    /// Executes the GetBindingInfo operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <returns>Returns the result produced by the GetBindingInfo operation.</returns>
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
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    /// <summary>
    /// Defines the data layout and behavior of the ResourceBindingInfo struct.
    /// </summary>
    internal struct ResourceBindingInfo {

        /// <summary>
        /// Stores the value associated with <c>Slot</c>.
        /// </summary>
        public uint Slot;

        /// <summary>
        /// Stores the value associated with <c>Stages</c>.
        /// </summary>
        public ShaderStages Stages;

        /// <summary>
        /// Stores the value associated with <c>Kind</c>.
        /// </summary>
        public ResourceKind Kind;

        /// <summary>
        /// Stores the value associated with <c>DynamicBuffer</c>.
        /// </summary>
        public bool DynamicBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBindingInfo" /> type.
        /// </summary>
        /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
        /// <param name="stages">Specifies the value of <paramref name="stages" />.</param>
        /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
        /// <param name="dynamicBuffer">Specifies the value of <paramref name="dynamicBuffer" />.</param>
        public ResourceBindingInfo(uint slot, ShaderStages stages, ResourceKind kind, bool dynamicBuffer) {
            this.Slot = slot;
            this.Stages = stages;
            this.Kind = kind;
            this.DynamicBuffer = dynamicBuffer;
        }
    }
}
