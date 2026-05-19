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
            this._elements = Util.ShallowClone(description.Elements);
        }

        internal ResourceLayoutElementDescription[] Elements => this._elements;

        public override bool IsDisposed => this._disposed;

        public override string Name
        {
            get => this._name;
            set => this._name = value;
        }

        public override void Dispose()
        {
            this._disposed = true;
        }
    }
}
