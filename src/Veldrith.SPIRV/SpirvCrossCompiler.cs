using System.Runtime.InteropServices;
using System.Text;
using Vortice.SPIRV;
using Vortice.SpirvCross;
using SpvcBackend = Vortice.SpirvCross.Backend;
using SpvcCompiler = Vortice.SpirvCross.spvc_compiler;
using SpvcContext = Vortice.SpirvCross.spvc_context;
using SpvcResources = Vortice.SpirvCross.spvc_resources;
using SpvcResult = Vortice.SpirvCross.Result;

namespace Veldrith.SPIRV;

/// <summary>
/// Provides SPIR-V compilation support for SpirvCrossCompiler.
/// </summary>
internal static unsafe class SpirvCrossCompiler {

    /// <summary>
    /// Executes the compile vertex fragment logic for this backend.
    /// </summary>
    /// <param name="vsSpirv">The vs spirv value used by this operation.</param>
    /// <param name="fsSpirv">The fs spirv value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VertexFragmentCompilationResult CompileVertexFragment(byte[] vsSpirv, byte[] fsSpirv, CrossCompileTarget target, CrossCompileOptions options) {
        SpvcContext ctx = default;
        try {
            Check(default, SpirvCrossApi.spvc_context_create(&ctx));

            spvc_parsed_ir vsIr = default;
            spvc_parsed_ir fsIr = default;
            fixed (byte* vsPtr = vsSpirv)
            fixed (byte* fsPtr = fsSpirv) {
                Check(ctx, SpirvCrossApi.spvc_context_parse_spirv(ctx, (uint*)vsPtr, (nuint)(vsSpirv.Length / 4), &vsIr));
                Check(ctx, SpirvCrossApi.spvc_context_parse_spirv(ctx, (uint*)fsPtr, (nuint)(fsSpirv.Length / 4), &fsIr));
            }

            SpvcBackend backend = GetSpvcBackend(target);
            SpvcCompiler vsCompiler = default;
            SpvcCompiler fsCompiler = default;
            Check(ctx, SpirvCrossApi.spvc_context_create_compiler(ctx, backend, vsIr, CaptureMode.TakeOwnership, &vsCompiler));
            Check(ctx, SpirvCrossApi.spvc_context_create_compiler(ctx, backend, fsIr, CaptureMode.TakeOwnership, &fsCompiler));

            // Collect resources from both shaders
            SortedDictionary<BindingKey, ResourceInfo> allResources = new();
            bool hasVsStorage = CollectResources(vsCompiler, allResources, 0, options.NormalizeResourceNames);
            bool hasFsStorage = CollectResources(fsCompiler, allResources, 1, options.NormalizeResourceNames);
            uint vsPushConstantId = GetPushConstantId(vsCompiler);
            uint fsPushConstantId = GetPushConstantId(fsCompiler);
            bool hasPushConstants = vsPushConstantId != 0 || fsPushConstantId != 0;

            // Set compiler options (GLSL version depends on whether storage resources are present)
            bool hasStorageResources = hasVsStorage || hasFsStorage;
            SetCompilerOptions(vsCompiler, target, options, false, hasStorageResources);
            SetCompilerOptions(fsCompiler, target, options, false, hasStorageResources);

            SetSpecializations(vsCompiler, options);
            SetSpecializations(fsCompiler, options);

            if (target == CrossCompileTarget.HLSL || target == CrossCompileTarget.MSL) {
                RemapBindingsHlslMsl(allResources, vsCompiler, fsCompiler, target, hasPushConstants, vsPushConstantId, fsPushConstantId);
            }

            if (target == CrossCompileTarget.GLSL) {
                BuildCombinedImageSamplers(vsCompiler);
                BuildCombinedImageSamplers(fsCompiler);
                RenameStageIO(vsCompiler, fsCompiler);
            }

            byte* vsSource = null;
            byte* fsSource = null;
            Check(ctx, SpirvCrossApi.spvc_compiler_compile(vsCompiler, &vsSource));
            Check(ctx, SpirvCrossApi.spvc_compiler_compile(fsCompiler, &fsSource));
            string vsText = Marshal.PtrToStringUTF8((nint)vsSource);
            string fsText = Marshal.PtrToStringUTF8((nint)fsSource);

            VertexElementDescription[] vertexElements = ReflectVertexInputs(vsCompiler);
            ResourceLayoutDescription[] layouts = BuildResourceLayouts(allResources, false);

            SpirvReflection reflection = new(vertexElements, layouts);
            return new VertexFragmentCompilationResult(vsText, fsText, reflection);
        }
        catch (Exception ex) when (ex is not SpirvCompilationException) {
            throw new SpirvCompilationException("Cross-compilation failed: " + ex.Message, ex);
        }
        finally {
            if (ctx.IsNotNull) {
                SpirvCrossApi.spvc_context_destroy(ctx);
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
        SpvcContext ctx = default;
        try {
            Check(default, SpirvCrossApi.spvc_context_create(&ctx));

            spvc_parsed_ir csIr = default;
            fixed (byte* csPtr = csSpirv) {
                Check(ctx, SpirvCrossApi.spvc_context_parse_spirv(ctx, (uint*)csPtr, (nuint)(csSpirv.Length / 4), &csIr));
            }

            SpvcBackend backend = GetSpvcBackend(target);
            SpvcCompiler csCompiler = default;
            Check(ctx, SpirvCrossApi.spvc_context_create_compiler(ctx, backend, csIr, CaptureMode.TakeOwnership, &csCompiler));

            SortedDictionary<BindingKey, ResourceInfo> allResources = new();
            bool hasStorage = CollectResources(csCompiler, allResources, 0, options.NormalizeResourceNames);
            uint csPushConstantId = GetPushConstantId(csCompiler);
            bool hasPushConstants = csPushConstantId != 0;

            SetCompilerOptions(csCompiler, target, options, true, hasStorage);
            SetSpecializations(csCompiler, options);

            if (target == CrossCompileTarget.HLSL || target == CrossCompileTarget.MSL) {
                RemapBindingsHlslMsl(allResources, csCompiler, null, target, hasPushConstants, csPushConstantId, 0);
            }

            if (target == CrossCompileTarget.GLSL) {
                BuildCombinedImageSamplers(csCompiler);
            }

            byte* csSource = null;
            Check(ctx, SpirvCrossApi.spvc_compiler_compile(csCompiler, &csSource));
            string csText = Marshal.PtrToStringUTF8((nint)csSource);

            ResourceLayoutDescription[] layouts = BuildResourceLayouts(allResources, true);
            SpirvReflection reflection = new(Array.Empty<VertexElementDescription>(), layouts);
            return new ComputeCompilationResult(csText, reflection);
        }
        catch (Exception ex) when (ex is not SpirvCompilationException) {
            throw new SpirvCompilationException("Cross-compilation failed: " + ex.Message, ex);
        }
        finally {
            if (ctx.IsNotNull) {
                SpirvCrossApi.spvc_context_destroy(ctx);
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
    /// <param name="ctx">The ctx value used by this operation.</param>
    /// <param name="result">The result value used by this operation.</param>
    private static void Check(SpvcContext ctx, SpvcResult result) {
        if (result != SpvcResult.Success) {
            string msg = "SPIRV-Cross error";
            if (ctx.IsNotNull) {
                string error = SpirvCrossApi.spvc_context_get_last_error_string(ctx);
                if (!string.IsNullOrEmpty(error)) {
                    msg = error;
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
            CrossCompileTarget.HLSL => SpvcBackend.HLSL,
            CrossCompileTarget.GLSL => SpvcBackend.GLSL,
            CrossCompileTarget.MSL => SpvcBackend.MSL,
            _ => throw new SpirvCompilationException($"Invalid CrossCompileTarget: {target}")
        };
    }

    /// <summary>
    /// Sets the compiler options value.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="isCompute">The is compute value used by this operation.</param>
    /// <param name="hasStorageResources">The resource involved in this operation.</param>
    private static void SetCompilerOptions(SpvcCompiler compiler, CrossCompileTarget target, CrossCompileOptions options, bool isCompute, bool hasStorageResources) {
        spvc_compiler_options opts = default;
        Check(default, SpirvCrossApi.spvc_compiler_create_compiler_options(compiler, &opts));

        if (options.FixClipSpaceZ) {
            SpirvCrossApi.spvc_compiler_options_set_bool(opts, CompilerOption.FixupDepthConvention, 1);
        }

        if (options.InvertVertexOutputY) {
            SpirvCrossApi.spvc_compiler_options_set_bool(opts, CompilerOption.FlipVertexY, 1);
        }

        switch (target) {
            case CrossCompileTarget.HLSL:
                SpirvCrossApi.spvc_compiler_options_set_uint(opts, CompilerOption.HLSLShaderModel, 50);
                SpirvCrossApi.spvc_compiler_options_set_bool(opts, CompilerOption.HLSLPointSizeCompat, 1);
                break;

            case CrossCompileTarget.GLSL: {
                    uint version = isCompute || hasStorageResources ? 430u : 330u;
                    SpirvCrossApi.spvc_compiler_options_set_uint(opts, CompilerOption.GLSLVersion, version);
                    SpirvCrossApi.spvc_compiler_options_set_bool(opts, CompilerOption.GLSLES, 0);
                    SpirvCrossApi.spvc_compiler_options_set_bool(opts, CompilerOption.GLSLEnable420packExtension, 0);
                    break;
                }

            case CrossCompileTarget.MSL:
                SpirvCrossApi.spvc_compiler_options_set_bool(opts, CompilerOption.MSLEnableDecorationBinding, 1);
                break;
        }

        Check(default, SpirvCrossApi.spvc_compiler_install_compiler_options(compiler, opts));
    }

    /// <summary>
    /// Sets the specializations value.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    private static void SetSpecializations(SpvcCompiler compiler, CrossCompileOptions options) {
        if (options.Specializations.Length == 0) {
            return;
        }

        Vortice.SpirvCross.spvc_specialization_constant* constants = null;
        nuint count = 0;
        SpirvCrossApi.spvc_compiler_get_specialization_constants(compiler, &constants, &count);

        for (int i = 0; i < options.Specializations.Length; i++) {
            uint constID = options.Specializations[i].ID;

            uint varID = 0;
            for (nuint j = 0; j < count; j++) {
                if (constants[j].constant_id == constID) {
                    varID = constants[j].id;
                    break;
                }
            }

            if (varID != 0) {
                spvc_constant constant = SpirvCrossApi.spvc_compiler_get_constant_handle(compiler, varID);
                // Write the raw u64 value, matching upstream's direct `constVar.m.c[0].r[0].u64 = value`
                SpirvCrossApi.spvc_constant_set_scalar_u64(constant, 0, 0, options.Specializations[i].Data);
            }
        }
    }

    #endregion

    #region Resource Collection

    /// <summary>
    /// Executes the collect resources logic for this backend.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="idIndex">The id index value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool CollectResources(SpvcCompiler compiler, SortedDictionary<BindingKey, ResourceInfo> allResources, uint idIndex, bool normalizeResourceNames) {
        SpvcResources resources = default;
        Check(default, SpirvCrossApi.spvc_compiler_create_shader_resources(compiler, &resources));

        bool hasStorage = false;

        AddResourcesOfType(compiler, resources, ResourceType.UniformBuffer, allResources, idIndex, normalizeResourceNames, ResourceKind.UniformBuffer);

        hasStorage |= AddStorageBuffers(compiler, resources, allResources, idIndex, normalizeResourceNames);

        AddResourcesOfType(compiler, resources, ResourceType.SeparateImage, allResources, idIndex, normalizeResourceNames, ResourceKind.TextureReadOnly);

        hasStorage |= AddResourcesOfType(compiler, resources, ResourceType.StorageImage, allResources, idIndex, normalizeResourceNames, ResourceKind.TextureReadWrite);

        AddResourcesOfType(compiler, resources, ResourceType.SeparateSamplers, allResources, idIndex, normalizeResourceNames, ResourceKind.Sampler);

        return hasStorage;
    }

    /// <summary>
    /// Executes the add resources of type logic for this backend.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="resources">The resource involved in this operation.</param>
    /// <param name="resourceType">The resource type value used by this operation.</param>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="idIndex">The id index value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool AddResourcesOfType(SpvcCompiler compiler, SpvcResources resources, ResourceType resourceType, SortedDictionary<BindingKey, ResourceInfo> allResources, uint idIndex, bool normalizeResourceNames, ResourceKind kind) {
        spvc_reflected_resource* resourceList = null;
        nuint resourceCount = 0;
        SpirvCrossApi.spvc_resources_get_resource_list_for_type(resources, resourceType, &resourceList, &resourceCount);

        bool any = false;
        for (nuint i = 0; i < resourceCount; i++) {
            any = true;
            ref spvc_reflected_resource resource = ref resourceList[i];
            uint set = SpirvCrossApi.spvc_compiler_get_decoration(compiler, resource.id, SpvDecoration.DescriptorSet);
            uint binding = SpirvCrossApi.spvc_compiler_get_decoration(compiler, resource.id, SpvDecoration.Binding);

            string name = GetOrSetResourceName(compiler, ref resource, kind, set, binding, normalizeResourceNames);

            InsertResource(allResources, set, binding, resource.id, idIndex, name, kind);
        }

        return any;
    }

    /// <summary>
    /// Executes the add storage buffers logic for this backend.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="resources">The resource involved in this operation.</param>
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="idIndex">The id index value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool AddStorageBuffers(SpvcCompiler compiler, SpvcResources resources, SortedDictionary<BindingKey, ResourceInfo> allResources, uint idIndex, bool normalizeResourceNames) {
        spvc_reflected_resource* resourceList = null;
        nuint resourceCount = 0;
        SpirvCrossApi.spvc_resources_get_resource_list_for_type(resources, ResourceType.StorageBuffer, &resourceList, &resourceCount);

        bool any = false;
        for (nuint i = 0; i < resourceCount; i++) {
            any = true;
            ref spvc_reflected_resource resource = ref resourceList[i];

            // Uses get_buffer_block_decorations (matching upstream's get_buffer_block_flags) which checks
            // both variable-level and member-level decorations, not just the variable itself.
            bool isNonWritable = HasBufferBlockDecoration(compiler, resource.id, SpvDecoration.NonWritable);
            ResourceKind kind = isNonWritable ? ResourceKind.StructuredBufferReadOnly : ResourceKind.StructuredBufferReadWrite;

            uint set = SpirvCrossApi.spvc_compiler_get_decoration(compiler, resource.id, SpvDecoration.DescriptorSet);
            uint binding = SpirvCrossApi.spvc_compiler_get_decoration(compiler, resource.id, SpvDecoration.Binding);

            string name;
            if (normalizeResourceNames) {
                name = $"vdspv_{set}_{binding}";
                SetNativeName(compiler, resource.id, name);
            }
            else {
                name = GetNativeName(compiler, resource.id, resource.base_type_id);
            }

            InsertResource(allResources, set, binding, resource.id, idIndex, name, kind);
        }

        return any;
    }

    /// <summary>
    /// Gets the or set resource name value.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="binding">The binding value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetOrSetResourceName(SpvcCompiler compiler, ref spvc_reflected_resource resource, ResourceKind kind, uint set, uint binding, bool normalizeResourceNames) {
        if (normalizeResourceNames) {
            string name = $"vdspv_{set}_{binding}";
            uint nameTarget = kind == ResourceKind.UniformBuffer ? resource.base_type_id : resource.id;
            SetNativeName(compiler, nameTarget, name);
            return name;
        }

        return GetNativeName(compiler, resource.id, resource.base_type_id);
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
    /// <param name="allResources">The resource involved in this operation.</param>
    /// <param name="compiler0">The compiler0 value used by this operation.</param>
    /// <param name="compiler1">The compiler1 value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    private static void RemapBindingsHlslMsl(SortedDictionary<BindingKey, ResourceInfo> allResources, SpvcCompiler compiler0, SpvcCompiler? compiler1, CrossCompileTarget target, bool hasPushConstants, uint pushConstantId0, uint pushConstantId1) {
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
                SpirvCrossApi.spvc_compiler_set_decoration(compiler0, id0, SpvDecoration.Binding, index);
            }

            if (compiler1.HasValue) {
                uint id1 = kvp.Value.IDs[1];
                if (id1 != 0) {
                    SpirvCrossApi.spvc_compiler_set_decoration(compiler1.Value, id1, SpvDecoration.Binding, index);
                }
            }
        }

        if (target == CrossCompileTarget.MSL && hasPushConstants) {
            if (pushConstantId0 != 0) {
                SpirvCrossApi.spvc_compiler_set_decoration(compiler0, pushConstantId0, SpvDecoration.Binding, bufferIndex);
            }

            if (compiler1.HasValue && pushConstantId1 != 0) {
                SpirvCrossApi.spvc_compiler_set_decoration(compiler1.Value, pushConstantId1, SpvDecoration.Binding, bufferIndex);
            }
        }
    }

    #endregion

    #region GLSL Specific

    /// <summary>
    /// Executes the build combined image samplers logic for this backend.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    private static void BuildCombinedImageSamplers(SpvcCompiler compiler) {
        uint dummySamplerId = 0;
        Check(default, SpirvCrossApi.spvc_compiler_build_dummy_sampler_for_combined_images(compiler, &dummySamplerId));
        Check(default, SpirvCrossApi.spvc_compiler_build_combined_image_samplers(compiler));

        spvc_combined_image_sampler* combinedSamplers = null;
        nuint count = 0;
        SpirvCrossApi.spvc_compiler_get_combined_image_samplers(compiler, &combinedSamplers, &count);

        for (nuint i = 0; i < count; i++) {
            string imageName = SpirvCrossApi.spvc_compiler_get_name(compiler, combinedSamplers[i].image_id);
            if (!string.IsNullOrEmpty(imageName)) {
                SetNativeName(compiler, combinedSamplers[i].combined_id, imageName);
            }
        }
    }

    /// <summary>
    /// Executes the rename stage io logic for this backend.
    /// </summary>
    /// <param name="vsCompiler">The vs compiler value used by this operation.</param>
    /// <param name="fsCompiler">The fs compiler value used by this operation.</param>
    private static void RenameStageIO(SpvcCompiler vsCompiler, SpvcCompiler fsCompiler) {
        // Rename vertex outputs to vdspv_fsinN
        SpvcResources vsResources = default;
        Check(default, SpirvCrossApi.spvc_compiler_create_shader_resources(vsCompiler, &vsResources));
        spvc_reflected_resource* vsOutputs = null;
        nuint vsOutputCount = 0;
        SpirvCrossApi.spvc_resources_get_resource_list_for_type(vsResources, ResourceType.StageOutput, &vsOutputs, &vsOutputCount);

        for (nuint i = 0; i < vsOutputCount; i++) {
            uint location = SpirvCrossApi.spvc_compiler_get_decoration(vsCompiler, vsOutputs[i].id, SpvDecoration.Location);
            SetNativeName(vsCompiler, vsOutputs[i].id, $"vdspv_fsin{location}");
        }

        // Rename fragment inputs to vdspv_fsinN
        SpvcResources fsResources = default;
        Check(default, SpirvCrossApi.spvc_compiler_create_shader_resources(fsCompiler, &fsResources));
        spvc_reflected_resource* fsInputs = null;
        nuint fsInputCount = 0;
        SpirvCrossApi.spvc_resources_get_resource_list_for_type(fsResources, ResourceType.StageInput, &fsInputs, &fsInputCount);

        for (nuint i = 0; i < fsInputCount; i++) {
            uint location = SpirvCrossApi.spvc_compiler_get_decoration(fsCompiler, fsInputs[i].id, SpvDecoration.Location);
            SetNativeName(fsCompiler, fsInputs[i].id, $"vdspv_fsin{location}");
        }
    }

    #endregion

    #region Reflection

    /// <summary>
    /// Executes the reflect vertex inputs logic for this backend.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static VertexElementDescription[] ReflectVertexInputs(SpvcCompiler compiler) {
        SpvcResources resources = default;
        Check(default, SpirvCrossApi.spvc_compiler_create_shader_resources(compiler, &resources));
        spvc_reflected_resource* inputs = null;
        nuint inputCount = 0;
        SpirvCrossApi.spvc_resources_get_resource_list_for_type(resources, ResourceType.StageInput, &inputs, &inputCount);

        uint elementCount = 0;
        for (nuint i = 0; i < inputCount; i++) {
            uint location = SpirvCrossApi.spvc_compiler_get_decoration(compiler, inputs[i].id, SpvDecoration.Location);
            elementCount = Math.Max(elementCount, location + 1);
        }

        VertexElementDescription[] elements = new VertexElementDescription[elementCount];
        for (nuint i = 0; i < inputCount; i++) {
            uint location = SpirvCrossApi.spvc_compiler_get_decoration(compiler, inputs[i].id, SpvDecoration.Location);

            string name = SpirvCrossApi.spvc_compiler_get_name(compiler, inputs[i].id);
            if (string.IsNullOrEmpty(name)) {
                name = $"_input{location}";
            }

            spvc_type type = SpirvCrossApi.spvc_compiler_get_type_handle(compiler, inputs[i].base_type_id);
            Basetype baseType = SpirvCrossApi.spvc_type_get_basetype(type);
            uint vecSize = SpirvCrossApi.spvc_type_get_vector_size(type);

            VertexElementFormat format = baseType switch {
                Basetype.Fp32 => vecSize switch {
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
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="fallbackId">The fallback id value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetNativeName(SpvcCompiler compiler, uint id, uint fallbackId) {
        string name = SpirvCrossApi.spvc_compiler_get_name(compiler, id);
        if (string.IsNullOrEmpty(name)) {
            name = SpirvCrossApi.spvc_compiler_get_name(compiler, fallbackId);
        }

        return name;
    }

    /// <summary>
    /// Sets the native name value.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="name">The name used by this operation.</param>
    private static void SetNativeName(SpvcCompiler compiler, uint id, string name) {
        byte[] nameBytes = Encoding.UTF8.GetBytes(name + '\0');
        fixed (byte* namePtr = nameBytes) {
            SpirvCrossApi.spvc_compiler_set_name(compiler, id, namePtr);
        }
    }

    /// <summary>
    /// Executes the has buffer block decoration logic for this backend.
    /// </summary>
    /// <param name="compiler">The compiler value used by this operation.</param>
    /// <param name="id">The id value used by this operation.</param>
    /// <param name="decoration">The decoration value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool HasBufferBlockDecoration(SpvcCompiler compiler, uint id, SpvDecoration decoration) {
        SpvDecoration* decorations = null;
        nuint count = 0;
        SpirvCrossApi.spvc_compiler_get_buffer_block_decorations(compiler, id, &decorations, &count);
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
    /// <param name="compiler">The compiler handle.</param>
    /// <returns>The reflected push-constant resource id, or <c>0</c> when none exists.</returns>
    private static uint GetPushConstantId(SpvcCompiler compiler) {
        SpvcResources resources = default;
        Check(default, SpirvCrossApi.spvc_compiler_create_shader_resources(compiler, &resources));

        spvc_reflected_resource* pushConstants = null;
        nuint pushConstantCount = 0;
        SpirvCrossApi.spvc_resources_get_resource_list_for_type(resources, ResourceType.PushConstant, &pushConstants, &pushConstantCount);
        if (pushConstantCount == 0) {
            return 0;
        }

        if (pushConstantCount > 1) {
            throw new SpirvCompilationException("Multiple push-constant blocks are not supported.");
        }

        return pushConstants[0].id;
    }

    #endregion
}
