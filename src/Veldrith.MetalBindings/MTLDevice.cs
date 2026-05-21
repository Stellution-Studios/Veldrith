using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Provides Objective-C interop bindings for MTLDevice.
/// </summary>
public unsafe struct MTLDevice {

    /// <summary>
    /// Stores the metal framework state used by this instance.
    /// </summary>
    private const string MetalFramework = "/System/Library/Frameworks/Metal.framework/Metal";

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="device">The device value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(MTLDevice device) {
        return device.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLDevice" /> type.
    /// </summary>
    /// <param name="nativePtr">The native ptr value used by this operation.</param>
    public MTLDevice(IntPtr nativePtr) {
        this.NativePtr = nativePtr;
    }

    /// <summary>
    /// Executes the string objc msg send logic for this backend.
    /// </summary>

    public string name => StringObjcMsgSend(this.NativePtr, _selName);

    /// <summary>
    /// Gets or sets maxThreadsPerThreadgroup.
    /// </summary>
    public MTLSize MaxThreadsPerThreadgroup {
        get {
            if (UseStret<MTLSize>()) {
                return ObjcMsgSendStret<MTLSize>(this, _selMaxThreadsPerThreadgroup);
            }

            return MTLSize_objc_msgSend(this, _selMaxThreadsPerThreadgroup);
        }
    }

    /// <summary>
    /// Executes the new library with source logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLLibrary NewLibraryWithSource(string source, MTLCompileOptions options) {
        NSString sourceNSS = NSString.New(source);

        IntPtr library = IntPtr_objc_msgSend(this.NativePtr, _selNewLibraryWithSource, sourceNSS, options, out NSError error);

        Release(sourceNSS.NativePtr);

        if (library == IntPtr.Zero) {
            throw new Exception("Shader compilation failed: " + error.LocalizedDescription);
        }

        return new MTLLibrary(library);
    }

    /// <summary>
    /// Executes the new library with data logic for this backend.
    /// </summary>
    /// <param name="data">The data value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLLibrary NewLibraryWithData(DispatchData data) {
        IntPtr library = IntPtr_objc_msgSend(this.NativePtr, _selNewLibraryWithData, data.NativePtr, out NSError error);

        if (library == IntPtr.Zero) {
            throw new Exception("Unable to load Metal library: " + error.LocalizedDescription);
        }

        return new MTLLibrary(library);
    }

    /// <summary>
    /// Executes the new render pipeline state with descriptor logic for this backend.
    /// </summary>
    /// <param name="desc">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLRenderPipelineState NewRenderPipelineStateWithDescriptor(MTLRenderPipelineDescriptor desc) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, _selNewRenderPipelineStateWithDescriptor, desc.NativePtr, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.LocalizedDescription);
        }

        return new MTLRenderPipelineState(ret);
    }

    /// <summary>
    /// Executes the new compute pipeline state with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [Pure]
    public MTLComputePipelineState NewComputePipelineStateWithDescriptor(MTLComputePipelineDescriptor descriptor) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, _selNewComputePipelineStateWithDescriptor, descriptor, 0, IntPtr.Zero, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.LocalizedDescription);
        }

        return new MTLComputePipelineState(ret);
    }

    /// <summary>
    /// Executes the new command queue logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public MTLCommandQueue NewCommandQueue() {
        return ObjcMsgSend<MTLCommandQueue>(this.NativePtr, _selNewCommandQueue);
    }

    /// <summary>
    /// Executes the new buffer logic for this backend.
    /// </summary>
    /// <param name="pointer">The pointer value used by this operation.</param>
    /// <param name="length">The number of items involved in this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLBuffer NewBuffer(void* pointer, UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, _selNewBufferWithBytes, pointer, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes the new buffer with length options logic for this backend.
    /// </summary>
    /// <param name="length">The number of items involved in this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLBuffer NewBufferWithLengthOptions(UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, _selNewBufferWithLength, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes the new texture with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLTexture NewTextureWithDescriptor(MTLTextureDescriptor descriptor) {
        return ObjcMsgSend<MTLTexture>(this.NativePtr, _selNewTextureWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the new sampler state with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLSamplerState NewSamplerStateWithDescriptor(MTLSamplerDescriptor descriptor) {
        return ObjcMsgSend<MTLSamplerState>(this.NativePtr, _selNewSamplerStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the new depth stencil state with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLDepthStencilState NewDepthStencilStateWithDescriptor(MTLDepthStencilDescriptor descriptor) {
        return ObjcMsgSend<MTLDepthStencilState>(this.NativePtr, _selNewDepthStencilStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the supports texture sample count logic for this backend.
    /// </summary>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Bool8 SupportsTextureSampleCount(UIntPtr sampleCount) {
        return Bool8ObjcMsgSend(this.NativePtr, _selSupportsTextureSampleCount, sampleCount);
    }

    /// <summary>
    /// Executes the supports feature set logic for this backend.
    /// </summary>
    /// <param name="featureSet">The feature set value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Bool8 SupportsFeatureSet(MTLFeatureSet featureSet) {
        return Bool8ObjcMsgSend(this.NativePtr, _selSupportsFeatureSet, (uint)featureSet);
    }

    /// <summary>
    /// Gets a value indicating whether the device supports <c>Depth24Stencil8</c> pixel format.
    /// </summary>

    public Bool8 IsDepth24Stencil8PixelFormatSupported => Bool8ObjcMsgSend(this.NativePtr, _selIsDepth24Stencil8PixelFormatSupported);

    /// <summary>
    /// Executes the mtlcreate system default device logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(MetalFramework)]
    public static extern MTLDevice MTLCreateSystemDefaultDevice();
    
    /// <summary>
    /// Executes the mtlcopy all devices logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [DllImport(MetalFramework)]
    public static extern NSArray MTLCopyAllDevices();

    /// <summary>
    /// Stores the sel name state used by this instance.
    /// </summary>
    private static readonly Selector _selName = "name";

    /// <summary>
    /// Stores the sel max threads per threadgroup state used by this instance.
    /// </summary>
    private static readonly Selector _selMaxThreadsPerThreadgroup = "maxThreadsPerThreadgroup";

    /// <summary>
    /// Stores the sel new library with source state used by this instance.
    /// </summary>
    private static readonly Selector _selNewLibraryWithSource = "newLibraryWithSource:options:error:";

    /// <summary>
    /// Stores the sel new library with data state used by this instance.
    /// </summary>
    private static readonly Selector _selNewLibraryWithData = "newLibraryWithData:error:";

    /// <summary>
    /// Stores the sel new render pipeline state with descriptor state used by this instance.
    /// </summary>
    private static readonly Selector _selNewRenderPipelineStateWithDescriptor = "newRenderPipelineStateWithDescriptor:error:";

    /// <summary>
    /// Stores the sel new compute pipeline state with descriptor state used by this instance.
    /// </summary>
    private static readonly Selector _selNewComputePipelineStateWithDescriptor = "newComputePipelineStateWithDescriptor:options:reflection:error:";

    /// <summary>
    /// Stores the sel new command queue state used by this instance.
    /// </summary>
    private static readonly Selector _selNewCommandQueue = "newCommandQueue";

    /// <summary>
    /// Stores the sel new buffer with bytes state used by this instance.
    /// </summary>
    private static readonly Selector _selNewBufferWithBytes = "newBufferWithBytes:length:options:";

    /// <summary>
    /// Stores the sel new buffer with length state used by this instance.
    /// </summary>
    private static readonly Selector _selNewBufferWithLength = "newBufferWithLength:options:";

    /// <summary>
    /// Stores the sel new texture with descriptor state used by this instance.
    /// </summary>
    private static readonly Selector _selNewTextureWithDescriptor = "newTextureWithDescriptor:";

    /// <summary>
    /// Stores the sel new sampler state with descriptor collection used by this instance.
    /// </summary>
    private static readonly Selector _selNewSamplerStateWithDescriptor = "newSamplerStateWithDescriptor:";

    /// <summary>
    /// Stores the sel new depth stencil state with descriptor value used during command execution.
    /// </summary>
    private static readonly Selector _selNewDepthStencilStateWithDescriptor = "newDepthStencilStateWithDescriptor:";

    /// <summary>
    /// Stores the sel supports texture sample count collection used by this instance.
    /// </summary>
    private static readonly Selector _selSupportsTextureSampleCount = "supportsTextureSampleCount:";

    /// <summary>
    /// Stores the sel supports feature set state used by this instance.
    /// </summary>
    private static readonly Selector _selSupportsFeatureSet = "supportsFeatureSet:";

    /// <summary>
    /// Stores the sel is depth24 stencil8 pixel format supported value used during command execution.
    /// </summary>
    private static readonly Selector _selIsDepth24Stencil8PixelFormatSupported = "isDepth24Stencil8PixelFormatSupported";
}
