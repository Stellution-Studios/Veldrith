using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlBuffer : DeviceBuffer
    {
        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }

        public uint ActualCapacity { get; }

        public override bool IsDisposed => this._disposed;

        public override string Name
        {
            get => this._name;
            set
            {
                var nameNss = NSString.New(value);
                DeviceBuffer.addDebugMarker(nameNss, new NSRange(0, SizeInBytes));
                ObjectiveCRuntime.release(nameNss.NativePtr);
                this._name = value;
            }
        }

        public MTLBuffer DeviceBuffer { get; }

        public unsafe void* Pointer { get; private set; }
        private string _name;
        private bool _disposed;

        public MtlBuffer(ref BufferDescription bd, MtlGraphicsDevice gd)
        {
            SizeInBytes = bd.SizeInBytes;
            uint roundFactor = (4 - SizeInBytes % 4) % 4;
            ActualCapacity = SizeInBytes + roundFactor;
            Usage = bd.Usage;

            bool sharedMemory = Usage == BufferUsage.Staging || (Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            var bufferOptions = sharedMemory ? MTLResourceOptions.StorageModeShared : MTLResourceOptions.StorageModePrivate;

            DeviceBuffer = gd.Device.newBufferWithLengthOptions(
                ActualCapacity,
                bufferOptions);

            unsafe
            {
                if (sharedMemory)
                    Pointer = DeviceBuffer.contents();
            }
        }

        #region Disposal

        public override void Dispose()
        {
            if (!this._disposed)
            {
                this._disposed = true;
                ObjectiveCRuntime.release(DeviceBuffer.NativePtr);
            }
        }

        #endregion
    }
}
