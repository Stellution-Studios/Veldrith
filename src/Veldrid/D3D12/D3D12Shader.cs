namespace Veldrid.D3D12
{
    internal sealed class D3D12Shader : Shader
    {
        private bool disposed;
        private string name;

        public D3D12Shader(ref ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            ShaderBytes = description.ShaderBytes;
            Debug = description.Debug;
        }

        public byte[] ShaderBytes { get; }
        public bool Debug { get; }
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
