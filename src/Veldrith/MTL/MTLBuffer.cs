using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlBuffer class.
/// </summary>
internal class MtlBuffer : DeviceBuffer {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlBuffer" /> class.
    /// </summary>
    public MtlBuffer(ref BufferDescription bd, MtlGraphicsDevice gd) {
        this.SizeInBytes = bd.SizeInBytes;
        uint roundFactor = (4 - this.SizeInBytes % 4) % 4;
        this.ActualCapacity = this.SizeInBytes + roundFactor;
        this.Usage = bd.Usage;

        bool sharedMemory = this.Usage == BufferUsage.Staging || (this.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        MTLResourceOptions bufferOptions = sharedMemory ? MTLResourceOptions.StorageModeShared : MTLResourceOptions.StorageModePrivate;

        this.DeviceBuffer = gd.Device.newBufferWithLengthOptions(this.ActualCapacity, bufferOptions);

        unsafe {
            if (sharedMemory) {
                this.Pointer = this.DeviceBuffer.contents();
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
            this.DeviceBuffer.addDebugMarker(nameNss, new NSRange(0, this.SizeInBytes));
            ObjectiveCRuntime.release(nameNss.NativePtr);
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
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.release(this.DeviceBuffer.NativePtr);
        }
    }

    #endregion
}