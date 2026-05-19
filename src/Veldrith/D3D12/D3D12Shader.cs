using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrith.D3D12;

/// <summary>
/// Defines the behavior and responsibilities of the D3D12Shader class.
/// </summary>
internal sealed class D3D12Shader : Shader {

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Shader" /> class.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    /// <summary>
    /// Executes the EnsureDxbcBytecode operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the EnsureDxbcBytecode operation.</returns>
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
    /// Executes the GetTargetProfile operation.
    /// </summary>
    /// <param name="stage">Specifies the value of <paramref name="stage" />.</param>
    /// <returns>Returns the result produced by the GetTargetProfile operation.</returns>
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
    /// Executes the CompileHlsl operation.
    /// </summary>
    /// <param name="sourceCode">Specifies the value of <paramref name="sourceCode" />.</param>
    /// <param name="entryPoint">Specifies the value of <paramref name="entryPoint" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <returns>Returns the result produced by the CompileHlsl operation.</returns>
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
    /// Executes the D3DCompile operation.
    /// </summary>
    /// <param name="srcData">Specifies the value of <paramref name="srcData" />.</param>
    /// <param name="srcDataSize">Specifies the value of <paramref name="srcDataSize" />.</param>
    /// <param name="sourceName">Specifies the value of <paramref name="sourceName" />.</param>
    /// <param name="defines">Specifies the value of <paramref name="defines" />.</param>
    /// <param name="include">Specifies the value of <paramref name="include" />.</param>
    /// <param name="entryPoint">Specifies the value of <paramref name="entryPoint" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="flags1">Specifies the value of <paramref name="flags1" />.</param>
    /// <param name="flags2">Specifies the value of <paramref name="flags2" />.</param>
    /// <param name="code">Specifies the value of <paramref name="code" />.</param>
    /// <param name="errorMsgs">Specifies the value of <paramref name="errorMsgs" />.</param>
    /// <returns>Returns the result produced by the D3DCompile operation.</returns>
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
        /// Executes the GetBufferPointer operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetBufferPointer operation.</returns>
        IntPtr GetBufferPointer();

        [PreserveSig]

        /// <summary>
        /// Executes the GetBufferSize operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetBufferSize operation.</returns>
        nuint GetBufferSize();
    }
}
