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
/// Defines the behavior and responsibilities of the SpirvCrossCompiler class.
/// </summary>
internal static unsafe class SpirvCrossCompiler {

    /// <summary>
    /// Executes the GetApi operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetApi operation.</returns>
    private static readonly Cross s_cross = Cross.GetApi();

    /// <summary>
    /// Executes the CompileVertexFragment operation.
    /// </summary>
    /// <param name="vsSpirv">Specifies the value of <paramref name="vsSpirv" />.</param>
    /// <param name="fsSpirv">Specifies the value of <paramref name="fsSpirv" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the CompileVertexFragment operation.</returns>
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

            // Set compiler options (GLSL version depends on whether storage resources are present)
            bool hasStorageResources = hasVsStorage || hasFsStorage;
            SetCompilerOptions(cross, vsCompiler, target, options, false, hasStorageResources);
            SetCompilerOptions(cross, fsCompiler, target, options, false, hasStorageResources);

            SetSpecializations(cross, vsCompiler, options);
            SetSpecializations(cross, fsCompiler, options);

            if (target == CrossCompileTarget.HLSL || target == CrossCompileTarget.MSL) {
                RemapBindingsHlslMsl(cross, allResources, vsCompiler, fsCompiler, target);
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
    /// Executes the CompileCompute operation.
    /// </summary>
    /// <param name="csSpirv">Specifies the value of <paramref name="csSpirv" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the CompileCompute operation.</returns>
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

            SetCompilerOptions(cross, csCompiler, target, options, true, hasStorage);
            SetSpecializations(cross, csCompiler, options);

            if (target == CrossCompileTarget.HLSL || target == CrossCompileTarget.MSL) {
                RemapBindingsHlslMsl(cross, allResources, csCompiler, null, target);
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
    private readonly struct BindingKey(uint set, uint binding) : IComparable<BindingKey> {

        /// <summary>
        /// Stores the value associated with <c>Set</c>.
        /// </summary>
        public readonly uint Set = set;

        /// <summary>
        /// Stores the value associated with <c>Binding</c>.
        /// </summary>
        public readonly uint Binding = binding;

        /// <summary>
        /// Executes the CompareTo operation.
        /// </summary>
        /// <param name="other">Specifies the value of <paramref name="other" />.</param>
        /// <returns>Returns the result produced by the CompareTo operation.</returns>
        public int CompareTo(BindingKey other) {
            int c = this.Set.CompareTo(other.Set);
            return c != 0 ? c : this.Binding.CompareTo(other.Binding);
        }
    }

    /// <summary>
    /// Defines the behavior and responsibilities of the ResourceInfo class.
    /// </summary>
    private class ResourceInfo {

        /// <summary>
        /// Stores the value associated with <c>IDs</c>.
        /// </summary>
        public readonly uint[] IDs = new uint[2]; // 0 = VS/CS, 1 = FS

        /// <summary>
        /// Stores the value associated with <c>Kind</c>.
        /// </summary>
        public ResourceKind Kind;

        /// <summary>
        /// Stores the value associated with <c>Name</c>.
        /// </summary>
        public string Name;
    }

    #endregion

    #region Compiler Setup

    /// <summary>
    /// Executes the Check operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="ctx">Specifies the value of <paramref name="ctx" />.</param>
    /// <param name="result">Specifies the value of <paramref name="result" />.</param>
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
    /// Executes the GetSpvcBackend operation.
    /// </summary>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <returns>Returns the result produced by the GetSpvcBackend operation.</returns>
    private static SpvcBackend GetSpvcBackend(CrossCompileTarget target) {
        return target switch {
            CrossCompileTarget.HLSL => SpvcBackend.Hlsl,
            CrossCompileTarget.GLSL => SpvcBackend.Glsl,
            CrossCompileTarget.MSL => SpvcBackend.Msl,
            _ => throw new SpirvCompilationException($"Invalid CrossCompileTarget: {target}")
        };
    }

    /// <summary>
    /// Executes the SetCompilerOptions operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <param name="isCompute">Specifies the value of <paramref name="isCompute" />.</param>
    /// <param name="hasStorageResources">Specifies the value of <paramref name="hasStorageResources" />.</param>
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

            case CrossCompileTarget.MSL: break;
        }

        Check(cross, null, cross.CompilerInstallCompilerOptions(compiler, opts));
    }

    /// <summary>
    /// Executes the SetSpecializations operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
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
    /// Executes the CollectResources operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="allResources">Specifies the value of <paramref name="allResources" />.</param>
    /// <param name="idIndex">Specifies the value of <paramref name="idIndex" />.</param>
    /// <param name="normalizeResourceNames">Specifies the value of <paramref name="normalizeResourceNames" />.</param>
    /// <returns>Returns the result produced by the CollectResources operation.</returns>
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
    /// Executes the AddResourcesOfType operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="resources">Specifies the value of <paramref name="resources" />.</param>
    /// <param name="resourceType">Specifies the value of <paramref name="resourceType" />.</param>
    /// <param name="allResources">Specifies the value of <paramref name="allResources" />.</param>
    /// <param name="idIndex">Specifies the value of <paramref name="idIndex" />.</param>
    /// <param name="normalizeResourceNames">Specifies the value of <paramref name="normalizeResourceNames" />.</param>
    /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
    /// <returns>Returns the result produced by the AddResourcesOfType operation.</returns>
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
    /// Executes the AddStorageBuffers operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="resources">Specifies the value of <paramref name="resources" />.</param>
    /// <param name="allResources">Specifies the value of <paramref name="allResources" />.</param>
    /// <param name="idIndex">Specifies the value of <paramref name="idIndex" />.</param>
    /// <param name="normalizeResourceNames">Specifies the value of <paramref name="normalizeResourceNames" />.</param>
    /// <returns>Returns the result produced by the AddStorageBuffers operation.</returns>
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
    /// Executes the GetOrSetResourceName operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="binding">Specifies the value of <paramref name="binding" />.</param>
    /// <param name="normalizeResourceNames">Specifies the value of <paramref name="normalizeResourceNames" />.</param>
    /// <returns>Returns the result produced by the GetOrSetResourceName operation.</returns>
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
    /// Executes the InsertResource operation.
    /// </summary>
    /// <param name="allResources">Specifies the value of <paramref name="allResources" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="binding">Specifies the value of <paramref name="binding" />.</param>
    /// <param name="resourceId">Specifies the value of <paramref name="resourceId" />.</param>
    /// <param name="idIndex">Specifies the value of <paramref name="idIndex" />.</param>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
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
    /// Executes the GetResourceIndex operation.
    /// </summary>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
    /// <param name="bufferIndex">Specifies the value of <paramref name="bufferIndex" />.</param>
    /// <param name="textureIndex">Specifies the value of <paramref name="textureIndex" />.</param>
    /// <param name="uavIndex">Specifies the value of <paramref name="uavIndex" />.</param>
    /// <param name="samplerIndex">Specifies the value of <paramref name="samplerIndex" />.</param>
    /// <returns>Returns the result produced by the GetResourceIndex operation.</returns>
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
    /// Executes the RemapBindingsHlslMsl operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="allResources">Specifies the value of <paramref name="allResources" />.</param>
    /// <param name="compiler0">Specifies the value of <paramref name="compiler0" />.</param>
    /// <param name="compiler1">Specifies the value of <paramref name="compiler1" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    private static void RemapBindingsHlslMsl(Cross cross, SortedDictionary<BindingKey, ResourceInfo> allResources, SpvcCompiler* compiler0, SpvcCompiler* compiler1, CrossCompileTarget target) {
        uint bufferIndex = 0, textureIndex = 0, uavIndex = 0, samplerIndex = 0;

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
    }

    #endregion

    #region GLSL Specific

    /// <summary>
    /// Executes the BuildCombinedImageSamplers operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
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
    /// Executes the RenameStageIO operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="vsCompiler">Specifies the value of <paramref name="vsCompiler" />.</param>
    /// <param name="fsCompiler">Specifies the value of <paramref name="fsCompiler" />.</param>
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
    /// Executes the ReflectVertexInputs operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <returns>Returns the result produced by the ReflectVertexInputs operation.</returns>
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
    /// Executes the BuildResourceLayouts operation.
    /// </summary>
    /// <param name="allResources">Specifies the value of <paramref name="allResources" />.</param>
    /// <param name="isCompute">Specifies the value of <paramref name="isCompute" />.</param>
    /// <returns>Returns the result produced by the BuildResourceLayouts operation.</returns>
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
    /// Executes the GetNativeName operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="fallbackId">Specifies the value of <paramref name="fallbackId" />.</param>
    /// <returns>Returns the result produced by the GetNativeName operation.</returns>
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
    /// Executes the SetNativeName operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    private static void SetNativeName(Cross cross, SpvcCompiler* compiler, uint id, string name) {
        byte[] nameBytes = Encoding.UTF8.GetBytes(name + '\0');
        fixed (byte* namePtr = nameBytes) {
            cross.CompilerSetName(compiler, id, namePtr);
        }
    }

    /// <summary>
    /// Executes the HasBufferBlockDecoration operation.
    /// </summary>
    /// <param name="cross">Specifies the value of <paramref name="cross" />.</param>
    /// <param name="compiler">Specifies the value of <paramref name="compiler" />.</param>
    /// <param name="id">Specifies the value of <paramref name="id" />.</param>
    /// <param name="decoration">Specifies the value of <paramref name="decoration" />.</param>
    /// <returns>Returns the result produced by the HasBufferBlockDecoration operation.</returns>
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

    #endregion
}