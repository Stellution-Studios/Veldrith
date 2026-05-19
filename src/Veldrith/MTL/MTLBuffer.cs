using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlBuffer : DeviceBuffer {
    private bool _disposed;
    private string _name;

    public MtlBuffer(ref BufferDescription bd, MtlGraphicsDevice gd) {
        this.SizeInBytes = bd.SizeInBytes;
        uint roundFactor = (4 - this.SizeInBytes % 4) % 4;
        this.ActualCapacity = this.SizeInBytes + roundFactor;
        this.Usage = bd.Usage;

        bool sharedMemory = this.Usage == BufferUsage.Staging ||
                            (this.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        MTLResourceOptions bufferOptions =
            sharedMemory ? MTLResourceOptions.StorageModeShared : MTLResourceOptions.StorageModePrivate;

        this.DeviceBuffer = gd.Device.newBufferWithLengthOptions(this.ActualCapacity,
            bufferOptions);

        unsafe {
            if (sharedMemory) {
                this.Pointer = this.DeviceBuffer.contents();
            }
        }
    }

    public override uint SizeInBytes { get; }
    public override BufferUsage Usage { get; }

    public uint ActualCapacity { get; }

    public override bool IsDisposed => this._disposed;

    public override string Name {
        get => this._name;
        set {
            NSString nameNss = NSString.New(value);
            this.DeviceBuffer.addDebugMarker(nameNss, new NSRange(0, this.SizeInBytes));
            ObjectiveCRuntime.release(nameNss.NativePtr);
            this._name = value;
        }
    }

    public MTLBuffer DeviceBuffer { get; }

    public unsafe void* Pointer { get; private set; }

    #region Disposal

    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.release(this.DeviceBuffer.NativePtr);
        }
    }

    #endregion
}