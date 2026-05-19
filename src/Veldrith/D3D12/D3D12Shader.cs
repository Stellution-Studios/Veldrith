using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.D3D12
{
    internal sealed class D3D12Shader : Shader
    {
        private bool disposed;
        private string name;

        public D3D12Shader(ref ShaderDescription description)
            : base(description.Stage, description.EntryPoint)
        {
            ShaderBytes = EnsureDxbcBytecode(description);
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

        private static byte[] EnsureDxbcBytecode(ShaderDescription description)
        {
            byte[] source = description.ShaderBytes ?? Array.Empty<byte>();
            if (source.Length >= 4
                && source[0] == (byte)'D'
                && source[1] == (byte)'X'
                && source[2] == (byte)'B'
                && source[3] == (byte)'C')
            {
                return source;
            }

            string sourceText = Encoding.UTF8.GetString(source);
            string targetProfile = GetTargetProfile(description.Stage);
            return CompileHlsl(sourceText, description.EntryPoint, targetProfile);
        }

        private static string GetTargetProfile(ShaderStages stage)
        {
            switch (stage)
            {
                case ShaderStages.Vertex:
                    return "vs_5_0";
                case ShaderStages.Fragment:
                    return "ps_5_0";
                case ShaderStages.Geometry:
                    return "gs_5_0";
                case ShaderStages.TessellationControl:
                    return "hs_5_0";
                case ShaderStages.TessellationEvaluation:
                    return "ds_5_0";
                case ShaderStages.Compute:
                    return "cs_5_0";
                default:
                    throw new VeldridException($"Unsupported D3D12 shader stage: {stage}.");
            }
        }

        private static byte[] CompileHlsl(string sourceCode, string entryPoint, string target)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(sourceCode ?? string.Empty);
            int result = D3DCompile(
                sourceBytes,
                (nuint)sourceBytes.Length,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                entryPoint,
                target,
                0,
                0,
                out IntPtr codeBlobPtr,
                out IntPtr errorBlobPtr);

            string errorMessage = null;
            if (errorBlobPtr != IntPtr.Zero)
            {
                try
                {
                    var errorBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(errorBlobPtr);
                    IntPtr errorPtr = errorBlob.GetBufferPointer();
                    int errorSize = checked((int)errorBlob.GetBufferSize());
                    if (errorSize > 0)
                    {
                        byte[] errorBytes = new byte[errorSize];
                        Marshal.Copy(errorPtr, errorBytes, 0, errorSize);
                        errorMessage = Encoding.UTF8.GetString(errorBytes).TrimEnd('\0', '\r', '\n');
                    }
                }
                finally
                {
                    Marshal.Release(errorBlobPtr);
                }
            }

            if (result < 0 || codeBlobPtr == IntPtr.Zero)
            {
                throw new VeldridException($"Failed to compile D3D12 shader entry '{entryPoint}' target '{target}'. {errorMessage}");
            }

            try
            {
                var codeBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(codeBlobPtr);
                IntPtr codePtr = codeBlob.GetBufferPointer();
                int codeSize = checked((int)codeBlob.GetBufferSize());
                byte[] shaderBytes = new byte[codeSize];
                Marshal.Copy(codePtr, shaderBytes, 0, codeSize);
                return shaderBytes;
            }
            finally
            {
                Marshal.Release(codeBlobPtr);
            }
        }

        [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
        private static extern int D3DCompile(
            byte[] srcData,
            nuint srcDataSize,
            string sourceName,
            IntPtr defines,
            IntPtr include,
            string entryPoint,
            string target,
            uint flags1,
            uint flags2,
            out IntPtr code,
            out IntPtr errorMsgs);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]
        private interface ID3DBlob
        {
            [PreserveSig]
            IntPtr GetBufferPointer();

            [PreserveSig]
            nuint GetBufferSize();
        }
    }
}
