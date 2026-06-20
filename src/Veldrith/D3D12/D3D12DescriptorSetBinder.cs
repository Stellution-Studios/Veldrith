using System;
using System.Diagnostics;
using Vortice.Direct3D12;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Flushes D3D12 ResourceSet changes to root descriptors and descriptor tables.
/// </summary>
internal sealed class D3D12DescriptorSetBinder : IDisposable {

    /// <summary>
    /// Stores the command list that receives D3D12 binding and transition commands.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Tracks root descriptor state to avoid redundant D3D12 root binding calls.
    /// </summary>
    private readonly D3D12RootBindingCache _rootBindingCache;

    /// <summary>
    /// Stores optional performance counters updated while resource sets are flushed.
    /// </summary>
    private readonly D3D12CommandListPerfTracker _perf;

    /// <summary>
    /// Caches D3D12 root-binding plans for resource sets by pipeline, layout, and set slot.
    /// </summary>
    private readonly D3D12ResourceSetBindingPlanCache _bindingPlans = new();

    /// <summary>
    /// Owns shader-visible descriptor heaps and descriptor table copies for this command list.
    /// </summary>
    private readonly D3D12DescriptorHeapState _descriptorHeapState;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DescriptorSetBinder" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns descriptor resources.</param>
    /// <param name="commandList">The command list that receives bindings.</param>
    /// <param name="rootBindingCache">The root binding cache used to skip redundant root updates.</param>
    /// <param name="perf">The optional performance tracker updated by the binder.</param>
    internal D3D12DescriptorSetBinder(D3D12GraphicsDevice gd, D3D12CommandList commandList, D3D12RootBindingCache rootBindingCache, D3D12CommandListPerfTracker perf) {
        this._commandList = commandList;
        this._rootBindingCache = rootBindingCache;
        this._perf = perf;
        this._descriptorHeapState = new D3D12DescriptorHeapState(gd);
    }

    /// <summary>
    /// Resets per-recording descriptor heap and fast binding-plan state.
    /// </summary>
    internal void BeginRecording() {
        this._descriptorHeapState.BeginRecording();
        this._bindingPlans.ClearPipelineCaches();
    }

    /// <summary>
    /// Flushes dirty graphics resource sets to the currently bound graphics pipeline.
    /// </summary>
    /// <param name="resourceSets">The graphics resource-set state to flush.</param>
    /// <param name="pipeline">The active graphics pipeline.</param>
    internal void FlushGraphicsResourceSets(D3D12BoundResourceSetState resourceSets, D3D12Pipeline pipeline) {
        this.FlushResourceSets(resourceSets, pipeline, false);
    }

    /// <summary>
    /// Flushes dirty compute resource sets to the currently bound compute pipeline.
    /// </summary>
    /// <param name="resourceSets">The compute resource-set state to flush.</param>
    /// <param name="pipeline">The active compute pipeline.</param>
    internal void FlushComputeResourceSets(D3D12BoundResourceSetState resourceSets, D3D12Pipeline pipeline) {
        this.FlushResourceSets(resourceSets, pipeline, true);
    }

    /// <summary>
    /// Marks bound resource sets dirty when they reference a dynamic buffer whose GPU address changed.
    /// </summary>
    /// <param name="graphicsSets">The current graphics resource-set state.</param>
    /// <param name="graphicsPipeline">The active graphics pipeline.</param>
    /// <param name="computeSets">The current compute resource-set state.</param>
    /// <param name="computePipeline">The active compute pipeline.</param>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    internal void MarkResourceSetsReferencingBufferDirty(D3D12BoundResourceSetState graphicsSets, D3D12Pipeline graphicsPipeline, D3D12BoundResourceSetState computeSets, D3D12Pipeline computePipeline, D3D12DeviceBuffer buffer) {
        graphicsSets.MarkSetsReferencingBufferDirty(graphicsPipeline?.ResourceSetCount ?? 0u, buffer);
        computeSets.MarkSetsReferencingBufferDirty(computePipeline?.ResourceSetCount ?? 0u, buffer);
    }

    /// <summary>
    /// Releases descriptor heap resources held by this instance.
    /// </summary>
    public void Dispose() {
        this._descriptorHeapState.Dispose();
    }

    /// <summary>
    /// Flushes dirty resource sets for one D3D12 bind point.
    /// </summary>
    /// <param name="resourceSets">The resource-set state to flush.</param>
    /// <param name="pipeline">The active pipeline.</param>
    /// <param name="compute">Whether the active bind point is compute.</param>
    private void FlushResourceSets(D3D12BoundResourceSetState resourceSets, D3D12Pipeline pipeline, bool compute) {
        if (!resourceSets.Dirty || pipeline == null) {
            return;
        }

        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        int start = resourceSets.ChangedStart;
        int end = Math.Min(resourceSets.ChangedEnd, resourceSets.GetFlushEnd(pipeline.ResourceSetCount));
        if (start < 0 || end < start) {
            resourceSets.ResetDirtyRange();
            return;
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ResourceSetScanSlots += (ulong)(end - start + 1);
        }

        for (int slot = start; slot <= end; slot++) {
            if (!resourceSets.Changed[slot]) {
                continue;
            }

            resourceSets.Changed[slot] = false;
            D3D12ResourceSetChangeKind changeKind = resourceSets.ChangeKinds[slot];
            resourceSets.ChangeKinds[slot] = D3D12ResourceSetChangeKind.None;
            this.BindResourceSet(pipeline, (uint)slot, ref resourceSets.BoundSets[slot], compute, changeKind);
        }

        resourceSets.ResetDirtyRange();
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ResourceSetFlushMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Binds one dirty resource set to the active root signature.
    /// </summary>
    /// <param name="pipeline">The active pipeline.</param>
    /// <param name="slot">The resource set slot.</param>
    /// <param name="boundSet">The bound resource set information.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private void BindResourceSet(D3D12Pipeline pipeline, uint slot, ref BoundResourceSetInfo boundSet, bool compute, D3D12ResourceSetChangeKind changeKind) {
        if (slot >= pipeline.ResourceSetCount || boundSet.Set == null) {
            return;
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ResourceSetBinds++;
        }

        D3D12ResourceSet d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(boundSet.Set);
        D3D12ResourceSetElementCache[] elementCaches = d3d12Set.ElementCaches;
        D3D12ResourceSetBindingPlan bindingPlan = compute
            ? this._bindingPlans.GetCompute(pipeline, slot, d3d12Set.ResourceLayoutInfo)
            : this._bindingPlans.GetGraphics(pipeline, slot, d3d12Set.ResourceLayoutInfo);
        uint dynamicOffsetIndex = 0;
        bool descriptorTablesChanged = false;
        D3D12ResourceSetBindingPlanEntry[] entries = bindingPlan.Entries;
        bool rootBindingsOnly = changeKind == D3D12ResourceSetChangeKind.RootBindingsOnly;
        bool hasSrvUavTable = bindingPlan.SrvUavTable.HasTable;
        bool skipSrvUavTableResourcePreparation = hasSrvUavTable
                                                   && (rootBindingsOnly
                                                       || CanSkipDescriptorTableResourcePreparation(d3d12Set, entries, bindingPlan.SrvUavTable, compute));

        for (int i = 0; i < entries.Length; i++) {
            ref readonly D3D12ResourceSetBindingPlanEntry bindingEntry = ref entries[i];
            uint dynamicOffset = 0;
            if (bindingEntry.IsDynamicBinding) {
                if (dynamicOffsetIndex >= boundSet.Offsets.Count) {
                    throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                }

                dynamicOffset = boundSet.Offsets.Get(dynamicOffsetIndex);
                dynamicOffsetIndex++;
            }

            D3D12ResourceSetElementCache elementCache = elementCaches[bindingEntry.ElementIndex];
            if (bindingEntry.BindingInfo.DescriptorTable) {
                if (rootBindingsOnly) {
                    continue;
                }

                if (!skipSrvUavTableResourcePreparation
                    && bindingEntry.BindingInfo.DescriptorTableKind == D3D12Pipeline.DescriptorTableKind.SrvUav) {
                    this.PrepareDescriptorTableResource(bindingEntry.BindingInfo.Kind, elementCache, compute);
                }

                descriptorTablesChanged = true;
                continue;
            }

            if (compute) {
                this.BindComputeResource(bindingEntry.BindingInfo, elementCache, dynamicOffset);
            }
            else {
                this.BindGraphicsResource(bindingEntry.BindingInfo, elementCache, dynamicOffset);
            }
        }

        if (!rootBindingsOnly && hasSrvUavTable && !skipSrvUavTableResourcePreparation) {
            StoreDescriptorTableResourcePreparation(d3d12Set, entries, bindingPlan.SrvUavTable, compute);
        }

        if (descriptorTablesChanged) {
            this.BindResourceSetDescriptorTables(d3d12Set, bindingPlan, compute);
        }
    }

    /// <summary>
    /// Binds a graphics root resource for subsequent draw commands.
    /// </summary>
    /// <param name="bindingInfo">The root binding metadata.</param>
    /// <param name="elementCache">The pre-resolved resource-set element cache.</param>
    /// <param name="dynamicOffset">The dynamic offset applied to buffer bindings.</param>
    private void BindGraphicsResource(D3D12Pipeline.RootBindingInfo bindingInfo, D3D12ResourceSetElementCache elementCache, uint dynamicOffset) {
        if (bindingInfo.DescriptorTable) {
            return;
        }

        D3D12DeviceBuffer d3D12Buffer = elementCache.Buffer
                                        ?? throw new VeldridException("D3D12 root binding requires a buffer resource.");
        uint rangeOffset = elementCache.BufferOffset + dynamicOffset;
        this._commandList.TransitionBufferForInternalUse(d3D12Buffer, GetGraphicsBufferState(bindingInfo.Kind));
        ulong gpuAddress = this._commandList.GetBufferGpuVirtualAddressForInternalUse(d3D12Buffer, rangeOffset);
        if (this._rootBindingCache.IsSameGraphicsRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
            return;
        }

        switch (bindingInfo.Kind) {
            case ResourceKind.UniformBuffer:
                this._commandList.SetGraphicsRootConstantBufferViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadOnly:
                this._commandList.SetGraphicsRootShaderResourceViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadWrite:
                this._commandList.SetGraphicsRootUnorderedAccessViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.TextureReadOnly: case ResourceKind.TextureReadWrite: case ResourceKind.Sampler: throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
            default: throw Illegal.Value<ResourceKind>();
        }

        this._rootBindingCache.SetGraphicsRootBuffer(bindingInfo.RootParameterIndex, gpuAddress);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.RootBufferSets++;
        }
    }

    /// <summary>
    /// Binds a compute root resource for subsequent dispatch commands.
    /// </summary>
    /// <param name="bindingInfo">The root binding metadata.</param>
    /// <param name="elementCache">The pre-resolved resource-set element cache.</param>
    /// <param name="dynamicOffset">The dynamic offset applied to buffer bindings.</param>
    private void BindComputeResource(D3D12Pipeline.RootBindingInfo bindingInfo, D3D12ResourceSetElementCache elementCache, uint dynamicOffset) {
        if (bindingInfo.DescriptorTable) {
            return;
        }

        D3D12DeviceBuffer d3D12Buffer = elementCache.Buffer
                                        ?? throw new VeldridException("D3D12 root binding requires a buffer resource.");
        uint rangeOffset = elementCache.BufferOffset + dynamicOffset;
        this._commandList.TransitionBufferForInternalUse(d3D12Buffer, GetComputeBufferState(bindingInfo.Kind));
        ulong gpuAddress = this._commandList.GetBufferGpuVirtualAddressForInternalUse(d3D12Buffer, rangeOffset);

        if (this._rootBindingCache.IsSameComputeRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
            return;
        }

        switch (bindingInfo.Kind) {
            case ResourceKind.UniformBuffer:
                this._commandList.SetComputeRootConstantBufferViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadOnly:
                this._commandList.SetComputeRootShaderResourceViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadWrite:
                this._commandList.SetComputeRootUnorderedAccessViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.TextureReadOnly: case ResourceKind.TextureReadWrite: case ResourceKind.Sampler: throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
            default: throw Illegal.Value<ResourceKind>();
        }

        this._rootBindingCache.SetComputeRootBuffer(bindingInfo.RootParameterIndex, gpuAddress);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.RootBufferSets++;
        }
    }

    /// <summary>
    /// Prepares a descriptor-table resource for binding by validating and transitioning it.
    /// </summary>
    /// <param name="kind">The resource kind.</param>
    /// <param name="elementCache">The pre-resolved resource-set element cache.</param>
    /// <param name="compute">Whether the resource is being used by a compute pipeline.</param>
    private void PrepareDescriptorTableResource(ResourceKind kind, D3D12ResourceSetElementCache elementCache, bool compute) {
        switch (kind) {
            case ResourceKind.TextureReadOnly: {
                    D3D12TextureView d3d12TextureView = elementCache.TextureView
                                                        ?? throw new VeldridException("D3D12 sampled descriptor-table binding requires a texture view.");
                    this._commandList.TransitionTextureViewForInternalUse(d3d12TextureView, GetDescriptorTableTextureReadState(compute));
                    break;
                }
            case ResourceKind.TextureReadWrite: {
                    D3D12TextureView d3D12TextureView = elementCache.TextureView
                                                        ?? throw new VeldridException("D3D12 storage descriptor-table binding requires a texture view.");
                    this._commandList.TransitionTextureViewForInternalUse(d3D12TextureView, ResourceStates.UnorderedAccess);
                    break;
                }
            case ResourceKind.Sampler: break;
            default: throw new VeldridException("Invalid descriptor-table binding kind.");
        }
    }

    /// <summary>
    /// Binds grouped descriptor tables for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan for the active pipeline and set slot.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private void BindResourceSetDescriptorTables(D3D12ResourceSet set, D3D12ResourceSetBindingPlan bindingPlan, bool compute) {
        this._descriptorHeapState.BindDescriptorHeaps(this._commandList.NativeCommandList);
        this.BindResourceSetDescriptorTable(set, bindingPlan.Entries, bindingPlan.SrvUavTable, compute);
        this.BindResourceSetDescriptorTable(set, bindingPlan.Entries, bindingPlan.SamplerTable, compute);
    }

    /// <summary>
    /// Binds one grouped descriptor table for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan for the active pipeline and set slot.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private void BindResourceSetDescriptorTable(D3D12ResourceSet set, D3D12ResourceSetBindingPlanEntry[] bindingPlan, D3D12DescriptorTableBindingInfo tableInfo, bool compute) {
        if (!tableInfo.HasTable) {
            return;
        }

        long descriptorCopyStartTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        uint copiedDescriptors = this._descriptorHeapState.GetOrCreateDescriptorTable(set, bindingPlan, tableInfo, out GpuDescriptorHandle gpuHandle);
        if (D3D12CommandListPerfTracker.Enabled && copiedDescriptors > 0) {
            this._perf.DescriptorCopyMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - descriptorCopyStartTicks);
            this._perf.DescriptorCopies += copiedDescriptors;
        }

        if ((compute && this._rootBindingCache.IsSameComputeRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr))
            || (!compute && this._rootBindingCache.IsSameGraphicsRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr))) {
            return;
        }

        if (compute) {
            this._commandList.SetComputeRootDescriptorTableNoAlloc(tableInfo.RootParameterIndex, gpuHandle);
            this._rootBindingCache.SetComputeRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr);
        }
        else {
            this._commandList.SetGraphicsRootDescriptorTableNoAlloc(tableInfo.RootParameterIndex, gpuHandle);
            this._rootBindingCache.SetGraphicsRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.RootTableSets++;
        }
    }

    /// <summary>
    /// Gets the required graphics-state transition for a root-buffer resource kind.
    /// </summary>
    /// <param name="kind">The resource kind.</param>
    /// <returns>The required D3D12 resource state.</returns>
    private static ResourceStates GetGraphicsBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Gets the required compute-state transition for a root-buffer resource kind.
    /// </summary>
    /// <param name="kind">The resource kind.</param>
    /// <returns>The required D3D12 resource state.</returns>
    private static ResourceStates GetComputeBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Checks whether descriptor-table texture preparation can be skipped for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan entries for the resource set slot.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <returns><see langword="true" /> when all table textures are known to be in the required state.</returns>
    private static bool CanSkipDescriptorTableResourcePreparation(D3D12ResourceSet set, D3D12ResourceSetBindingPlanEntry[] bindingPlan, D3D12DescriptorTableBindingInfo tableInfo, bool compute) {
        ulong stateVersionHash = ComputeDescriptorTableStateVersionHash(set, bindingPlan, tableInfo, compute, out uint textureCount);
        if (textureCount == 0) {
            return true;
        }

        ref D3D12DescriptorTableTransitionCache cache = ref GetSrvUavTransitionCache(set, compute);
        return cache.Matches(tableInfo.Signature, stateVersionHash, textureCount);
    }

    /// <summary>
    /// Stores descriptor-table texture preparation state for later resource-set binds.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan entries for the resource set slot.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private static void StoreDescriptorTableResourcePreparation(D3D12ResourceSet set, D3D12ResourceSetBindingPlanEntry[] bindingPlan, D3D12DescriptorTableBindingInfo tableInfo, bool compute) {
        ulong stateVersionHash = ComputeDescriptorTableStateVersionHash(set, bindingPlan, tableInfo, compute, out uint textureCount);
        if (textureCount == 0) {
            return;
        }

        ref D3D12DescriptorTableTransitionCache cache = ref GetSrvUavTransitionCache(set, compute);
        cache.Store(tableInfo.Signature, stateVersionHash, textureCount);
    }

    /// <summary>
    /// Computes a stable hash of the texture state versions required by one descriptor table.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan entries for the resource set slot.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <param name="textureCount">The number of texture descriptors included in the hash.</param>
    /// <returns>The combined state-version hash.</returns>
    private static ulong ComputeDescriptorTableStateVersionHash(D3D12ResourceSet set, D3D12ResourceSetBindingPlanEntry[] bindingPlan, D3D12DescriptorTableBindingInfo tableInfo, bool compute, out uint textureCount) {
        const ulong fnvOffsetBasis = 14695981039346656037ul;
        const ulong fnvPrime = 1099511628211ul;
        ulong hash = fnvOffsetBasis;
        textureCount = 0;

        for (int i = 0; i < bindingPlan.Length; i++) {
            D3D12ResourceSetBindingPlanEntry entry = bindingPlan[i];
            D3D12Pipeline.RootBindingInfo bindingInfo = entry.BindingInfo;
            if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableInfo.TableKind) {
                continue;
            }

            ResourceStates requiredState;
            switch (bindingInfo.Kind) {
                case ResourceKind.TextureReadOnly:
                    requiredState = GetDescriptorTableTextureReadState(compute);
                    break;
                case ResourceKind.TextureReadWrite:
                    requiredState = ResourceStates.UnorderedAccess;
                    break;
                case ResourceKind.Sampler:
                    continue;
                default:
                    continue;
            }

            D3D12TextureView textureView = set.ElementCaches[entry.ElementIndex].TextureView
                                           ?? throw new VeldridException("D3D12 descriptor-table texture binding requires a texture view.");
            hash = (hash ^ entry.ElementIndex) * fnvPrime;
            hash = (hash ^ (ulong)requiredState) * fnvPrime;
            hash = (hash ^ textureView.TargetTexture.StateVersion) * fnvPrime;
            textureCount++;
        }

        return hash;
    }

    /// <summary>
    /// Gets the descriptor-table transition cache for a bind point.
    /// </summary>
    /// <param name="set">The resource set that owns the cache.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <returns>The descriptor-table transition cache.</returns>
    private static ref D3D12DescriptorTableTransitionCache GetSrvUavTransitionCache(D3D12ResourceSet set, bool compute) {
        if (compute) {
            return ref set.ComputeSrvUavTransitionCache;
        }

        return ref set.GraphicsSrvUavTransitionCache;
    }

    /// <summary>
    /// Gets the required read state for sampled descriptor-table textures.
    /// </summary>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <returns>The required D3D12 resource state.</returns>
    private static ResourceStates GetDescriptorTableTextureReadState(bool compute) {
        return compute ? ResourceStates.NonPixelShaderResource : ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource;
    }
}
