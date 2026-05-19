namespace Veldrith.D3D12
{
    internal sealed class D3D12ResourceLayout : ResourceLayout
    {
        private readonly ResourceLayoutElementDescription[] _elements;
        private bool _disposed;
        private string _name;

        public D3D12ResourceLayout(ref ResourceLayoutDescription description)
            : base(ref description)
        {
            _elements = Util.ShallowClone(description.Elements);
        }

        internal ResourceLayoutElementDescription[] Elements => _elements;

        public override bool IsDisposed => _disposed;

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        public override void Dispose()
        {
            _disposed = true;
        }
    }
}
