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
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(MTLDevice device) {
        return device.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLDevice" /> class.
    /// </summary>
    public MTLDevice(IntPtr nativePtr) {
        this.NativePtr = nativePtr;
    }

    /// <summary>
    /// Gets or sets name.
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
    /// Executes newLibraryWithSource.
    /// </summary>
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
    /// Executes newLibraryWithData.
    /// </summary>
    public MTLLibrary newLibraryWithData(DispatchData data) {
        IntPtr library = IntPtr_objc_msgSend(this.NativePtr, sel_newLibraryWithData, data.NativePtr, out NSError error);

        if (library == IntPtr.Zero) {
            throw new Exception("Unable to load Metal library: " + error.localizedDescription);
        }

        return new MTLLibrary(library);
    }

    /// <summary>
    /// Executes newRenderPipelineStateWithDescriptor.
    /// </summary>
    public MTLRenderPipelineState newRenderPipelineStateWithDescriptor(MTLRenderPipelineDescriptor desc) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newRenderPipelineStateWithDescriptor, desc.NativePtr, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLRenderPipelineState(ret);
    }

    [Pure]

    /// <summary>
    /// Executes newComputePipelineStateWithDescriptor.
    /// </summary>
    public MTLComputePipelineState newComputePipelineStateWithDescriptor(MTLComputePipelineDescriptor descriptor) {
        IntPtr ret = IntPtr_objc_msgSend(this.NativePtr, sel_newComputePipelineStateWithDescriptor, descriptor, 0, IntPtr.Zero, out NSError error);

        if (error.NativePtr != IntPtr.Zero) {
            throw new Exception("Failed to create new MTLRenderPipelineState: " + error.localizedDescription);
        }

        return new MTLComputePipelineState(ret);
    }

    /// <summary>
    /// Executes newCommandQueue.
    /// </summary>
    public MTLCommandQueue newCommandQueue() {
        return objc_msgSend<MTLCommandQueue>(this.NativePtr, sel_newCommandQueue);
    }

    /// <summary>
    /// Executes newBuffer.
    /// </summary>
    public MTLBuffer newBuffer(void* pointer, UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithBytes, pointer, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes newBufferWithLengthOptions.
    /// </summary>
    public MTLBuffer newBufferWithLengthOptions(UIntPtr length, MTLResourceOptions options) {
        IntPtr buffer = IntPtr_objc_msgSend(this.NativePtr, sel_newBufferWithLength, length, options);
        return new MTLBuffer(buffer);
    }

    /// <summary>
    /// Executes newTextureWithDescriptor.
    /// </summary>
    public MTLTexture newTextureWithDescriptor(MTLTextureDescriptor descriptor) {
        return objc_msgSend<MTLTexture>(this.NativePtr, sel_newTextureWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes newSamplerStateWithDescriptor.
    /// </summary>
    public MTLSamplerState newSamplerStateWithDescriptor(MTLSamplerDescriptor descriptor) {
        return objc_msgSend<MTLSamplerState>(this.NativePtr, sel_newSamplerStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes newDepthStencilStateWithDescriptor.
    /// </summary>
    public MTLDepthStencilState newDepthStencilStateWithDescriptor(MTLDepthStencilDescriptor descriptor) {
        return objc_msgSend<MTLDepthStencilState>(this.NativePtr, sel_newDepthStencilStateWithDescriptor, descriptor.NativePtr);
    }

    /// <summary>
    /// Executes supportsTextureSampleCount.
    /// </summary>
    public Bool8 supportsTextureSampleCount(UIntPtr sampleCount) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsTextureSampleCount, sampleCount);
    }

    /// <summary>
    /// Executes supportsFeatureSet.
    /// </summary>
    public Bool8 supportsFeatureSet(MTLFeatureSet featureSet) {
        return bool8_objc_msgSend(this.NativePtr, sel_supportsFeatureSet, (uint)featureSet);
    }

    public Bool8 isDepth24Stencil8PixelFormatSupported
        => bool8_objc_msgSend(this.NativePtr, sel_isDepth24Stencil8PixelFormatSupported);

    [DllImport(MetalFramework)]

    /// <summary>
    /// Executes MTLCreateSystemDefaultDevice.
    /// </summary>
    public static extern MTLDevice MTLCreateSystemDefaultDevice();

    [DllImport(MetalFramework)]

    /// <summary>
    /// Executes MTLCopyAllDevices.
    /// </summary>
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