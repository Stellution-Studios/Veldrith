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
            _layout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(description.Layout);
            _boundResources = Util.ShallowClone(description.BoundResources);
        }

        internal D3D12ResourceLayout ResourceLayoutInfo => _layout;
        internal IBindableResource[] BoundResources => _boundResources;

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
