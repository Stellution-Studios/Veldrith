using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Veldrith.D3D12;

/// <summary>
/// Shares mipmap shaders, pipelines, and filtering bindings across command lists belonging to one device.
/// </summary>
internal sealed class D3D12MipmapPipelineCache : IDisposable {
    
    /// <summary>
    /// Stores the graphics device that owns the cached resources.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Serializes initialization so concurrent texture loads compile each shader variant only once.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Caches pipelines by their shader source and destination binding kind.
    /// </summary>
    private readonly Dictionary<(string, bool), Entry> _entries = new();

    /// <summary>
    /// Stores the binding layout for shaders that write encoded pixels into a raw buffer.
    /// </summary>
    private ResourceLayout _bufferLayout;

    /// <summary>
    /// Stores the binding layout for shaders that write directly into a texture UAV.
    /// </summary>
    private ResourceLayout _textureLayout;

    /// <summary>
    /// Stores the linear clamp sampler shared by all mipmap shader variants.
    /// </summary>
    private Sampler _sampler;

    /// <summary>
    /// Initializes a cache whose resources live until the owning graphics device is disposed.
    /// </summary>
    /// <param name="gd">The device used to create mipmap pipelines and bindings.</param>
    internal D3D12MipmapPipelineCache(D3D12GraphicsDevice gd) {
        this._gd = gd;
    }

    /// <summary>
    /// Gets a mipmap pipeline, compiling its shader only on the first request for this device.
    /// </summary>
    /// <param name="sourceCode">The complete HLSL source of the shader variant.</param>
    /// <param name="bufferDestination">Whether the shader writes a raw buffer instead of a texture UAV.</param>
    /// <param name="compileShader">The compiler invoked only when the variant is not cached.</param>
    /// <returns>The device-owned pipeline and its shared binding resources.</returns>
    [SupportedOSPlatform("windows")]
    internal Entry GetOrCreate(string sourceCode, bool bufferDestination, Func<string, byte[]> compileShader) {
        lock (this._lock) {
            if (this._entries.TryGetValue((sourceCode, bufferDestination), out Entry entry)) {
                return entry;
            }

            ResourceLayout layout = bufferDestination ? this._bufferLayout : this._textureLayout;
            if (layout == null) {
                ResourceKind destinationKind = bufferDestination ? ResourceKind.StructuredBufferReadWrite : ResourceKind.TextureReadWrite;
                layout = this._gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("Destination", destinationKind, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Compute)
                ));

                if (bufferDestination) {
                    this._bufferLayout = layout;
                }
                else {
                    this._textureLayout = layout;
                }
            }

            if (this._sampler == null) {
                SamplerDescription description = SamplerDescription.LINEAR;
                description.AddressModeU = SamplerAddressMode.Clamp;
                description.AddressModeV = SamplerAddressMode.Clamp;
                description.AddressModeW = SamplerAddressMode.Clamp;
                this._sampler = this._gd.ResourceFactory.CreateSampler(description);
            }

            // Keep compilation inside the lock. A new command list for every loaded texture
            // must reuse the first compilation rather than starting another compiler invocation.
            byte[] code = compileShader(sourceCode);
            using Shader shader = this._gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, code, "cs_main"));
            D3D12Pipeline pipeline = (D3D12Pipeline)this._gd.ResourceFactory.CreateComputePipeline(
                new ComputePipelineDescription(shader, [layout], 8, 8, 1)
            );

            entry = new Entry(pipeline, layout, this._sampler);
            this._entries.Add((sourceCode, bufferDestination), entry);
            return entry;
        }
    }

    /// <summary>
    /// Releases shared mipmap resources after the graphics device has finished its queued work.
    /// </summary>
    public void Dispose() {
        lock (this._lock) {
            foreach (Entry entry in this._entries.Values) {
                entry.Pipeline.Dispose();
            }

            this._entries.Clear();
            this._bufferLayout?.Dispose();
            this._textureLayout?.Dispose();
            this._sampler?.Dispose();
        }
    }

    /// <summary>
    /// Provides shared, device-owned resources for one mipmap shader variant.
    /// </summary>
    internal sealed class Entry {
        /// <summary>
        /// Stores the compiled compute pipeline for the shader variant.
        /// </summary>
        internal readonly D3D12Pipeline Pipeline;

        /// <summary>
        /// Stores the source texture, destination, and sampler binding layout.
        /// </summary>
        internal readonly ResourceLayout Layout;

        /// <summary>
        /// Stores the linear clamp sampler shared across all mipmap variants.
        /// </summary>
        internal readonly Sampler Sampler;

        /// <summary>
        /// Initializes references to resources owned by the device cache.
        /// </summary>
        /// <param name="pipeline">The compiled mipmap compute pipeline.</param>
        /// <param name="layout">The resource layout used by the pipeline.</param>
        /// <param name="sampler">The shared filtering sampler.</param>
        internal Entry(D3D12Pipeline pipeline, ResourceLayout layout, Sampler sampler) {
            this.Pipeline = pipeline;
            this.Layout = layout;
            this.Sampler = sampler;
        }
    }
}