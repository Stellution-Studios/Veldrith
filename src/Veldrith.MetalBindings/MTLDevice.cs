using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLDevice struct.
/// </summary>
public unsafe struct MTLDevice {

    /// <summary>
    /// Stores the value associated with <c>MetalFramework</c>.
    /// </summary>
    private const string MetalFramework = "/System/Library/Frameworks/Metal.framework/Metal";

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="device">Specifies the value of <paramref name="device" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(MTLDevice device) {
        return device.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLDevice" /> type.
    /// </summary>
    /// <param name="nativePtr">Specifies the value of <paramref name="nativePtr" />.</param>
    public MTLDevice(IntPtr nativePtr) {
        this.NativePtr = nativePtr;
    }

    /// <summary>
    /// Executes the string_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">Specifies the value of <paramref name="NativePtr" />.</param>
    /// <param name="sel_name">Specifies the value of <paramref name="sel_name" />.</param>
    /// <returns>Returns the result produced by the string_objc_msgSend operation.</returns>
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
    /// Executes the newLibraryWithSource operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the newLibraryWithSource operation.</returns>
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
    /// Executes the newLibraryWithData operation.
    /// </summary>
    /// <param name="data">Specifies the value of <paramref name="data" />.</param>
    /// <returns>Returns the result produced by the newLibraryWithData operation.</returns>
    public MTLLibrary newLibraryWithData(DispatchData data) {
        IntPtr library = IntPtr_objc_msgSend(this.NativePtr, sel_newLibraryWithData, data.NativePtr, out NSError error);

        if (library == IntPtr.Zero) {
            throw new Exception("Unable to load Metal library: " + error.localizedDescription);
        }

        return new MTLLibrary(library);
    }

    /// <summary>
    /// Executes the newRenderPipelineStateWithDescriptor operation.
    /// </summary>
    /// <param name="desc">Specifies the value of <paramref name="desc" />.</param>
    /// <returns>Returns the result produced by the newRenderPipelineStateWithDescriptor operation.</returns>
    public MTLRenderPipelineState newRenderPipelineStateWithDescriptor(MTLRenderPipelineDescriptor desc) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newRenderPipelineStateWithDescriptor, desc.NativePtr, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLRenderPipelineState(ret);
    }

    [Pure]

    /// <summary>
    /// Executes the newComputePipelineStateWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">Specifies the value of <paramref name="descriptor" />.</param>
    /// <returns>Returns the result produced by the newComputePipelineStateWithDescriptor operation.</returns>
    public MTLComputePipelineState newComputePipelineStateWithDescriptor(MTLComputePipelineDescriptor descriptor) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newComputePipelineStateWithDescriptor, descriptor, 0, IntPtr.Zero, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLComputePipelineState(ret);
    }

    /// <summary>
    /// Executes the newCommandQueue operation.
    /// </summary>
    /// <returns>Returns the result produced by the newCommandQueue operation.</returns>
    public MTLCommandQueue newCommandQueue() {
        return objc_msgSend<MTLCommandQueue>(this.NativePtr, sel_newCommandQueue);
    }

    /// <summary>
    /// Executes the newBuffer operation.
    /// </summary>
    /// <param name="pointer">Specifies the value of <paramref name="pointer" />.</param>
    /// <param name="length">Specifies the value of <paramref name="length" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the newBuffer operation.</returns>
    public MTLBuffer newBuffer(void* pointer, UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithBytes, pointer, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes the newBufferWithLengthOptions operation.
    /// </summary>
    /// <param name="length">Specifies the value of <paramref name="length" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the newBufferWithLengthOptions operation.</returns>
    public MTLBuffer newBufferWithLengthOptions(UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithLength, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes the newTextureWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">Specifies the value of <paramref name="descriptor" />.</param>
    /// <returns>Returns the result produced by the newTextureWithDescriptor operation.</returns>
    public MTLTexture newTextureWithDescriptor(MTLTextureDescriptor descriptor) {
        return objc_msgSend<MTLTexture>(this.NativePtr, sel_newTextureWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the newSamplerStateWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">Specifies the value of <paramref name="descriptor" />.</param>
    /// <returns>Returns the result produced by the newSamplerStateWithDescriptor operation.</returns>
    public MTLSamplerState newSamplerStateWithDescriptor(MTLSamplerDescriptor descriptor) {
        return objc_msgSend<MTLSamplerState>(this.NativePtr, sel_newSamplerStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the newDepthStencilStateWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">Specifies the value of <paramref name="descriptor" />.</param>
    /// <returns>Returns the result produced by the newDepthStencilStateWithDescriptor operation.</returns>
    public MTLDepthStencilState newDepthStencilStateWithDescriptor(MTLDepthStencilDescriptor descriptor) {
        return objc_msgSend<MTLDepthStencilState>(this.NativePtr, sel_newDepthStencilStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes the supportsTextureSampleCount operation.
    /// </summary>
    /// <param name="sampleCount">Specifies the value of <paramref name="sampleCount" />.</param>
    /// <returns>Returns the result produced by the supportsTextureSampleCount operation.</returns>
    public Bool8 supportsTextureSampleCount(UIntPtr sampleCount) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsTextureSampleCount, sampleCount);
    }

    /// <summary>
    /// Executes the supportsFeatureSet operation.
    /// </summary>
    /// <param name="featureSet">Specifies the value of <paramref name="featureSet" />.</param>
    /// <returns>Returns the result produced by the supportsFeatureSet operation.</returns>
    public Bool8 supportsFeatureSet(MTLFeatureSet featureSet) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsFeatureSet, (uint)featureSet);
    }

    /// <summary>
    /// Gets a value indicating whether the device supports <c>Depth24Stencil8</c> pixel format.
    /// </summary>
    public Bool8 isDepth24Stencil8PixelFormatSupported => bool8_objc_msgSend(this.NativePtr, sel_isDepth24Stencil8PixelFormatSupported);

    [DllImport(MetalFramework)]

    /// <summary>
    /// Executes the MTLCreateSystemDefaultDevice operation.
    /// </summary>
    /// <returns>Returns the result produced by the MTLCreateSystemDefaultDevice operation.</returns>
    public static extern MTLDevice MTLCreateSystemDefaultDevice();

    [DllImport(MetalFramework)]

    /// <summary>
    /// Executes the MTLCopyAllDevices operation.
    /// </summary>
    /// <returns>Returns the result produced by the MTLCopyAllDevices operation.</returns>
    public static extern NSArray MTLCopyAllDevices();

    /// <summary>
    /// Stores the value associated with <c>sel_name</c>.
    /// </summary>
    private static readonly Selector sel_name = "name";

    /// <summary>
    /// Stores the value associated with <c>sel_maxThreadsPerThreadgroup</c>.
    /// </summary>
    private static readonly Selector sel_maxThreadsPerThreadgroup = "maxThreadsPerThreadgroup";

    /// <summary>
    /// Stores the value associated with <c>sel_newLibraryWithSource</c>.
    /// </summary>
    private static readonly Selector sel_newLibraryWithSource = "newLibraryWithSource:options:error:";

    /// <summary>
    /// Stores the value associated with <c>sel_newLibraryWithData</c>.
    /// </summary>
    private static readonly Selector sel_newLibraryWithData = "newLibraryWithData:error:";

    /// <summary>
    /// Stores the value associated with <c>sel_newRenderPipelineStateWithDescriptor</c>.
    /// </summary>
    private static readonly Selector sel_newRenderPipelineStateWithDescriptor = "newRenderPipelineStateWithDescriptor:error:";

    /// <summary>
    /// Stores the value associated with <c>sel_newComputePipelineStateWithDescriptor</c>.
    /// </summary>
    private static readonly Selector sel_newComputePipelineStateWithDescriptor = "newComputePipelineStateWithDescriptor:options:reflection:error:";

    /// <summary>
    /// Stores the value associated with <c>sel_newCommandQueue</c>.
    /// </summary>
    private static readonly Selector sel_newCommandQueue = "newCommandQueue";

    /// <summary>
    /// Stores the value associated with <c>sel_newBufferWithBytes</c>.
    /// </summary>
    private static readonly Selector sel_newBufferWithBytes = "newBufferWithBytes:length:options:";

    /// <summary>
    /// Stores the value associated with <c>sel_newBufferWithLength</c>.
    /// </summary>
    private static readonly Selector sel_newBufferWithLength = "newBufferWithLength:options:";

    /// <summary>
    /// Stores the value associated with <c>sel_newTextureWithDescriptor</c>.
    /// </summary>
    private static readonly Selector sel_newTextureWithDescriptor = "newTextureWithDescriptor:";

    /// <summary>
    /// Stores the value associated with <c>sel_newSamplerStateWithDescriptor</c>.
    /// </summary>
    private static readonly Selector sel_newSamplerStateWithDescriptor = "newSamplerStateWithDescriptor:";

    /// <summary>
    /// Stores the value associated with <c>sel_newDepthStencilStateWithDescriptor</c>.
    /// </summary>
    private static readonly Selector sel_newDepthStencilStateWithDescriptor = "newDepthStencilStateWithDescriptor:";

    /// <summary>
    /// Stores the value associated with <c>sel_supportsTextureSampleCount</c>.
    /// </summary>
    private static readonly Selector sel_supportsTextureSampleCount = "supportsTextureSampleCount:";

    /// <summary>
    /// Stores the value associated with <c>sel_supportsFeatureSet</c>.
    /// </summary>
    private static readonly Selector sel_supportsFeatureSet = "supportsFeatureSet:";

    /// <summary>
    /// Stores the value associated with <c>sel_isDepth24Stencil8PixelFormatSupported</c>.
    /// </summary>
    private static readonly Selector sel_isDepth24Stencil8PixelFormatSupported = "isDepth24Stencil8PixelFormatSupported";
}