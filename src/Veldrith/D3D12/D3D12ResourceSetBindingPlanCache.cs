using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Veldrith.D3D12;

/// <summary>
/// Caches D3D12 resource-set binding plans by pipeline, layout, and set slot.
/// </summary>
internal sealed class D3D12ResourceSetBindingPlanCache {

    /// <summary>
    /// Stores compute binding plans keyed by pipeline, layout, and resource-set slot.
    /// </summary>
    private readonly Dictionary<ResourceSetBindingPlanKey, D3D12ResourceSetBindingPlan> _computePlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Stores graphics binding plans keyed by pipeline, layout, and resource-set slot.
    /// </summary>
    private readonly Dictionary<ResourceSetBindingPlanKey, D3D12ResourceSetBindingPlan> _graphicsPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Stores a fast per-slot compute cache for the last compute pipeline.
    /// </summary>
    private D3D12ResourceSetBindingPlan[] _computeSlotCache = new D3D12ResourceSetBindingPlan[8];

    /// <summary>
    /// Stores validity generations for compute slot-cache entries.
    /// </summary>
    private uint[] _computeSlotCacheGenerations = new uint[8];

    /// <summary>
    /// Stores the compute pipeline that owns the current per-slot compute cache.
    /// </summary>
    private D3D12Pipeline _computeSlotCachePipeline;

    /// <summary>
    /// Stores the active compute slot-cache generation.
    /// </summary>
    private uint _computeSlotCacheGeneration = 1;

    /// <summary>
    /// Stores a fast per-slot graphics cache for the last graphics pipeline.
    /// </summary>
    private D3D12ResourceSetBindingPlan[] _graphicsSlotCache = new D3D12ResourceSetBindingPlan[8];

    /// <summary>
    /// Stores validity generations for graphics slot-cache entries.
    /// </summary>
    private uint[] _graphicsSlotCacheGenerations = new uint[8];

    /// <summary>
    /// Stores the graphics pipeline that owns the current per-slot graphics cache.
    /// </summary>
    private D3D12Pipeline _graphicsSlotCachePipeline;

    /// <summary>
    /// Stores the active graphics slot-cache generation.
    /// </summary>
    private uint _graphicsSlotCacheGeneration = 1;

    /// <summary>
    /// Gets a graphics resource-set binding plan for a pipeline, slot, and layout.
    /// </summary>
    /// <param name="pipeline">The graphics pipeline being bound.</param>
    /// <param name="slot">The resource-set slot.</param>
    /// <param name="layout">The resource layout used by the set.</param>
    /// <returns>The cached or newly created binding plan.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal D3D12ResourceSetBindingPlan GetGraphics(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        if (ReferenceEquals(this._graphicsSlotCachePipeline, pipeline)
            && slot < (uint)this._graphicsSlotCache.Length
            && this._graphicsSlotCacheGenerations[slot] == this._graphicsSlotCacheGeneration
            && this._graphicsSlotCache[slot].Entries != null) {
            return this._graphicsSlotCache[slot];
        }

        return this.GetGraphicsSlow(pipeline, slot, layout);
    }

    /// <summary>
    /// Gets a compute resource-set binding plan for a pipeline, slot, and layout.
    /// </summary>
    /// <param name="pipeline">The compute pipeline being bound.</param>
    /// <param name="slot">The resource-set slot.</param>
    /// <param name="layout">The resource layout used by the set.</param>
    /// <returns>The cached or newly created binding plan.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal D3D12ResourceSetBindingPlan GetCompute(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        if (ReferenceEquals(this._computeSlotCachePipeline, pipeline)
            && slot < (uint)this._computeSlotCache.Length
            && this._computeSlotCacheGenerations[slot] == this._computeSlotCacheGeneration
            && this._computeSlotCache[slot].Entries != null) {
            return this._computeSlotCache[slot];
        }

        return this.GetComputeSlow(pipeline, slot, layout);
    }

    /// <summary>
    /// Clears the fast per-pipeline slot caches after command-list pipeline state is reset.
    /// </summary>
    internal void ClearPipelineCaches() {
        this._graphicsSlotCachePipeline = null;
        this._computeSlotCachePipeline = null;
    }

    /// <summary>
    /// Gets or creates a graphics binding plan through the dictionary-backed slow path.
    /// </summary>
    /// <param name="pipeline">The graphics pipeline being bound.</param>
    /// <param name="slot">The resource-set slot.</param>
    /// <param name="layout">The resource layout used by the set.</param>
    /// <returns>The cached or newly created binding plan.</returns>
    private D3D12ResourceSetBindingPlan GetGraphicsSlow(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        ResourceSetBindingPlanKey key = new(pipeline, layout, slot);
        if (!this._graphicsPlans.TryGetValue(key, out D3D12ResourceSetBindingPlan existingPlan)) {
            existingPlan = CreateGraphicsPlan(pipeline, slot, layout.Elements);
            this._graphicsPlans.Add(key, existingPlan);
        }

        if (!ReferenceEquals(this._graphicsSlotCachePipeline, pipeline)) {
            this._graphicsSlotCachePipeline = pipeline;
            this.AdvanceGraphicsSlotCacheGeneration();
        }

        Util.EnsureArrayMinimumSize(ref this._graphicsSlotCache, slot + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsSlotCacheGenerations, slot + 1);
        this._graphicsSlotCache[slot] = existingPlan;
        this._graphicsSlotCacheGenerations[slot] = this._graphicsSlotCacheGeneration;
        return existingPlan;
    }

    /// <summary>
    /// Gets or creates a compute binding plan through the dictionary-backed slow path.
    /// </summary>
    /// <param name="pipeline">The compute pipeline being bound.</param>
    /// <param name="slot">The resource-set slot.</param>
    /// <param name="layout">The resource layout used by the set.</param>
    /// <returns>The cached or newly created binding plan.</returns>
    private D3D12ResourceSetBindingPlan GetComputeSlow(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        ResourceSetBindingPlanKey key = new(pipeline, layout, slot);
        if (!this._computePlans.TryGetValue(key, out D3D12ResourceSetBindingPlan existingPlan)) {
            existingPlan = CreateComputePlan(pipeline, slot, layout.Elements);
            this._computePlans.Add(key, existingPlan);
        }

        if (!ReferenceEquals(this._computeSlotCachePipeline, pipeline)) {
            this._computeSlotCachePipeline = pipeline;
            this.AdvanceComputeSlotCacheGeneration();
        }

        Util.EnsureArrayMinimumSize(ref this._computeSlotCache, slot + 1);
        Util.EnsureArrayMinimumSize(ref this._computeSlotCacheGenerations, slot + 1);
        this._computeSlotCache[slot] = existingPlan;
        this._computeSlotCacheGenerations[slot] = this._computeSlotCacheGeneration;
        return existingPlan;
    }

    /// <summary>
    /// Invalidates graphics slot-cache entries without clearing the plan array.
    /// </summary>
    private void AdvanceGraphicsSlotCacheGeneration() {
        this._graphicsSlotCacheGeneration++;
        if (this._graphicsSlotCacheGeneration != 0) {
            return;
        }

        System.Array.Clear(this._graphicsSlotCacheGenerations, 0, this._graphicsSlotCacheGenerations.Length);
        this._graphicsSlotCacheGeneration = 1;
    }

    /// <summary>
    /// Invalidates compute slot-cache entries without clearing the plan array.
    /// </summary>
    private void AdvanceComputeSlotCacheGeneration() {
        this._computeSlotCacheGeneration++;
        if (this._computeSlotCacheGeneration != 0) {
            return;
        }

        System.Array.Clear(this._computeSlotCacheGenerations, 0, this._computeSlotCacheGenerations.Length);
        this._computeSlotCacheGeneration = 1;
    }

    /// <summary>
    /// Builds a graphics binding plan from pipeline root bindings and resource-layout elements.
    /// </summary>
    /// <param name="pipeline">The graphics pipeline that owns root binding metadata.</param>
    /// <param name="slot">The resource-set slot.</param>
    /// <param name="elements">The layout elements in the resource set.</param>
    /// <returns>The created binding plan.</returns>
    private static D3D12ResourceSetBindingPlan CreateGraphicsPlan(D3D12Pipeline pipeline, uint slot, ResourceLayoutElementDescription[] elements) {
        List<D3D12ResourceSetBindingPlanEntry> plan = new(elements.Length);
        for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
            if (!pipeline.TryGetGraphicsRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo)) {
                continue;
            }

            bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
            plan.Add(new D3D12ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
        }

        return new D3D12ResourceSetBindingPlan(plan.ToArray());
    }

    /// <summary>
    /// Builds a compute binding plan from pipeline root bindings and resource-layout elements.
    /// </summary>
    /// <param name="pipeline">The compute pipeline that owns root binding metadata.</param>
    /// <param name="slot">The resource-set slot.</param>
    /// <param name="elements">The layout elements in the resource set.</param>
    /// <returns>The created binding plan.</returns>
    private static D3D12ResourceSetBindingPlan CreateComputePlan(D3D12Pipeline pipeline, uint slot, ResourceLayoutElementDescription[] elements) {
        List<D3D12ResourceSetBindingPlanEntry> plan = new(elements.Length);
        for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
            if (!pipeline.TryGetComputeRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo)) {
                continue;
            }

            bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
            plan.Add(new D3D12ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
        }

        return new D3D12ResourceSetBindingPlan(plan.ToArray());
    }

    /// <summary>
    /// Identifies a resource-set binding plan by pipeline, layout, and slot.
    /// </summary>
    private readonly struct ResourceSetBindingPlanKey {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlanKey" /> struct.
        /// </summary>
        /// <param name="pipeline">The pipeline used by the key.</param>
        /// <param name="layout">The resource layout used by the key.</param>
        /// <param name="slot">The resource-set slot used by the key.</param>
        public ResourceSetBindingPlanKey(D3D12Pipeline pipeline, D3D12ResourceLayout layout, uint slot) {
            this.Pipeline = pipeline;
            this.Layout = layout;
            this.Slot = slot;
        }

        /// <summary>
        /// Gets the pipeline used by the key.
        /// </summary>
        public D3D12Pipeline Pipeline { get; }

        /// <summary>
        /// Gets the resource layout used by the key.
        /// </summary>
        public D3D12ResourceLayout Layout { get; }

        /// <summary>
        /// Gets the resource-set slot used by the key.
        /// </summary>
        public uint Slot { get; }
    }

    /// <summary>
    /// Compares resource-set binding plan keys by object identity and set slot.
    /// </summary>
    private sealed class ResourceSetBindingPlanKeyComparer : IEqualityComparer<ResourceSetBindingPlanKey> {

        /// <summary>
        /// Stores the shared comparer instance.
        /// </summary>
        public static readonly ResourceSetBindingPlanKeyComparer Instance = new();

        /// <summary>
        /// Determines whether two binding-plan keys reference the same pipeline, layout, and slot.
        /// </summary>
        /// <param name="x">The first key.</param>
        /// <param name="y">The second key.</param>
        /// <returns><see langword="true" /> when the keys identify the same binding plan.</returns>
        public bool Equals(ResourceSetBindingPlanKey x, ResourceSetBindingPlanKey y) {
            return x.Slot == y.Slot && ReferenceEquals(x.Pipeline, y.Pipeline) && ReferenceEquals(x.Layout, y.Layout);
        }

        /// <summary>
        /// Gets a hash code for a binding-plan key.
        /// </summary>
        /// <param name="obj">The key to hash.</param>
        /// <returns>The key hash code.</returns>
        public int GetHashCode(ResourceSetBindingPlanKey obj) {
            unchecked {
                int hash = RuntimeHelpers.GetHashCode(obj.Pipeline);
                hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(obj.Layout);
                hash = (hash * 397) ^ (int)obj.Slot;
                return hash;
            }
        }
    }
}

/// <summary>
/// Represents a cached resource-set binding plan and its descriptor-table metadata.
/// </summary>
internal readonly struct D3D12ResourceSetBindingPlan {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceSetBindingPlan" /> struct.
    /// </summary>
    /// <param name="entries">The root binding entries used by this plan.</param>
    public D3D12ResourceSetBindingPlan(D3D12ResourceSetBindingPlanEntry[] entries) {
        this.Entries = entries;
        this.SrvUavTable = CreateDescriptorTableBindingInfo(entries, D3D12Pipeline.DescriptorTableKind.SrvUav);
        this.SamplerTable = CreateDescriptorTableBindingInfo(entries, D3D12Pipeline.DescriptorTableKind.Sampler);
        this.SingleRootBindingOnly = entries.Length == 1 && !entries[0].BindingInfo.DescriptorTable;
        this.SingleRootBinding = this.SingleRootBindingOnly ? entries[0] : default;
        this.SingleUniformRootBindingOnly = this.SingleRootBindingOnly && entries[0].BindingInfo.Kind == ResourceKind.UniformBuffer;
        this.DescriptorTablesOnly = entries.Length != 0;
        for (int i = 0; i < entries.Length; i++) {
            if (!entries[i].BindingInfo.DescriptorTable) {
                this.DescriptorTablesOnly = false;
                break;
            }
        }
    }

    /// <summary>
    /// Gets the root binding entries used by this plan.
    /// </summary>
    public D3D12ResourceSetBindingPlanEntry[] Entries { get; }

    /// <summary>
    /// Gets metadata for the SRV/UAV descriptor table used by this plan.
    /// </summary>
    public D3D12DescriptorTableBindingInfo SrvUavTable { get; }

    /// <summary>
    /// Gets metadata for the sampler descriptor table used by this plan.
    /// </summary>
    public D3D12DescriptorTableBindingInfo SamplerTable { get; }

    /// <summary>
    /// Gets whether this plan contains exactly one root buffer binding and no descriptor tables.
    /// </summary>
    public bool SingleRootBindingOnly { get; }

    /// <summary>
    /// Gets the single root buffer binding when <see cref="SingleRootBindingOnly"/> is true.
    /// </summary>
    public D3D12ResourceSetBindingPlanEntry SingleRootBinding { get; }

    /// <summary>
    /// Gets whether this plan contains exactly one uniform-buffer root binding.
    /// </summary>
    public bool SingleUniformRootBindingOnly { get; }

    /// <summary>
    /// Gets whether this plan contains descriptor-table bindings and no root-buffer bindings.
    /// </summary>
    public bool DescriptorTablesOnly { get; }

    /// <summary>
    /// Creates descriptor-table metadata for one descriptor table kind.
    /// </summary>
    /// <param name="bindingPlan">The binding plan entries to scan.</param>
    /// <param name="tableKind">The descriptor table kind to describe.</param>
    /// <returns>The descriptor-table metadata.</returns>
    private static D3D12DescriptorTableBindingInfo CreateDescriptorTableBindingInfo(D3D12ResourceSetBindingPlanEntry[] bindingPlan, D3D12Pipeline.DescriptorTableKind tableKind) {
        uint descriptorCount = 0;
        uint rootParameterIndex = 0;
        uint tableEntryCount = 0;

        for (int i = 0; i < bindingPlan.Length; i++) {
            D3D12Pipeline.RootBindingInfo bindingInfo = bindingPlan[i].BindingInfo;
            if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableKind) {
                continue;
            }

            tableEntryCount++;
            rootParameterIndex = bindingInfo.RootParameterIndex;
            descriptorCount = System.Math.Max(descriptorCount, bindingInfo.DescriptorTableOffset + 1);
        }

        if (tableEntryCount == 0) {
            return default;
        }

        D3D12DescriptorTableBindingEntry[] tableEntries = new D3D12DescriptorTableBindingEntry[tableEntryCount];
        uint tableEntryIndex = 0;
        for (int i = 0; i < bindingPlan.Length; i++) {
            D3D12Pipeline.RootBindingInfo bindingInfo = bindingPlan[i].BindingInfo;
            if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableKind) {
                continue;
            }

            tableEntries[tableEntryIndex++] = new D3D12DescriptorTableBindingEntry(bindingPlan[i].ElementIndex, bindingInfo.Kind, bindingInfo.DescriptorTableOffset);
        }

        uint hash = ComputeDescriptorTableSignature(tableEntries, descriptorCount);
        return new D3D12DescriptorTableBindingInfo(tableKind, descriptorCount, rootParameterIndex, hash, tableEntries);
    }

    /// <summary>
    /// Creates descriptor-table metadata directly from a resource layout.
    /// </summary>
    /// <param name="elements">The resource-layout elements.</param>
    /// <param name="tableKind">The descriptor table kind to describe.</param>
    /// <returns>The descriptor-table metadata.</returns>
    internal static D3D12DescriptorTableBindingInfo CreateLayoutDescriptorTableBindingInfo(ResourceLayoutElementDescription[] elements, D3D12Pipeline.DescriptorTableKind tableKind) {
        uint tableEntryCount = 0;
        for (uint elementIndex = 0; elementIndex < (uint)elements.Length; elementIndex++) {
            if (GetDescriptorTableKind(elements[elementIndex].Kind) == tableKind) {
                tableEntryCount++;
            }
        }

        if (tableEntryCount == 0) {
            return default;
        }

        D3D12DescriptorTableBindingEntry[] tableEntries = new D3D12DescriptorTableBindingEntry[tableEntryCount];
        uint tableEntryIndex = 0;
        for (uint elementIndex = 0; elementIndex < (uint)elements.Length; elementIndex++) {
            ResourceKind kind = elements[elementIndex].Kind;
            if (GetDescriptorTableKind(kind) != tableKind) {
                continue;
            }

            tableEntries[tableEntryIndex] = new D3D12DescriptorTableBindingEntry(elementIndex, kind, tableEntryIndex);
            tableEntryIndex++;
        }

        uint hash = ComputeDescriptorTableSignature(tableEntries, tableEntryCount);
        return new D3D12DescriptorTableBindingInfo(tableKind, tableEntryCount, 0, hash, tableEntries);
    }

    /// <summary>
    /// Computes the descriptor-table cache signature from table entries.
    /// </summary>
    /// <param name="entries">The descriptor-table entries.</param>
    /// <param name="descriptorCount">The number of descriptors in the table.</param>
    /// <returns>The descriptor-table signature.</returns>
    private static uint ComputeDescriptorTableSignature(D3D12DescriptorTableBindingEntry[] entries, uint descriptorCount) {
        uint hash = 2166136261u;
        for (int i = 0; i < entries.Length; i++) {
            D3D12DescriptorTableBindingEntry entry = entries[i];
            hash = (hash ^ entry.ElementIndex) * 16777619u;
            hash = (hash ^ entry.DescriptorTableOffset) * 16777619u;
            hash = (hash ^ (uint)entry.Kind) * 16777619u;
        }

        hash = (hash ^ descriptorCount) * 16777619u;
        return hash;
    }

    /// <summary>
    /// Gets the descriptor-table kind used by a resource kind, or None for root-buffer resources.
    /// </summary>
    /// <param name="kind">The resource kind.</param>
    /// <returns>The descriptor-table kind.</returns>
    private static D3D12Pipeline.DescriptorTableKind GetDescriptorTableKind(ResourceKind kind) {
        return kind switch {
            ResourceKind.TextureReadOnly or ResourceKind.TextureReadWrite => D3D12Pipeline.DescriptorTableKind.SrvUav,
            ResourceKind.Sampler => D3D12Pipeline.DescriptorTableKind.Sampler,
            _ => D3D12Pipeline.DescriptorTableKind.None
        };
    }
}

/// <summary>
/// Represents one descriptor-table entry in a cached D3D12 resource-set binding plan.
/// </summary>
internal readonly struct D3D12DescriptorTableBindingEntry {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DescriptorTableBindingEntry" /> struct.
    /// </summary>
    /// <param name="elementIndex">The resource-set element index.</param>
    /// <param name="kind">The resource kind.</param>
    /// <param name="descriptorTableOffset">The descriptor offset inside the table.</param>
    public D3D12DescriptorTableBindingEntry(uint elementIndex, ResourceKind kind, uint descriptorTableOffset) {
        this.ElementIndex = elementIndex;
        this.Kind = kind;
        this.DescriptorTableOffset = descriptorTableOffset;
    }

    /// <summary>
    /// Gets the resource-set element index.
    /// </summary>
    public uint ElementIndex { get; }

    /// <summary>
    /// Gets the resource kind.
    /// </summary>
    public ResourceKind Kind { get; }

    /// <summary>
    /// Gets the descriptor offset inside the table.
    /// </summary>
    public uint DescriptorTableOffset { get; }
}

/// <summary>
/// Represents cached metadata for one descriptor table in a resource-set binding plan.
/// </summary>
internal readonly struct D3D12DescriptorTableBindingInfo {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DescriptorTableBindingInfo" /> struct.
    /// </summary>
    /// <param name="tableKind">The descriptor table kind.</param>
    /// <param name="descriptorCount">The number of descriptors in the table.</param>
    /// <param name="rootParameterIndex">The root parameter index used by the table.</param>
    /// <param name="signature">The descriptor table signature used for cache validation.</param>
    /// <param name="entries">The descriptor entries included in this table.</param>
    public D3D12DescriptorTableBindingInfo(D3D12Pipeline.DescriptorTableKind tableKind, uint descriptorCount, uint rootParameterIndex, uint signature, D3D12DescriptorTableBindingEntry[] entries) {
        this.HasTable = true;
        this.TableKind = tableKind;
        this.DescriptorCount = descriptorCount;
        this.RootParameterIndex = rootParameterIndex;
        this.Signature = signature;
        this.Entries = entries;
    }

    /// <summary>
    /// Gets whether this value describes a descriptor table.
    /// </summary>
    public bool HasTable { get; }

    /// <summary>
    /// Gets the descriptor table kind.
    /// </summary>
    public D3D12Pipeline.DescriptorTableKind TableKind { get; }

    /// <summary>
    /// Gets the number of descriptors in the descriptor table.
    /// </summary>
    public uint DescriptorCount { get; }

    /// <summary>
    /// Gets the root parameter index that receives the descriptor table.
    /// </summary>
    public uint RootParameterIndex { get; }

    /// <summary>
    /// Gets the descriptor table signature used for cache validation.
    /// </summary>
    public uint Signature { get; }

    /// <summary>
    /// Gets the descriptor entries included in this table.
    /// </summary>
    public D3D12DescriptorTableBindingEntry[] Entries { get; }
}

/// <summary>
/// Represents one root binding used by a cached D3D12 resource-set binding plan.
/// </summary>
internal readonly struct D3D12ResourceSetBindingPlanEntry {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceSetBindingPlanEntry" /> struct.
    /// </summary>
    /// <param name="elementIndex">The resource-layout element index.</param>
    /// <param name="bindingInfo">The D3D12 root binding metadata.</param>
    /// <param name="isDynamicBinding">Whether the binding consumes a dynamic offset.</param>
    public D3D12ResourceSetBindingPlanEntry(uint elementIndex, D3D12Pipeline.RootBindingInfo bindingInfo, bool isDynamicBinding) {
        this.ElementIndex = elementIndex;
        this.BindingInfo = bindingInfo;
        this.IsDynamicBinding = isDynamicBinding;
    }

    /// <summary>
    /// Gets the resource-layout element index.
    /// </summary>
    public uint ElementIndex { get; }

    /// <summary>
    /// Gets the D3D12 root binding metadata.
    /// </summary>
    public D3D12Pipeline.RootBindingInfo BindingInfo { get; }

    /// <summary>
    /// Gets whether the binding consumes a dynamic offset.
    /// </summary>
    public bool IsDynamicBinding { get; }
}
