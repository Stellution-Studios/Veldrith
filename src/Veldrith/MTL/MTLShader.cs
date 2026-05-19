using System;
using System.Text;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlShader : Shader {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlShader" /> class.
    /// </summary>
    public unsafe MtlShader(ref ShaderDescription description, MtlGraphicsDevice gd)
        : base(description.Stage, description.EntryPoint) {
        if (description.ShaderBytes.Length > 4
            && description.ShaderBytes[0] == 0x4d
            && description.ShaderBytes[1] == 0x54
            && description.ShaderBytes[2] == 0x4c
            && description.ShaderBytes[3] == 0x42) {
            DispatchQueue queue = Dispatch.dispatch_get_global_queue(QualityOfServiceLevel.QOS_CLASS_USER_INTERACTIVE, 0);

            fixed (byte* shaderBytesPtr = description.ShaderBytes) {
                DispatchData dispatchData = Dispatch.dispatch_data_create(shaderBytesPtr, (UIntPtr)description.ShaderBytes.Length, queue, IntPtr.Zero);

                try {
                    this.Library = gd.Device.newLibraryWithData(dispatchData);
                }
                finally {
                    Dispatch.dispatch_release(dispatchData.NativePtr);
                }
            }
        }
        else {
            string source = Encoding.UTF8.GetString(description.ShaderBytes);
            MTLCompileOptions compileOptions = MTLCompileOptions.New();
            this.Library = gd.Device.newLibraryWithSource(source, compileOptions);
            ObjectiveCRuntime.release(compileOptions);
        }

        this.Function = this.Library.newFunctionWithName(description.EntryPoint);

        if (this.Function.NativePtr == IntPtr.Zero) {
            throw new VeldridException($"Failed to create Metal {description.Stage} Shader. The given entry point \"{description.EntryPoint}\" was not found.");
        }

        this.HasFunctionConstants = this.Function.functionConstantsDictionary.count != UIntPtr.Zero;
    }

    /// <summary>
    /// Gets or sets HasFunctionConstants.
    /// </summary>
    public bool HasFunctionConstants { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Library.
    /// </summary>
    public MTLLibrary Library { get; }

    /// <summary>
    /// Gets or sets Function.
    /// </summary>
    public MTLFunction Function { get; }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.release(this.Function.NativePtr);
            ObjectiveCRuntime.release(this.Library.NativePtr);
        }
    }

    #endregion
}