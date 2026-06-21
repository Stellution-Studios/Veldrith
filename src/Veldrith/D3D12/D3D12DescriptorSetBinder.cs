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
    /// Owns device-global shader-visible descriptor heaps and descriptor table copies.
    /// </summary>
    private readonly D3D12DescriptorHeapState _descriptorHeapState;

    /// <summary>
    /// Tracks whether the global descriptor heaps have been bound for the current command-list recording.
    /// </summary>
    private bool _descriptorHeapsBound;

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
        this._descriptorHeapState = gd.DescriptorHeapState;
    }

    /// <summary>
    /// Resets per-recording descriptor heap binding state.
    /// </summary>
    internal void BeginRecording() {
        this._descriptorHeapsBound = false;
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
            this._perf.ResourceSetScanSlots += (ulong)resourceSets.ChangedSlotCount;
        }

        int changedSlotCount = resourceSets.ChangedSlotCount;
        int[] changedSlots = resourceSets.ChangedSlots;
        if (changedSlotCount == 1) {
            int slot = changedSlots[0];
            if (slot >= start && slot <= end && resourceSets.Changed[slot]) {
                D3D12ResourceSetChangeKind changeKind = resourceSets.ChangeKinds[slot];
                this.BindResourceSet(pipeline, (uint)slot, ref resourceSets.BoundSets[slot], compute, changeKind);
            }

            resourceSets.ResetSingleDirtySlot(slot);
            if (D3D12CommandListPerfTracker.Enabled) {
                this._perf.ResourceSetFlushMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        for (int changedSlotIndex = 0; changedSlotIndex < changedSlotCount; changedSlotIndex++) {
            int slot = changedSlots[changedSlotIndex];
            if (slot < start || slot > end) {
                continue;
            }

            if (!resourceSets.Changed[slot]) {
                continue;
            }

            D3D12ResourceSetChangeKind changeKind = resourceSets.ChangeKinds[slot];
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

        if (bindingPlan.SingleUniformRootBindingOnly) {
            this.BindSingleUniformBufferResourceSet(ref boundSet, d3d12Set, bindingPlan.SingleRootBinding, compute);
            return;
        }

        if (bindingPlan.SingleRootBindingOnly) {
            this.BindSingleRootResourceSet(ref boundSet, d3d12Set, bindingPlan.SingleRootBinding, compute);
            return;
        }

        bool rootBindingsOnly = changeKind == D3D12ResourceSetChangeKind.RootBindingsOnly;
        if (bindingPlan.DescriptorTablesOnly) {
            if (!rootBindingsOnly) {
                this.BindDescriptorTableOnlyResourceSet(d3d12Set, bindingPlan, compute);
            }

            return;
        }

        uint dynamicOffsetIndex = 0;
        bool descriptorTablesChanged = false;
        D3D12ResourceSetBindingPlanEntry[] entries = bindingPlan.Entries;
        bool hasSrvUavTable = bindingPlan.SrvUavTable.HasTable;
        bool skipSrvUavTableResourcePreparation = hasSrvUavTable
                                                   && (rootBindingsOnly
                                                       || CanSkipDescriptorTableResourcePreparation(d3d12Set, bindingPlan.SrvUavTable, compute));

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
                    this.PrepareDescriptorTableTextureResource(bindingEntry.BindingInfo.Kind, elementCache, compute);
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
            StoreDescriptorTableResourcePreparation(d3d12Set, bindingPlan.SrvUavTable, compute);
        }

        if (descriptorTablesChanged) {
            this.BindResourceSetDescriptorTables(d3d12Set, bindingPlan, compute);
        }
    }

    /// <summary>
    /// Binds the common one-buffer ResourceSet path without the general descriptor-table loop.
    /// </summary>
    /// <param name="boundSet">The bound resource-set information.</param>
    /// <param name="set">The D3D12 resource set.</param>
    /// <param name="entry">The single root buffer binding entry.</param>
    /// <param name="compute">Whether the active pipeline is compute.</param>
    private void BindSingleRootResourceSet(ref BoundResourceSetInfo boundSet, D3D12ResourceSet set, D3D12ResourceSetBindingPlanEntry entry, bool compute) {
        uint dynamicOffset = 0;
        if (entry.IsDynamicBinding) {
            if (boundSet.Offsets.Count == 0) {
                throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
            }

            dynamicOffset = boundSet.Offsets.Get(0);
        }

        D3D12ResourceSetElementCache elementCache = set.ElementCaches[entry.ElementIndex];
        if (compute) {
            this.BindComputeResource(entry.BindingInfo, elementCache, dynamicOffset);
        }
        else {
            this.BindGraphicsResource(entry.BindingInfo, elementCache, dynamicOffset);
        }
    }

    /// <summary>
    /// Binds the common one-uniform-buffer ResourceSet path without the generic root-resource switch.
    /// </summary>
    /// <param name="boundSet">The bound resource-set information.</param>
    /// <param name="set">The D3D12 resource set.</param>
    /// <param name="entry">The single uniform-buffer binding entry.</param>
    /// <param name="compute">Whether the active pipeline is compute.</param>
    private void BindSingleUniformBufferResourceSet(ref BoundResourceSetInfo boundSet, D3D12ResourceSet set, D3D12ResourceSetBindingPlanEntry entry, bool compute) {
        uint dynamicOffset = 0;
        if (entry.IsDynamicBinding) {
            if (boundSet.Offsets.Count == 0) {
                throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
            }

            dynamicOffset = boundSet.Offsets.Get(0);
        }

        D3D12ResourceSetElementCache elementCache = set.ElementCaches[entry.ElementIndex];
        D3D12DeviceBuffer d3D12Buffer = elementCache.Buffer
                                        ?? throw new VeldridException("D3D12 root binding requires a buffer resource.");
        uint rangeOffset = elementCache.BufferOffset + dynamicOffset;
        if (d3D12Buffer.CanTransitionState) {
            this._commandList.TransitionBufferForInternalUse(d3D12Buffer, ResourceStates.VertexAndConstantBuffer);
        }

        ulong gpuAddress = this._commandList.GetBufferGpuVirtualAddressForInternalUse(d3D12Buffer, rangeOffset);
        uint rootParameterIndex = entry.BindingInfo.RootParameterIndex;

        if (compute) {
            if (!this._rootBindingCache.TryUpdateComputeRootBuffer(rootParameterIndex, gpuAddress)) {
                return;
            }

            this._commandList.SetComputeRootConstantBufferViewNoAlloc(rootParameterIndex, gpuAddress);
        }
        else {
            if (!this._rootBindingCache.TryUpdateGraphicsRootBuffer(rootParameterIndex, gpuAddress)) {
                return;
            }

            this._commandList.SetGraphicsRootConstantBufferViewNoAlloc(rootParameterIndex, gpuAddress);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.RootBufferSets++;
        }
    }

    /// <summary>
    /// Binds the common texture/sampler ResourceSet path without scanning root-buffer entries.
    /// </summary>
    /// <param name="set">The D3D12 resource set.</param>
    /// <param name="bindingPlan">The cached descriptor-table-only binding plan.</param>
    /// <param name="compute">Whether the active pipeline is compute.</param>
    private void BindDescriptorTableOnlyResourceSet(D3D12ResourceSet set, D3D12ResourceSetBindingPlan bindingPlan, bool compute) {
        bool hasSrvUavTable = bindingPlan.SrvUavTable.HasTable;
        bool skipSrvUavTableResourcePreparation = hasSrvUavTable
                                                   && CanSkipDescriptorTableResourcePreparation(set, bindingPlan.SrvUavTable, compute);
        if (hasSrvUavTable && !skipSrvUavTableResourcePreparation) {
            this.PrepareDescriptorTableResources(set, bindingPlan.SrvUavTable, compute);
            StoreDescriptorTableResourcePreparation(set, bindingPlan.SrvUavTable, compute);
        }

        this.BindResourceSetDescriptorTables(set, bindingPlan, compute);
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
        if (d3D12Buffer.CanTransitionState) {
            this._commandList.TransitionBufferForInternalUse(d3D12Buffer, GetGraphicsBufferState(bindingInfo.Kind));
        }

        ulong gpuAddress = this._commandList.GetBufferGpuVirtualAddressForInternalUse(d3D12Buffer, rangeOffset);
        if (!this._rootBindingCache.TryUpdateGraphicsRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
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
        if (d3D12Buffer.CanTransitionState) {
            this._commandList.TransitionBufferForInternalUse(d3D12Buffer, GetComputeBufferState(bindingInfo.Kind));
        }

        ulong gpuAddress = this._commandList.GetBufferGpuVirtualAddressForInternalUse(d3D12Buffer, rangeOffset);

        if (!this._rootBindingCache.TryUpdateComputeRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
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

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.RootBufferSets++;
        }
    }

    /// <summary>
    /// Prepares a descriptor-table texture resource for binding by validating and transitioning it.
    /// </summary>
    /// <param name="kind">The resource kind.</param>
    /// <param name="elementCache">The pre-resolved resource-set element cache.</param>
    /// <param name="compute">Whether the resource is being used by a compute pipeline.</param>
    private void PrepareDescriptorTableTextureResource(ResourceKind kind, D3D12ResourceSetElementCache elementCache, bool compute) {
        D3D12TextureView textureView = elementCache.TextureView
                                       ?? throw new VeldridException("D3D12 descriptor-table texture binding requires a texture view.");
        if (kind == ResourceKind.TextureReadOnly) {
            this._commandList.TransitionTextureViewForInternalUse(textureView, GetDescriptorTableTextureReadState(compute));
            return;
        }

        this._commandList.TransitionTextureViewForInternalUse(textureView, ResourceStates.UnorderedAccess);
    }

    /// <summary>
    /// Prepares all resources used by one descriptor table for binding.
    /// </summary>
    /// <param name="set">The D3D12 resource set.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the resource is being used by a compute pipeline.</param>
    private void PrepareDescriptorTableResources(D3D12ResourceSet set, D3D12DescriptorTableBindingInfo tableInfo, bool compute) {
        D3D12DescriptorTableTextureTransitionEntry[] entries = tableInfo.TextureTransitionEntries;
        D3D12ResourceSetElementCache[] elementCaches = set.ElementCaches;
        for (int i = 0; i < entries.Length; i++) {
            D3D12DescriptorTableTextureTransitionEntry entry = entries[i];
            this.PrepareDescriptorTableTextureResource(entry.Kind, elementCaches[entry.ElementIndex], compute);
        }
    }

    /// <summary>
    /// Binds grouped descriptor tables for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan for the active pipeline and set slot.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private void BindResourceSetDescriptorTables(D3D12ResourceSet set, D3D12ResourceSetBindingPlan bindingPlan, bool compute) {
        this.BindResourceSetDescriptorTable(set, bindingPlan.SrvUavTable, compute);
        this.BindResourceSetDescriptorTable(set, bindingPlan.SamplerTable, compute);
    }

    /// <summary>
    /// Binds the device-global shader-visible descriptor heaps once per command-list recording.
    /// </summary>
    private void BindDescriptorHeaps() {
        if (this._descriptorHeapsBound) {
            return;
        }

        this._descriptorHeapState.BindDescriptorHeaps(this._commandList.NativeCommandList);
        this._descriptorHeapsBound = true;
    }

    /// <summary>
    /// Binds one grouped descriptor table for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private void BindResourceSetDescriptorTable(D3D12ResourceSet set, D3D12DescriptorTableBindingInfo tableInfo, bool compute) {
        if (!tableInfo.HasTable) {
            return;
        }

        GpuDescriptorHandle gpuHandle;
        if (!set.TryGetCachedDescriptorTableHandle(tableInfo, this._descriptorHeapState.CacheId, out gpuHandle)) {
            long descriptorCopyStartTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
            uint copiedDescriptors = this._descriptorHeapState.GetOrCreateDescriptorTable(set, tableInfo, out gpuHandle);
            if (D3D12CommandListPerfTracker.Enabled && copiedDescriptors > 0) {
                this._perf.DescriptorCopyMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - descriptorCopyStartTicks);
                this._perf.DescriptorCopies += copiedDescriptors;
            }
        }

        if (compute) {
            if (!this._rootBindingCache.TryUpdateComputeRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr)) {
                return;
            }

            this.BindDescriptorHeaps();
            this._commandList.SetComputeRootDescriptorTableNoAlloc(tableInfo.RootParameterIndex, gpuHandle);
        }
        else {
            if (!this._rootBindingCache.TryUpdateGraphicsRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr)) {
                return;
            }

            this.BindDescriptorHeaps();
            this._commandList.SetGraphicsRootDescriptorTableNoAlloc(tableInfo.RootParameterIndex, gpuHandle);
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
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <returns><see langword="true" /> when all table textures are known to be in the required state.</returns>
    private static bool CanSkipDescriptorTableResourcePreparation(D3D12ResourceSet set, D3D12DescriptorTableBindingInfo tableInfo, bool compute) {
        if (tableInfo.TextureTransitionEntries.Length == 0) {
            return true;
        }

        ulong stateVersionHash = ComputeDescriptorTableStateVersionHash(set, tableInfo, compute, out uint textureCount);
        ref D3D12DescriptorTableTransitionCache cache = ref GetSrvUavTransitionCache(set, compute);
        return cache.Matches(tableInfo.Signature, stateVersionHash, textureCount);
    }

    /// <summary>
    /// Stores descriptor-table texture preparation state for later resource-set binds.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private static void StoreDescriptorTableResourcePreparation(D3D12ResourceSet set, D3D12DescriptorTableBindingInfo tableInfo, bool compute) {
        if (tableInfo.TextureTransitionEntries.Length == 0) {
            return;
        }

        ulong stateVersionHash = ComputeDescriptorTableStateVersionHash(set, tableInfo, compute, out uint textureCount);
        ref D3D12DescriptorTableTransitionCache cache = ref GetSrvUavTransitionCache(set, compute);
        cache.Store(tableInfo.Signature, stateVersionHash, textureCount);
    }

    /// <summary>
    /// Computes a stable hash of the texture state versions required by one descriptor table.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <param name="textureCount">The number of texture descriptors included in the hash.</param>
    /// <returns>The combined state-version hash.</returns>
    private static ulong ComputeDescriptorTableStateVersionHash(D3D12ResourceSet set, D3D12DescriptorTableBindingInfo tableInfo, bool compute, out uint textureCount) {
        const ulong fnvOffsetBasis = 14695981039346656037ul;
        const ulong fnvPrime = 1099511628211ul;
        ulong hash = fnvOffsetBasis;
        textureCount = 0;

        D3D12DescriptorTableTextureTransitionEntry[] entries = tableInfo.TextureTransitionEntries;
        for (int i = 0; i < entries.Length; i++) {
            D3D12DescriptorTableTextureTransitionEntry entry = entries[i];
            ResourceStates requiredState = entry.Kind == ResourceKind.TextureReadOnly
                ? GetDescriptorTableTextureReadState(compute)
                : ResourceStates.UnorderedAccess;

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
