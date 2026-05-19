namespace Veldrith.D3D12
{
    internal sealed class D3D12ResourceSet : ResourceSet
    {
        private readonly D3D12ResourceLayout _layout;
        private readonly IBindableResource[] _boundResources;
        private bool _disposed;
        private string _name;

        public D3D12ResourceSet(ref ResourceSetDescription description)
            : base(ref description)
        {
            this._layout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(description.Layout);
            this._boundResources = Util.ShallowClone(description.BoundResources);
        }

        internal D3D12ResourceLayout ResourceLayoutInfo => this._layout;
        internal IBindableResource[] BoundResources => this._boundResources;

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
