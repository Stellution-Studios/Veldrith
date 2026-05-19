using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLDevice struct.
/// </summary>
public unsafe struct MTLDevice {

    /// <summary>
    /// Represents the MetalFramework field.
    /// </summary>
    private const string MetalFramework = "/System/Library/Frameworks/Metal.framework/Metal";

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Performs the operator IntPtr operation.
    /// </summary>
    /// <param name="device">The value of device.</param>
    /// <returns>The result of the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(MTLDevice device) {
        return device.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLDevice" /> type.
    /// </summary>
    /// <param name="nativePtr">The value of nativePtr.</param>
    public MTLDevice(IntPtr nativePtr) {
        this.NativePtr = nativePtr;
    }

    /// <summary>
    /// Performs the string_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">The value of NativePtr.</param>
    /// <param name="sel_name">The value of sel_name.</param>
    /// <returns>The result of the string_objc_msgSend operation.</returns>
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
    /// Performs the newLibraryWithSource operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the newLibraryWithSource operation.</returns>
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
    /// Performs the newLibraryWithData operation.
    /// </summary>
    /// <param name="data">The value of data.</param>
    /// <returns>The result of the newLibraryWithData operation.</returns>
    public MTLLibrary newLibraryWithData(DispatchData data) {
        IntPtr library = IntPtr_objc_msgSend(this.NativePtr, sel_newLibraryWithData, data.NativePtr, out NSError error);

        if (library == IntPtr.Zero) {
            throw new Exception("Unable to load Metal library: " + error.localizedDescription);
        }

        return new MTLLibrary(library);
    }

    /// <summary>
    /// Performs the newRenderPipelineStateWithDescriptor operation.
    /// </summary>
    /// <param name="desc">The value of desc.</param>
    /// <returns>The result of the newRenderPipelineStateWithDescriptor operation.</returns>
    public MTLRenderPipelineState newRenderPipelineStateWithDescriptor(MTLRenderPipelineDescriptor desc) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newRenderPipelineStateWithDescriptor, desc.NativePtr, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLRenderPipelineState(ret);
    }

    [Pure]

    /// <summary>
    /// Performs the newComputePipelineStateWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">The value of descriptor.</param>
    /// <returns>The result of the newComputePipelineStateWithDescriptor operation.</returns>
    public MTLComputePipelineState newComputePipelineStateWithDescriptor(MTLComputePipelineDescriptor descriptor) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newComputePipelineStateWithDescriptor, descriptor, 0, IntPtr.Zero, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLComputePipelineState(ret);
    }

    /// <summary>
    /// Performs the newCommandQueue operation.
    /// </summary>
    /// <returns>The result of the newCommandQueue operation.</returns>
    public MTLCommandQueue newCommandQueue() {
        return objc_msgSend<MTLCommandQueue>(this.NativePtr, sel_newCommandQueue);
    }

    /// <summary>
    /// Performs the newBuffer operation.
    /// </summary>
    /// <param name="pointer">The value of pointer.</param>
    /// <param name="length">The value of length.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the newBuffer operation.</returns>
    public MTLBuffer newBuffer(void* pointer, UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithBytes, pointer, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Performs the newBufferWithLengthOptions operation.
    /// </summary>
    /// <param name="length">The value of length.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the newBufferWithLengthOptions operation.</returns>
    public MTLBuffer newBufferWithLengthOptions(UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithLength, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Performs the newTextureWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">The value of descriptor.</param>
    /// <returns>The result of the newTextureWithDescriptor operation.</returns>
    public MTLTexture newTextureWithDescriptor(MTLTextureDescriptor descriptor) {
        return objc_msgSend<MTLTexture>(this.NativePtr, sel_newTextureWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Performs the newSamplerStateWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">The value of descriptor.</param>
    /// <returns>The result of the newSamplerStateWithDescriptor operation.</returns>
    public MTLSamplerState newSamplerStateWithDescriptor(MTLSamplerDescriptor descriptor) {
        return objc_msgSend<MTLSamplerState>(this.NativePtr, sel_newSamplerStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Performs the newDepthStencilStateWithDescriptor operation.
    /// </summary>
    /// <param name="descriptor">The value of descriptor.</param>
    /// <returns>The result of the newDepthStencilStateWithDescriptor operation.</returns>
    public MTLDepthStencilState newDepthStencilStateWithDescriptor(MTLDepthStencilDescriptor descriptor) {
        return objc_msgSend<MTLDepthStencilState>(this.NativePtr, sel_newDepthStencilStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Performs the supportsTextureSampleCount operation.
    /// </summary>
    /// <param name="sampleCount">The value of sampleCount.</param>
    /// <returns>The result of the supportsTextureSampleCount operation.</returns>
    public Bool8 supportsTextureSampleCount(UIntPtr sampleCount) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsTextureSampleCount, sampleCount);
    }

    /// <summary>
    /// Performs the supportsFeatureSet operation.
    /// </summary>
    /// <param name="featureSet">The value of featureSet.</param>
    /// <returns>The result of the supportsFeatureSet operation.</returns>
    public Bool8 supportsFeatureSet(MTLFeatureSet featureSet) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsFeatureSet, (uint)featureSet);
    }

    /// <summary>
    /// Gets a value indicating whether the device supports <c>Depth24Stencil8</c> pixel format.
    /// </summary>
    public Bool8 isDepth24Stencil8PixelFormatSupported => bool8_objc_msgSend(this.NativePtr, sel_isDepth24Stencil8PixelFormatSupported);

    [DllImport(MetalFramework)]

    /// <summary>
    /// Performs the MTLCreateSystemDefaultDevice operation.
    /// </summary>
    /// <returns>The result of the MTLCreateSystemDefaultDevice operation.</returns>
    public static extern MTLDevice MTLCreateSystemDefaultDevice();

    [DllImport(MetalFramework)]

    /// <summary>
    /// Performs the MTLCopyAllDevices operation.
    /// </summary>
    /// <returns>The result of the MTLCopyAllDevices operation.</returns>
    public static extern NSArray MTLCopyAllDevices();

    /// <summary>
    /// Represents the sel_name field.
    /// </summary>
    private static readonly Selector sel_name = "name";

    /// <summary>
    /// Represents the sel_maxThreadsPerThreadgroup field.
    /// </summary>
    private static readonly Selector sel_maxThreadsPerThreadgroup = "maxThreadsPerThreadgroup";

    /// <summary>
    /// Represents the sel_newLibraryWithSource field.
    /// </summary>
    private static readonly Selector sel_newLibraryWithSource = "newLibraryWithSource:options:error:";

    /// <summary>
    /// Represents the sel_newLibraryWithData field.
    /// </summary>
    private static readonly Selector sel_newLibraryWithData = "newLibraryWithData:error:";

    /// <summary>
    /// Represents the sel_newRenderPipelineStateWithDescriptor field.
    /// </summary>
    private static readonly Selector sel_newRenderPipelineStateWithDescriptor = "newRenderPipelineStateWithDescriptor:error:";

    /// <summary>
    /// Represents the sel_newComputePipelineStateWithDescriptor field.
    /// </summary>
    private static readonly Selector sel_newComputePipelineStateWithDescriptor = "newComputePipelineStateWithDescriptor:options:reflection:error:";

    /// <summary>
    /// Represents the sel_newCommandQueue field.
    /// </summary>
    private static readonly Selector sel_newCommandQueue = "newCommandQueue";

    /// <summary>
    /// Represents the sel_newBufferWithBytes field.
    /// </summary>
    private static readonly Selector sel_newBufferWithBytes = "newBufferWithBytes:length:options:";

    /// <summary>
    /// Represents the sel_newBufferWithLength field.
    /// </summary>
    private static readonly Selector sel_newBufferWithLength = "newBufferWithLength:options:";

    /// <summary>
    /// Represents the sel_newTextureWithDescriptor field.
    /// </summary>
    private static readonly Selector sel_newTextureWithDescriptor = "newTextureWithDescriptor:";

    /// <summary>
    /// Represents the sel_newSamplerStateWithDescriptor field.
    /// </summary>
    private static readonly Selector sel_newSamplerStateWithDescriptor = "newSamplerStateWithDescriptor:";

    /// <summary>
    /// Represents the sel_newDepthStencilStateWithDescriptor field.
    /// </summary>
    private static readonly Selector sel_newDepthStencilStateWithDescriptor = "newDepthStencilStateWithDescriptor:";

    /// <summary>
    /// Represents the sel_supportsTextureSampleCount field.
    /// </summary>
    private static readonly Selector sel_supportsTextureSampleCount = "supportsTextureSampleCount:";

    /// <summary>
    /// Represents the sel_supportsFeatureSet field.
    /// </summary>
    private static readonly Selector sel_supportsFeatureSet = "supportsFeatureSet:";

    /// <summary>
    /// Represents the sel_isDepth24Stencil8PixelFormatSupported field.
    /// </summary>
    private static readonly Selector sel_isDepth24Stencil8PixelFormatSupported = "isDepth24Stencil8PixelFormatSupported";
}