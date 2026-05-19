using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using SpvcBackend = Silk.NET.SPIRV.Cross.Backend;
using SpvcCompiler = Silk.NET.SPIRV.Cross.Compiler;
using SpvcContext = Silk.NET.SPIRV.Cross.Context;
using SpvcResources = Silk.NET.SPIRV.Cross.Resources;
using SpvcResult = Silk.NET.SPIRV.Cross.Result;

namespace Veldrith.SPIRV;

/// <summary>
/// Provides SPIR-V compilation support for SpirvCrossCompiler.
/// </summary>
internal static unsafe class SpirvCrossCompiler {

    /// <summary>
    /// Gets the api value.
    /// </summary>
    private static readonly Cross s_cross = Cross.GetApi();

    /// <summary>
    /// Executes the compile vertex fragment logic for this backend.
    /// </summary>
    /// <param name="vsSpirv">The vs spirv value used by this operation.</param>
    /// <param name="fsSpirv">The fs spirv value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VertexFragmentCompilationResult CompileVertexFragment(byte[] vsSpirv, byte[] fsSpirv, CrossCompileTarget target, CrossCompileOptions options) {
        Cross cross = s_cross;
        SpvcContext* ctx = null;
        try {
            Check(cross, null, cross.ContextCreate(&ctx));

            ParsedIr* vsIr = null;
            ParsedIr* fsIr = null;
            fixed (byte* vsPtr = vsSpirv)
            fixed (byte* fsPtr = fsSpirv) {
                Check(cross, ctx, cross.ContextParseSpirv(ctx, (uint*)vsPtr, (nuint)(vsSpirv.Length / 4), &vsIr));
                Check(cross, ctx, cross.ContextParseSpirv(ctx, (uint*)fsPtr, (nuint)(fsSpirv.Length / 4), &fsIr));
            }

            SpvcBackend backend = GetSpvcBackend(target);
            SpvcCompiler* vsCompiler = null;
            SpvcCompiler* fsCompiler = null;
            Check(cross, ctx, cross.ContextCreateCompiler(ctx, backend, vsIr, CaptureMode.TakeOwnership, &vsCompiler));
            Check(cross, ctx, cross.ContextCreateCompiler(ctx, backend, fsIr, CaptureMode.TakeOwnership, &fsCompiler));

            // Collect resources from both shaders
            SortedDictionary<BindingKey, ResourceInfo> allResources = new();
            bool hasVsStorage = CollectResources(cross, vsCompiler, allResources, 0, options.NormalizeResourceNames);
            bool hasFsStorage = CollectResources(cross, fsCompiler, allResources, 1, options.NormalizeResourceNames);
            uint vsPushConstantId = GetPushConstantId(cross, vsCompiler);
            uint fsPushConstantId = GetPushConstantId(cross, fsCompiler);
            bool hasPushConstants = vsPushConstantId != 0 || fsPushConstantId != 0;

            // Set compiler options (GLSL version depends on whether storage resources are present)
            bool hasStorageResources = hasVsStorage || hasFsStorage;
            SetCompilerOptions(cross, vsCompiler, target, options, false, hasStorageResources);
            SetCompilerOptions(cross, fsCompiler, target, options, false, hasStorageResources);

            SetSpecializations(cross, vsCompiler, options);
            SetSpecializations(cross, fsCompiler, options);

            if (target == CrossCompileTarget.HLSL || target == CrossCompileTarget.MSL) {
                RemapBindingsHlslMsl(cross, allResources, vsCompiler, fsCompiler, target, hasPushConstants, vsPushConstantId, fsPushConstantId);
            }

            if (target == CrossCompileTarget.GLSL) {
                BuildCombinedImageSamplers(cross, vsCompiler);
                BuildCombinedImageSamplers(cross, fsCompiler);
                RenameStageIO(cross, vsCompiler, fsCompiler);
            }

            byte* vsSource = null;
            byte* fsSource = null;
            Check(cross, ctx, cross.CompilerCompile(vsCompiler, &vsSource));
            Check(cross, ctx, cross.CompilerCompile(fsCompiler, &fsSource));
            string vsText = Marshal.PtrToStringUTF8((nint)vsSource);
            string fsText = Marshal.PtrToStringUTF8((nint)fsSource);

            VertexElementDescription[] vertexElements = ReflectVertexInputs(cross, vsCompiler);
            ResourceLayoutDescription[] layouts = BuildResourceLayouts(allResources, false);

            SpirvReflection reflection = new(vertexElements, layouts);
            return new VertexFragmentCompilationResult(vsText, fsText, reflection);
        }
        catch (Exception ex) when (ex is not SpirvCompilationException) {
            throw new SpirvCompilationException("Cross-compilation failed: " + ex.Message, ex);
        }
        finally {
            if (ctx != null) {
                cross.ContextDestroy(ctx);
            }
        }
    }

    /// <summary>
    /// Executes the compile compute logic for this backend.
    /// </summary>
    /// <param name="csSpirv">The cs spirv value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static ComputeCompilationResult CompileCompute(byte[] csSpirv, CrossCompileTarget target, CrossCompileOptions options) {
        Cross cross = s_cross;
        SpvcContext* ctx = null;
        try {
            Check(cross, null, cross.ContextCreate(&ctx));

            ParsedIr* csIr = null;
            fixed (byte* csPtr = csSpirv) {
                Check(cross, ctx, cross.ContextParseSpirv(ctx, (uint*)csPtr, (nuint)(csSpirv.Length / 4), &csIr));
            }

            SpvcBackend backend = GetSpvcBackend(target);
            SpvcCompiler* csCompiler = null;
            Check(cross, ctx, cross.ContextCreateCompiler(ctx, backend, csIr, CaptureMode.TakeOwnership, &csCompiler));

            SortedDictionary<BindingKey, ResourceInfo> allResources = new();
            bool hasStorage = CollectResources(cross, csCompiler, allResources, 0, options.NormalizeResourceNames);
            uint csPushConstantId = GetPushConstantId(cross, csCompiler);
            bool hasPushConstants = csPushConstantId != 0;

            SetCompilerOptions(cross, csCompiler, target, options, true, hasStorage);
            SetSpecializations(cross, csCompiler, options);

            if (target == CrossCompileTarget.HLSL || target == CrossCompileTarget.MSL) {
                RemapBindingsHlslMsl(cross, allResources, csCompiler, null, target, hasPushConstants, csPushConstantId, 0);
            }

            if (target == CrossCompileTarget.GLSL) {
                BuildCombinedImageSamplers(cross, csCompiler);
            }

            byte* csSource = null;
            Check(cross, ctx, cross.CompilerCompile(csCompiler, &csSource));
            string csText = Marshal.PtrToStringUTF8((nint)csSource);

            ResourceLayoutDescription[] layouts = BuildResourceLayouts(allResources, true);
            SpirvReflection reflection = new(Array.Empty<VertexElementDescription>(), layouts);
            return new ComputeCompilationResult(csText, reflection);
        }
        catch (Exception ex) when (ex is not SpirvCompilationException) {
            throw new SpirvCompilationException("Cross-compilation failed: " + ex.Message, ex);
        }
        finally {
            if (ctx != null) {
                cross.ContextDestroy(ctx);
            }
        }
    }

    #region Types

    /// <summary>
    /// Executes BindingKey.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="binding">The binding value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private readonly struct BindingKey(uint set, uint binding) : IComparable<BindingKey> {

        /// <summary>
        /// Stores the set state used by this instance.
        /// </summary>
        public readonly uint Set = set;

        /// <summary>
        /// Stores the binding state used by this instance.
        /// </summary>
        public readonly uint Binding = binding;

        /// <summary>
        /// Executes the compare to logic for this backend.
        /// </summary>
        /// <param name="other">The value to compare against.</param>
        /// <returns>The value produced by this operation.</returns>
        public int CompareTo(BindingKey other) {
            int c = this.Set.CompareTo(other.Set);
            return c != 0 ? c : this.Binding.CompareTo(other.Binding);
        }
    }

    /// <summary>
    /// Represents the ResourceInfo type used by the graphics runtime.
    /// </summary>
    private class ResourceInfo {

        /// <summary>
        /// Stores the ids state used by this instance.
        /// </summary>
        public readonly uint[] IDs = new uint[2]; // 0 = VS/CS, 1 = FS

        /// <summary>
        /// Stores the kind state used by this instance.
        /// </summary>
        public ResourceKind Kind;

        /// <summary>
        /// Stores the human-readable name associated with this instance.
        /// </summary>
        public string Name;
    }

    #endregion

    #region Compiler Setup

    /// <summary>
    /// Executes the check logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="ctx">The ctx value used by this operation.</param>
    /// <param name="result">The result value used by this operation.</param>
    private static void Check(Cross cross, SpvcContext* ctx, SpvcResult result) {
        if (result != SpvcResult.Success) {
            string msg = "SPIRV-Cross error";
            if (ctx != null) {
                byte* errorPtr = cross.ContextGetLastErrorString(ctx);
                if (errorPtr != null) {
                    msg = Marshal.PtrToStringUTF8((nint)errorPtr) ?? msg;
                }
            }

            throw new SpirvCompilationException(msg);
        }
    }

    /// <summary>
    /// Gets the spvc backend value.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static SpvcBackend GetSpvcBackend(CrossCompileTarget target) {
        return target switch {
            CrossCompileTarget.HLSL => SpvcBackend.Hlsl,
            CrossCompileTarget.GLSL => SpvcBackend.Glsl,
            CrossCompileTarget.MSL => SpvcBackend.Msl,
            _ => throw new SpirvCompilationException($"Invalid CrossCompileTarget: {target}")
        };
    }

    /// <summary>
    /// Sets the compiler options value.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="isCompute">The is compute value used by this operation.</param>
    /// <param name="hasStorageResources">The resource involved in this operation.</param>
    private static void SetCompilerOptions(Cross cross, SpvcCompiler* compiler, CrossCompileTarget target, CrossCompileOptions options, bool isCompute, bool hasStorageResources) {
        CompilerOptions* opts = null;
        Check(cross, null, cross.CompilerCreateCompilerOptions(compiler, &opts));

        if (options.FixClipSpaceZ) {
            cross.CompilerOptionsSetBool(opts, CompilerOption.FixupDepthConvention, 1);
        }

        if (options.InvertVertexOutputY) {
            cross.CompilerOptionsSetBool(opts, CompilerOption.FlipVertexY, 1);
        }

        switch (target) {
            case CrossCompileTarget.HLSL:
                cross.CompilerOptionsSetUint(opts, CompilerOption.HlslShaderModel, 50);
                cross.CompilerOptionsSetBool(opts, CompilerOption.HlslPointSizeCompat, 1);
                break;

            case CrossCompileTarget.GLSL: {
                    uint version = isCompute || hasStorageResources ? 430u : 330u;
                    cross.CompilerOptionsSetUint(opts, CompilerOption.GlslVersion, version);
                    cross.CompilerOptionsSetBool(opts, CompilerOption.GlslES, 0);
                    cross.CompilerOptionsSetBool(opts, CompilerOption.GlslEnable420PackExtension, 0);
                    break;
                }

            case CrossCompileTarget.MSL:
                cross.CompilerOptionsSetBool(opts, CompilerOption.MslEnableDecorationBinding, 1);
                break;
        }

        Check(cross, null, cross.CompilerInstallCompilerOptions(compiler, opts));
    }

    /// <summary>
    /// Sets the specializations value.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    private static void SetSpecializations(Cross cross, SpvcCompiler* compiler, CrossCompileOptions options) {
        if (options.Specializations.Length == 0) {
            return;
        }

        Silk.NET.SPIRV.Cross.SpecializationConstant* constants = null;
        nuint count = 0;
        cross.CompilerGetSpecializationConstants(compiler, &constants, &count);

        for (int i = 0; i < options.Specializations.Length; i++) {
            uint constID = options.Specializations[i].ID;

            uint varID = 0;
            for (nuint j = 0; j < count; j++) {
                if (constants[j].ConstantId == constID) {
                    varID = constants[j].Id;
                    break;
                }
            }

            if (varID != 0) {
                Constant* constant = cross.CompilerGetConstantHandle(compiler, varID);
                // Write the raw u64 value, matching upstream's direct `constVar.m.c[0].r[0].u64 = value`
                cross.ConstantSetScalarU64(constant, 0, 0, options.Specializations[i].Data);
            }
        }
    }

    #endregion

    #region Resource Collection

    /// <summary>
    /// Executes the collect resources logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="idIndex">The id index value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool CollectResources(Cross cross, SpvcCompiler* compiler, SortedDictionary<BindingKey, ResourceInfo> allResources, uint idIndex, bool normalizeResourceNames) {
        SpvcResources* resources = null;
        Check(cross, null, cross.CompilerCreateShaderResources(compiler, &resources));

        bool hasStorage = false;

        AddResourcesOfType(cross, compiler, resources, ResourceType.UniformBuffer, allResources, idIndex, normalizeResourceNames, ResourceKind.UniformBuffer);

        hasStorage |= AddStorageBuffers(cross, compiler, resources, allResources, idIndex, normalizeResourceNames);

        AddResourcesOfType(cross, compiler, resources, ResourceType.SeparateImage, allResources, idIndex, normalizeResourceNames, ResourceKind.TextureReadOnly);

        hasStorage |= AddResourcesOfType(cross, compiler, resources, ResourceType.StorageImage, allResources, idIndex, normalizeResourceNames, ResourceKind.TextureReadWrite);

        AddResourcesOfType(cross, compiler, resources, ResourceType.SeparateSamplers, allResources, idIndex, normalizeResourceNames, ResourceKind.Sampler);

        return hasStorage;
    }

    /// <summary>
    /// Executes the add resources of type logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="resources">The resource involved in this operation.</param>
    /// <param name="resourceType">The resource type value used by this operation.</param>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="idIndex">The id index value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool AddResourcesOfType(Cross cross, SpvcCompiler* compiler, SpvcResources* resources, ResourceType resourceType, SortedDictionary<BindingKey, ResourceInfo> allResources, uint idIndex, bool normalizeResourceNames, ResourceKind kind) {
        ReflectedResource* resourceList = null;
        nuint resourceCount = 0;
        cross.ResourcesGetResourceListForType(resources, resourceType, &resourceList, &resourceCount);

        bool any = false;
        for (nuint i = 0; i < resourceCount; i++) {
            any = true;
            ref ReflectedResource resource = ref resourceList[i];
            uint set = cross.CompilerGetDecoration(compiler, resource.Id, Decoration.DescriptorSet);
            uint binding = cross.CompilerGetDecoration(compiler, resource.Id, Decoration.Binding);

            string name = GetOrSetResourceName(cross, compiler, ref resource, kind, set, binding, normalizeResourceNames);

            InsertResource(allResources, set, binding, resource.Id, idIndex, name, kind);
        }

        return any;
    }

    /// <summary>
    /// Executes the add storage buffers logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="resources">The resource involved in this operation.</param>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="idIndex">The id index value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool AddStorageBuffers(Cross cross, SpvcCompiler* compiler, SpvcResources* resources, SortedDictionary<BindingKey, ResourceInfo> allResources, uint idIndex, bool normalizeResourceNames) {
        ReflectedResource* resourceList = null;
        nuint resourceCount = 0;
        cross.ResourcesGetResourceListForType(resources, ResourceType.StorageBuffer, &resourceList, &resourceCount);

        bool any = false;
        for (nuint i = 0; i < resourceCount; i++) {
            any = true;
            ref ReflectedResource resource = ref resourceList[i];

            // Uses get_buffer_block_decorations (matching upstream's get_buffer_block_flags) which checks
            // both variable-level and member-level decorations, not just the variable itself.
            bool isNonWritable = HasBufferBlockDecoration(cross, compiler, resource.Id, Decoration.NonWritable);
            ResourceKind kind = isNonWritable ? ResourceKind.StructuredBufferReadOnly : ResourceKind.StructuredBufferReadWrite;

            uint set = cross.CompilerGetDecoration(compiler, resource.Id, Decoration.DescriptorSet);
            uint binding = cross.CompilerGetDecoration(compiler, resource.Id, Decoration.Binding);

            string name;
            if (normalizeResourceNames) {
                name = $"vdspv_{set}_{binding}";
                SetNativeName(cross, compiler, resource.Id, name);
            }
            else {
                name = GetNativeName(cross, compiler, resource.Id, resource.BaseTypeId);
            }

            InsertResource(allResources, set, binding, resource.Id, idIndex, name, kind);
        }

        return any;
    }

    /// <summary>
    /// Gets the or set resource name value.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="binding">The binding value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetOrSetResourceName(Cross cross, SpvcCompiler* compiler, ref ReflectedResource resource, ResourceKind kind, uint set, uint binding, bool normalizeResourceNames) {
        if (normalizeResourceNames) {
            string name = $"vdspv_{set}_{binding}";
            uint nameTarget = kind == ResourceKind.UniformBuffer ? resource.BaseTypeId : resource.Id;
            SetNativeName(cross, compiler, nameTarget, name);
            return name;
        }

        return GetNativeName(cross, compiler, resource.Id, resource.BaseTypeId);
    }

    /// <summary>
    /// Executes the insert resource logic for this backend.
    /// </summary>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="binding">The binding value used by this operation.</param>
    /// <param name="resourceId">The resource id value used by this operation.</param>
    /// <param name="idIndex">The id index value used by this operation.</param>
    /// <param name="name">The name used by this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    private static void InsertResource(SortedDictionary<BindingKey, ResourceInfo> allResources, uint set, uint binding, uint resourceId, uint idIndex, string name, ResourceKind kind) {
        BindingKey key = new(set, binding);
        if (allResources.TryGetValue(key, out ResourceInfo? existing)) {
            if (existing.IDs[idIndex] != 0) {
                throw new SpirvCompilationException($"The same binding slot ({set}, {binding}) was used by multiple distinct resources. " + $"First resource: {existing.Name}. Second resource: {name}");
            }

            if (existing.Kind != kind) {
                throw new SpirvCompilationException($"The same binding slot ({set}, {binding}) was used by multiple resources with " + $"incompatible types: \"{existing.Kind}\" and \"{kind}\".");
            }

            existing.IDs[idIndex] = resourceId;
        }
        else {
            ResourceInfo info = new() { Name = name, Kind = kind };
            info.IDs[idIndex] = resourceId;
            allResources[key] = info;
        }
    }

    #endregion

    #region Binding Remapping

    /// <summary>
    /// Gets the resource index value.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <param name="bufferIndex">The buffer index value used by this operation.</param>
    /// <param name="textureIndex">The texture index value used by this operation.</param>
    /// <param name="uavIndex">The uav index value used by this operation.</param>
    /// <param name="samplerIndex">The sampler index value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static uint GetResourceIndex(CrossCompileTarget target, ResourceKind kind, ref uint bufferIndex, ref uint textureIndex, ref uint uavIndex, ref uint samplerIndex) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return bufferIndex++;
            case ResourceKind.StructuredBufferReadWrite: return target == CrossCompileTarget.MSL ? bufferIndex++ : uavIndex++;
            case ResourceKind.TextureReadWrite: return target == CrossCompileTarget.MSL ? textureIndex++ : uavIndex++;
            case ResourceKind.TextureReadOnly: return textureIndex++;
            case ResourceKind.StructuredBufferReadOnly: return target == CrossCompileTarget.MSL ? bufferIndex++ : textureIndex++;
            case ResourceKind.Sampler: return samplerIndex++;
            default: throw new SpirvCompilationException($"Invalid ResourceKind: {kind}");
        }
    }

    /// <summary>
    /// Executes the remap bindings hlsl msl logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="compiler0">The compiler0 value used by this operation.</param>
    /// <param name="compiler1">The compiler1 value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    private static void RemapBindingsHlslMsl(Cross cross, SortedDictionary<BindingKey, ResourceInfo> allResources, SpvcCompiler* compiler0, SpvcCompiler* compiler1, CrossCompileTarget target, bool hasPushConstants, uint pushConstantId0, uint pushConstantId1) {
        // D3D12 root signatures reserve b0 for root constants (push constants),
        // so SPIR-V resources targeting HLSL must always start CBV-style bindings at b1.
        uint bufferIndex = target == CrossCompileTarget.HLSL ? 1u : 0u;
        uint textureIndex = 0;
        uint uavIndex = 0;
        uint samplerIndex = 0;

        foreach (KeyValuePair<BindingKey, ResourceInfo> kvp in allResources) {
            uint index = GetResourceIndex(target, kvp.Value.Kind, ref bufferIndex, ref textureIndex, ref uavIndex, ref samplerIndex);

            uint id0 = kvp.Value.IDs[0];
            if (id0 != 0) {
                cross.CompilerSetDecoration(compiler0, id0, Decoration.Binding, index);
            }

            if (compiler1 != null) {
                uint id1 = kvp.Value.IDs[1];
                if (id1 != 0) {
                    cross.CompilerSetDecoration(compiler1, id1, Decoration.Binding, index);
                }
            }
        }

        if (target == CrossCompileTarget.MSL && hasPushConstants) {
            if (pushConstantId0 != 0) {
                cross.CompilerSetDecoration(compiler0, pushConstantId0, Decoration.Binding, bufferIndex);
            }

            if (compiler1 != null && pushConstantId1 != 0) {
                cross.CompilerSetDecoration(compiler1, pushConstantId1, Decoration.Binding, bufferIndex);
            }
        }
    }

    #endregion

    #region GLSL Specific

    /// <summary>
    /// Executes the build combined image samplers logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    private static void BuildCombinedImageSamplers(Cross cross, SpvcCompiler* compiler) {
        uint dummySamplerId = 0;
        Check(cross, null, cross.CompilerBuildDummySamplerForCombinedImages(compiler, &dummySamplerId));
        Check(cross, null, cross.CompilerBuildCombinedImageSamplers(compiler));

        CombinedImageSampler* combinedSamplers = null;
        nuint count = 0;
        cross.CompilerGetCombinedImageSamplers(compiler, &combinedSamplers, &count);

        for (nuint i = 0; i < count; i++) {
            byte* imageName = cross.CompilerGetName(compiler, combinedSamplers[i].ImageId);
            if (imageName != null) {
                cross.CompilerSetName(compiler, combinedSamplers[i].CombinedId, imageName);
            }
        }
    }

    /// <summary>
    /// Executes the rename stage io logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="vsCompiler">The vs compiler value used by this operation.</param>
    /// <param name="fsCompiler">The fs compiler value used by this operation.</param>
    private static void RenameStageIO(Cross cross, SpvcCompiler* vsCompiler, SpvcCompiler* fsCompiler) {
        // Rename vertex outputs to vdspv_fsinN
        SpvcResources* vsResources = null;
        Check(cross, null, cross.CompilerCreateShaderResources(vsCompiler, &vsResources));
        ReflectedResource* vsOutputs = null;
        nuint vsOutputCount = 0;
        cross.ResourcesGetResourceListForType(vsResources, ResourceType.StageOutput, &vsOutputs, &vsOutputCount);

        for (nuint i = 0; i < vsOutputCount; i++) {
            uint location = cross.CompilerGetDecoration(vsCompiler, vsOutputs[i].Id, Decoration.Location);
            SetNativeName(cross, vsCompiler, vsOutputs[i].Id, $"vdspv_fsin{location}");
        }

        // Rename fragment inputs to vdspv_fsinN
        SpvcResources* fsResources = null;
        Check(cross, null, cross.CompilerCreateShaderResources(fsCompiler, &fsResources));
        ReflectedResource* fsInputs = null;
        nuint fsInputCount = 0;
        cross.ResourcesGetResourceListForType(fsResources, ResourceType.StageInput, &fsInputs, &fsInputCount);

        for (nuint i = 0; i < fsInputCount; i++) {
            uint location = cross.CompilerGetDecoration(fsCompiler, fsInputs[i].Id, Decoration.Location);
            SetNativeName(cross, fsCompiler, fsInputs[i].Id, $"vdspv_fsin{location}");
        }
    }

    #endregion

    #region Reflection

    /// <summary>
    /// Executes the reflect vertex inputs logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VertexElementDescription[] ReflectVertexInputs(Cross cross, SpvcCompiler* compiler) {
        SpvcResources* resources = null;
        Check(cross, null, cross.CompilerCreateShaderResources(compiler, &resources));
        ReflectedResource* inputs = null;
        nuint inputCount = 0;
        cross.ResourcesGetResourceListForType(resources, ResourceType.StageInput, &inputs, &inputCount);

        uint elementCount = 0;
        for (nuint i = 0; i < inputCount; i++) {
            uint location = cross.CompilerGetDecoration(compiler, inputs[i].Id, Decoration.Location);
            elementCount = Math.Max(elementCount, location + 1);
        }

        VertexElementDescription[] elements = new VertexElementDescription[elementCount];
        for (nuint i = 0; i < inputCount; i++) {
            uint location = cross.CompilerGetDecoration(compiler, inputs[i].Id, Decoration.Location);

            byte* namePtr = cross.CompilerGetName(compiler, inputs[i].Id);
            string name = namePtr != null ? Marshal.PtrToStringUTF8((nint)namePtr) ?? "" : "";
            if (string.IsNullOrEmpty(name)) {
                name = $"_input{location}";
            }

            CrossType* type = cross.CompilerGetTypeHandle(compiler, inputs[i].BaseTypeId);
            Basetype baseType = cross.TypeGetBasetype(type);
            uint vecSize = cross.TypeGetVectorSize(type);

            VertexElementFormat format = baseType switch {
                Basetype.FP32 => vecSize switch {
                    1 => VertexElementFormat.Float1,
                    2 => VertexElementFormat.Float2,
                    3 => VertexElementFormat.Float3,
                    4 => VertexElementFormat.Float4,
                    _ => VertexElementFormat.Float1
                },
                Basetype.Int32 => vecSize switch {
                    1 => VertexElementFormat.Int1,
                    2 => VertexElementFormat.Int2,
                    3 => VertexElementFormat.Int3,
                    4 => VertexElementFormat.Int4,
                    _ => VertexElementFormat.Int1
                },
                Basetype.Uint32 => vecSize switch {
                    1 => VertexElementFormat.UInt1,
                    2 => VertexElementFormat.UInt2,
                    3 => VertexElementFormat.UInt3,
                    4 => VertexElementFormat.UInt4,
                    _ => VertexElementFormat.UInt1
                },
                _ => throw new SpirvCompilationException($"Unhandled SPIR-V vertex input data type: {baseType}")
            };

            elements[location] = new VertexElementDescription(name, VertexElementSemantic.TextureCoordinate, format);
        }

        return elements;
    }

    /// <summary>
    /// Executes the build resource layouts logic for this backend.
    /// </summary>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="isCompute">The is compute value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceLayoutDescription[] BuildResourceLayouts(SortedDictionary<BindingKey, ResourceInfo> allResources, bool isCompute) {
        uint setCount = 0;
        Dictionary<uint, uint> setSizes = new();

        foreach (KeyValuePair<BindingKey, ResourceInfo> kvp in allResources) {
            uint set = kvp.Key.Set;
            if (set + 1 > setCount) {
                setCount = set + 1;
            }

            uint needed = kvp.Key.Binding + 1;
            if (!setSizes.TryGetValue(set, out uint current) || needed > current) {
                setSizes[set] = needed;
            }
        }

        if (setCount == 0 && allResources.Count == 0) {
            setCount = 1;
            setSizes[0] = 0;
        }

        ResourceLayoutDescription[] layouts = new ResourceLayoutDescription[setCount];
        for (uint i = 0; i < setCount; i++) {
            uint size = setSizes.TryGetValue(i, out uint s) ? s : 0;
            ResourceLayoutElementDescription[] elements = new ResourceLayoutElementDescription[size];
            for (uint j = 0; j < size; j++) {
                elements[j] = new ResourceLayoutElementDescription(null, ResourceKind.UniformBuffer, ShaderStages.None, (ResourceLayoutElementOptions)2); // "Unused" marker
            }

            layouts[i].Elements = elements;
        }

        foreach (KeyValuePair<BindingKey, ResourceInfo> kvp in allResources) {
            ShaderStages stages = ShaderStages.None;
            if (kvp.Value.IDs[0] != 0) {
                stages |= isCompute ? ShaderStages.Compute : ShaderStages.Vertex;
            }

            if (kvp.Value.IDs[1] != 0) {
                stages |= ShaderStages.Fragment;
            }

            layouts[kvp.Key.Set].Elements[kvp.Key.Binding] = new ResourceLayoutElementDescription(kvp.Value.Name, kvp.Value.Kind, stages);
        }

        return layouts;
    }

    #endregion

    #region Native String Helpers

    /// <summary>
    /// Gets the native name value.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="fallbackId">The fallback id value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetNativeName(Cross cross, SpvcCompiler* compiler, uint id, uint fallbackId) {
        byte* namePtr = cross.CompilerGetName(compiler, id);
        string name = namePtr != null ? Marshal.PtrToStringUTF8((nint)namePtr) ?? "" : "";
        if (string.IsNullOrEmpty(name)) {
            namePtr = cross.CompilerGetName(compiler, fallbackId);
            name = namePtr != null ? Marshal.PtrToStringUTF8((nint)namePtr) ?? "" : "";
        }

        return name;
    }

    /// <summary>
    /// Sets the native name value.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="name">The name used by this operation.</param>
    private static void SetNativeName(Cross cross, SpvcCompiler* compiler, uint id, string name) {
        byte[] nameBytes = Encoding.UTF8.GetBytes(name + '\0');
        fixed (byte* namePtr = nameBytes) {
            cross.CompilerSetName(compiler, id, namePtr);
        }
    }

    /// <summary>
    /// Executes the has buffer block decoration logic for this backend.
    /// </summary>
    /// <param name="cross">The cross value used by this operation.</param>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="decoration">The decoration value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool HasBufferBlockDecoration(Cross cross, SpvcCompiler* compiler, uint id, Decoration decoration) {
        Decoration* decorations = null;
        nuint count = 0;
        cross.CompilerGetBufferBlockDecorations(compiler, id, &decorations, &count);
        for (nuint i = 0; i < count; i++) {
            if (decorations[i] == decoration) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the push-constant resource identifier for a compiler, if present.
    /// </summary>
    /// <param name="cross">The SPIRV-Cross API instance.</param>
    /// <param name="compiler">The compiler handle.</param>
    /// <returns>The reflected push-constant resource id, or <c>0</c> when none exists.</returns>
    private static uint GetPushConstantId(Cross cross, SpvcCompiler* compiler) {
        SpvcResources* resources = null;
        Check(cross, null, cross.CompilerCreateShaderResources(compiler, &resources));

        ReflectedResource* pushConstants = null;
        nuint pushConstantCount = 0;
        cross.ResourcesGetResourceListForType(resources, ResourceType.PushConstant, &pushConstants, &pushConstantCount);
        if (pushConstantCount == 0) {
            return 0;
        }

        if (pushConstantCount > 1) {
            throw new SpirvCompilationException("Multiple push-constant blocks are not supported.");
        }

        return pushConstants[0].Id;
    }

    #endregion
}
