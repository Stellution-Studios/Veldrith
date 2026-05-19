namespace Veldrith.D3D12
{
    internal sealed class D3D12ResourceSet : ResourceSet
    {
        private readonly D3D12ResourceLayout layout;
        private readonly IBindableResource[] boundResources;
        private bool disposed;
        private string name;

        public D3D12ResourceSet(ref ResourceSetDescription description)
            : base(ref description)
        {
            layout = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(description.Layout);
            boundResources = Util.ShallowClone(description.BoundResources);
        }

        internal D3D12ResourceLayout ResourceLayoutInfo => layout;
        internal IBindableResource[] BoundResources => boundResources;

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
