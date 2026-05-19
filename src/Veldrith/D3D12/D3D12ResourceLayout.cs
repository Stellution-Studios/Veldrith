namespace Veldrith.D3D12
{
    internal sealed class D3D12ResourceLayout : ResourceLayout
    {
        private readonly ResourceLayoutElementDescription[] elements;
        private bool disposed;
        private string name;

        public D3D12ResourceLayout(ref ResourceLayoutDescription description)
            : base(ref description)
        {
            elements = Util.ShallowClone(description.Elements);
        }

        internal ResourceLayoutElementDescription[] Elements => elements;

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
