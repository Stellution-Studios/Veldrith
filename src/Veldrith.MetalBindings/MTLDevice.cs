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

    public string name => string_objc_msgSend(this.NativePtr, sel_name);

    /// <summary>
    /// Gets or sets maxThreadsPerThreadgroup.
    /// </summary>
    public MTLSize maxThreadsPerThreadgroup {
        get {
            if (UseStret<MTLSize>()) {
                return objc_msgSend_stret<MTLSize>(this, sel_maxThreadsPerThreadgroup);
            }

            return MTLSize_objc_msgSend(this, sel_maxThreadsPerThreadgroup);
        }
    }

    /// <summary>
    /// Executes the new library with source logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLLibrary newLibraryWithSource(string source, MTLCompileOptions options) {
        NSString sourceNSS = NSString.New(source);

        IntPtr library = IntPtr_objc_msgSend(this.NativePtr, sel_newLibraryWithSource, sourceNSS, options, out NSError error);

        release(sourceNSS.NativePtr);

        if (library == IntPtr.Zero) {
            throw new Exception("Shader compilation failed: " + error.localizedDescription);
        }

        return new MTLLibrary(library);
    }

    /// <summary>
    /// Executes the new library with data logic for this backend.
    /// </summary>
    /// <param name="data">The data value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLLibrary newLibraryWithData(DispatchData data) {
        IntPtr library = IntPtr_objc_msgSend(this.NativePtr, sel_newLibraryWithData, data.NativePtr, out NSError error);

        if (library == IntPtr.Zero) {
            throw new Exception("Unable to load Metal library: " + error.localizedDescription);
        }

        return new MTLLibrary(library);
    }

    /// <summary>
    /// Executes the new render pipeline state with descriptor logic for this backend.
    /// </summary>
    /// <param name="desc">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLRenderPipelineState newRenderPipelineStateWithDescriptor(MTLRenderPipelineDescriptor desc) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newRenderPipelineStateWithDescriptor, desc.NativePtr, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLRenderPipelineState(ret);
    }

    [Pure]

    /// <summary>
    /// Executes the new compute pipeline state with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLComputePipelineState newComputePipelineStateWithDescriptor(MTLComputePipelineDescriptor descriptor) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newComputePipelineStateWithDescriptor, descriptor, 0, IntPtr.Zero, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLComputePipelineState(ret);
    }

    /// <summary>
    /// Executes the new command queue logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public MTLCommandQueue newCommandQueue() {
        return objc_msgSend<MTLCommandQueue>(this.NativePtr, sel_newCommandQueue);
    }

    /// <summary>
    /// Executes the new buffer logic for this backend.
    /// </summary>
    /// <param name="pointer">The pointer value used by this operation.</param>
    /// <param name="length">The number of items involved in this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLBuffer newBuffer(void* pointer, UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithBytes, pointer, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes the new buffer with length options logic for this backend.
    /// </summary>
    /// <param name="length">The number of items involved in this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLBuffer newBufferWithLengthOptions(UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithLength, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes the new texture with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLTexture newTextureWithDescriptor(MTLTextureDescriptor descriptor) {
        return objc_msgSend<MTLTexture>(this.NativePtr, sel_newTextureWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the new sampler state with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLSamplerState newSamplerStateWithDescriptor(MTLSamplerDescriptor descriptor) {
        return objc_msgSend<MTLSamplerState>(this.NativePtr, sel_newSamplerStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the new depth stencil state with descriptor logic for this backend.
    /// </summary>
    /// <param name="descriptor">The descriptor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLDepthStencilState newDepthStencilStateWithDescriptor(MTLDepthStencilDescriptor descriptor) {
        return objc_msgSend<MTLDepthStencilState>(this.NativePtr, sel_newDepthStencilStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the supports texture sample count logic for this backend.
    /// </summary>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Bool8 supportsTextureSampleCount(UIntPtr sampleCount) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsTextureSampleCount, sampleCount);
    }

    /// <summary>
    /// Executes the supports feature set logic for this backend.
    /// </summary>
    /// <param name="featureSet">The feature set value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Bool8 supportsFeatureSet(MTLFeatureSet featureSet) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsFeatureSet, (uint)featureSet);
    }

    /// <summary>
    /// Gets a value indicating whether the device supports <c>Depth24Stencil8</c> pixel format.
    /// </summary>

    public Bool8 isDepth24Stencil8PixelFormatSupported => bool8_objc_msgSend(this.NativePtr, sel_isDepth24Stencil8PixelFormatSupported);

    [DllImport(MetalFramework)]

    /// <summary>
    /// Executes the mtlcreate system default device logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static extern MTLDevice MTLCreateSystemDefaultDevice();

    [DllImport(MetalFramework)]

    /// <summary>
    /// Executes the mtlcopy all devices logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static extern NSArray MTLCopyAllDevices();

    /// <summary>
    /// Stores the sel name state used by this instance.
    /// </summary>
    private static readonly Selector sel_name = "name";

    /// <summary>
    /// Stores the sel max threads per threadgroup state used by this instance.
    /// </summary>
    private static readonly Selector sel_maxThreadsPerThreadgroup = "maxThreadsPerThreadgroup";

    /// <summary>
    /// Stores the sel new library with source state used by this instance.
    /// </summary>
    private static readonly Selector sel_newLibraryWithSource = "newLibraryWithSource:options:error:";

    /// <summary>
    /// Stores the sel new library with data state used by this instance.
    /// </summary>
    private static readonly Selector sel_newLibraryWithData = "newLibraryWithData:error:";

    /// <summary>
    /// Stores the sel new render pipeline state with descriptor state used by this instance.
    /// </summary>
    private static readonly Selector sel_newRenderPipelineStateWithDescriptor = "newRenderPipelineStateWithDescriptor:error:";

    /// <summary>
    /// Stores the sel new compute pipeline state with descriptor state used by this instance.
    /// </summary>
    private static readonly Selector sel_newComputePipelineStateWithDescriptor = "newComputePipelineStateWithDescriptor:options:reflection:error:";

    /// <summary>
    /// Stores the sel new command queue state used by this instance.
    /// </summary>
    private static readonly Selector sel_newCommandQueue = "newCommandQueue";

    /// <summary>
    /// Stores the sel new buffer with bytes state used by this instance.
    /// </summary>
    private static readonly Selector sel_newBufferWithBytes = "newBufferWithBytes:length:options:";

    /// <summary>
    /// Stores the sel new buffer with length state used by this instance.
    /// </summary>
    private static readonly Selector sel_newBufferWithLength = "newBufferWithLength:options:";

    /// <summary>
    /// Stores the sel new texture with descriptor state used by this instance.
    /// </summary>
    private static readonly Selector sel_newTextureWithDescriptor = "newTextureWithDescriptor:";

    /// <summary>
    /// Stores the sel new sampler state with descriptor collection used by this instance.
    /// </summary>
    private static readonly Selector sel_newSamplerStateWithDescriptor = "newSamplerStateWithDescriptor:";

    /// <summary>
    /// Stores the sel new depth stencil state with descriptor value used during command execution.
    /// </summary>
    private static readonly Selector sel_newDepthStencilStateWithDescriptor = "newDepthStencilStateWithDescriptor:";

    /// <summary>
    /// Stores the sel supports texture sample count collection used by this instance.
    /// </summary>
    private static readonly Selector sel_supportsTextureSampleCount = "supportsTextureSampleCount:";

    /// <summary>
    /// Stores the sel supports feature set state used by this instance.
    /// </summary>
    private static readonly Selector sel_supportsFeatureSet = "supportsFeatureSet:";

    /// <summary>
    /// Stores the sel is depth24 stencil8 pixel format supported value used during command execution.
    /// </summary>
    private static readonly Selector sel_isDepth24Stencil8PixelFormatSupported = "isDepth24Stencil8PixelFormatSupported";
}