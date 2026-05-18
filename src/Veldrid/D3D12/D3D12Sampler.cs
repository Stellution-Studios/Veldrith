namespace Veldrid.D3D12
{
    internal sealed class D3D12Sampler : Sampler
    {
        private readonly SamplerDescription description;
        private bool disposed;
        private string name;

        public D3D12Sampler(ref SamplerDescription description)
        {
            this.description = description;
        }

        internal SamplerDescription Description => description;

        public override bool IsDisposed => disposed;

        public override string Name
        {
            get => name;
            set => name = value;
        }

        public override void Dispose()
        {
            disposed = true;
        }
    }
}
