using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Shader.
/// </summary>
internal sealed class D3D12Shader : Shader {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Shader" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Shader(ref ShaderDescription description) : base(description.Stage, description.EntryPoint) {
        this.ShaderBytes = EnsureDxbcBytecode(description);
        this.Debug = description.Debug;
    }

    /// <summary>
    /// Gets or sets ShaderBytes.
    /// </summary>
    public byte[] ShaderBytes { get; }

    /// <summary>
    /// Gets or sets Debug.
    /// </summary>
    public bool Debug { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    /// <summary>
    /// Executes the ensure dxbc bytecode logic for this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static byte[] EnsureDxbcBytecode(ShaderDescription description) {
        byte[] source = description.ShaderBytes ?? Array.Empty<byte>();
        if (source.Length >= 4
            && source[0] == (byte)'D'
            && source[1] == (byte)'X'
            && source[2] == (byte)'B'
            && source[3] == (byte)'C') {
            return source;
        }

        string sourceText = Encoding.UTF8.GetString(source);
        string targetProfile = GetTargetProfile(description.Stage);
        return CompileHlsl(sourceText, description.EntryPoint, targetProfile);
    }

    /// <summary>
    /// Gets the target profile value.
    /// </summary>
    /// <param name="stage">The stage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetTargetProfile(ShaderStages stage) {
        switch (stage) {
            case ShaderStages.Vertex: return "vs_5_0";
            case ShaderStages.Fragment: return "ps_5_0";
            case ShaderStages.Geometry: return "gs_5_0";
            case ShaderStages.TessellationControl: return "hs_5_0";
            case ShaderStages.TessellationEvaluation: return "ds_5_0";
            case ShaderStages.Compute: return "cs_5_0";
            default: throw new VeldridException($"Unsupported D3D12 shader stage: {stage}.");
        }
    }

    /// <summary>
    /// Executes the compile hlsl logic for this backend.
    /// </summary>
    /// <param name="sourceCode">The source code value used by this operation.</param>
    /// <param name="entryPoint">The entry point value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static byte[] CompileHlsl(string sourceCode, string entryPoint, string target) {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(sourceCode ?? string.Empty);
        int result = D3DCompile(sourceBytes, (nuint)sourceBytes.Length, null, IntPtr.Zero, IntPtr.Zero, entryPoint, target, 0, 0, out IntPtr codeBlobPtr, out IntPtr errorBlobPtr);

        string errorMessage = null;
        if (errorBlobPtr != IntPtr.Zero) {
            try {
                ID3DBlob errorBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(errorBlobPtr);
                IntPtr errorPtr = errorBlob.GetBufferPointer();
                int errorSize = checked((int)errorBlob.GetBufferSize());
                if (errorSize > 0) {
                    byte[] errorBytes = new byte[errorSize];
                    Marshal.Copy(errorPtr, errorBytes, 0, errorSize);
                    errorMessage = Encoding.UTF8.GetString(errorBytes).TrimEnd('\0', '\r', '\n');
                }
            }
            finally {
                Marshal.Release(errorBlobPtr);
            }
        }

        if (result < 0 || codeBlobPtr == IntPtr.Zero) {
            throw new VeldridException($"Failed to compile D3D12 shader entry '{entryPoint}' target '{target}'. {errorMessage}");
        }

        try {
            ID3DBlob codeBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(codeBlobPtr);
            IntPtr codePtr = codeBlob.GetBufferPointer();
            int codeSize = checked((int)codeBlob.GetBufferSize());
            byte[] shaderBytes = new byte[codeSize];
            Marshal.Copy(codePtr, shaderBytes, 0, codeSize);
            return shaderBytes;
        }
        finally {
            Marshal.Release(codeBlobPtr);
        }
    }

    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]

    /// <summary>
    /// Executes the d3 dcompile logic for this backend.
    /// </summary>
    /// <param name="srcData">The src data value used by this operation.</param>
    /// <param name="srcDataSize">The src data size value used by this operation.</param>
    /// <param name="sourceName">The source name value used by this operation.</param>
    /// <param name="defines">The defines value used by this operation.</param>
    /// <param name="include">The include value used by this operation.</param>
    /// <param name="entryPoint">The entry point value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="flags1">The flags1 value used by this operation.</param>
    /// <param name="flags2">The flags2 value used by this operation.</param>
    /// <param name="code">The code value used by this operation.</param>
    /// <param name="errorMsgs">The error msgs value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static extern int D3DCompile(byte[] srcData, nuint srcDataSize, string sourceName, IntPtr defines, IntPtr include, string entryPoint, string target, uint flags1, uint flags2, out IntPtr code, out IntPtr errorMsgs);

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]

    /// <summary>
    /// Defines the ID3DBlob interface.
    /// </summary>
    private interface ID3DBlob {
        [PreserveSig]

        /// <summary>
        /// Gets the buffer pointer value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        IntPtr GetBufferPointer();

        [PreserveSig]

        /// <summary>
        /// Gets the buffer size value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        nuint GetBufferSize();
    }
}