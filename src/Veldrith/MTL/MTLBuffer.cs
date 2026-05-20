using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlBuffer.
/// </summary>
internal class MtlBuffer : DeviceBuffer {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlBuffer" /> type.
    /// </summary>
    /// <param name="bd">The bd value used by this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlBuffer(ref BufferDescription bd, MtlGraphicsDevice gd) {
        this.SizeInBytes = bd.SizeInBytes;
        uint roundFactor = (4 - this.SizeInBytes % 4) % 4;
        this.ActualCapacity = this.SizeInBytes + roundFactor;
        this.Usage = bd.Usage;

        bool sharedMemory = this.Usage == BufferUsage.Staging || (this.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        MTLResourceOptions bufferOptions = sharedMemory ? MTLResourceOptions.StorageModeShared : MTLResourceOptions.StorageModePrivate;

        this.DeviceBuffer = gd.Device.NewBufferWithLengthOptions(this.ActualCapacity, bufferOptions);

        unsafe {
            if (sharedMemory) {
                this.Pointer = this.DeviceBuffer.Contents();
            }
        }
    }

    /// <summary>
    /// Gets or sets SizeInBytes.
    /// </summary>
    public override uint SizeInBytes { get; }

    /// <summary>
    /// Gets or sets Usage.
    /// </summary>
    public override BufferUsage Usage { get; }

    /// <summary>
    /// Gets or sets ActualCapacity.
    /// </summary>
    public uint ActualCapacity { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get;
        set {
            NSString nameNss = NSString.New(value);
            this.DeviceBuffer.AddDebugMarker(nameNss, new NSRange(0, this.SizeInBytes));
            ObjectiveCRuntime.Release(nameNss.NativePtr);
            field = value;
        }
    }

    /// <summary>
    /// Gets or sets DeviceBuffer.
    /// </summary>
    public MTLBuffer DeviceBuffer { get; }

    /// <summary>
    /// Gets or sets Pointer.
    /// </summary>
    public unsafe void* Pointer { get; private set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.Release(this.DeviceBuffer.NativePtr);
        }
    }

    #endregion
}