using System;
using System.Text;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlShader : Shader
    {
        public bool HasFunctionConstants { get; }
        public override bool IsDisposed => disposed;

        public MTLLibrary Library { get; }
        public MTLFunction Function { get; }
        public override string Name { get; set; }
        private bool disposed;

        public unsafe MtlShader(ref ShaderDescription description, MtlGraphicsDevice gd)
            : base(description.Stage, description.EntryPoint)
        {
            if (description.ShaderBytes.Length > 4
                && description.ShaderBytes[0] == 0x4d
                && description.ShaderBytes[1] == 0x54
                && description.ShaderBytes[2] == 0x4c
                && description.ShaderBytes[3] == 0x42)
            {
                var queue = Dispatch.dispatch_get_global_queue(QualityOfServiceLevel.QOS_CLASS_USER_INTERACTIVE, 0);

                fixed (byte* shaderBytesPtr = description.ShaderBytes)
                {
                    var dispatchData = Dispatch.dispatch_data_create(
                        shaderBytesPtr,
                        (UIntPtr)description.ShaderBytes.Length,
                        queue,
                        IntPtr.Zero);

                    try
                    {
                        Library = gd.Device.newLibraryWithData(dispatchData);
                    }
                    finally
                    {
                        Dispatch.dispatch_release(dispatchData.NativePtr);
                    }
                }
            }
            else
            {
                string source = Encoding.UTF8.GetString(description.ShaderBytes);
                var compileOptions = MTLCompileOptions.New();
                Library = gd.Device.newLibraryWithSource(source, compileOptions);
                ObjectiveCRuntime.release(compileOptions);
            }

            Function = Library.newFunctionWithName(description.EntryPoint);

            if (Function.NativePtr == IntPtr.Zero)
            {
                throw new VeldridException(
                    $"Failed to create Metal {description.Stage} Shader. The given entry point \"{description.EntryPoint}\" was not found.");
            }

            HasFunctionConstants = Function.functionConstantsDictionary.count != UIntPtr.Zero;
        }

        #region Disposal

        public override void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                ObjectiveCRuntime.release(Function.NativePtr);
                ObjectiveCRuntime.release(Library.NativePtr);
            }
        }

        #endregion
    }
}
