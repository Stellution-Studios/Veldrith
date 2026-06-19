using System.Diagnostics;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Owns D3D12 graphics and compute pipeline binding state for a command list.
/// </summary>
internal sealed class D3D12PipelineStateBinder {

    /// <summary>
    /// Stores the command list that receives native pipeline and root-signature commands.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Tracks currently bound graphics resource sets and dirty slots.
    /// </summary>
    private readonly D3D12BoundResourceSetState _graphicsResourceSets;

    /// <summary>
    /// Tracks currently bound compute resource sets and dirty slots.
    /// </summary>
    private readonly D3D12BoundResourceSetState _computeResourceSets;

    /// <summary>
    /// Tracks root descriptor state that must be invalidated when root signatures change.
    /// </summary>
    private readonly D3D12RootBindingCache _rootBindingCache;

    /// <summary>
    /// Stores optional performance counters updated while pipeline state is changed.
    /// </summary>
    private readonly D3D12CommandListPerfTracker _perf;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12PipelineStateBinder" /> class.
    /// </summary>
    /// <param name="commandList">The command list that receives D3D12 binding commands.</param>
    /// <param name="graphicsResourceSets">The graphics resource-set state owned by the command list.</param>
    /// <param name="computeResourceSets">The compute resource-set state owned by the command list.</param>
    /// <param name="rootBindingCache">The root binding cache owned by the command list.</param>
    /// <param name="perf">The optional performance tracker updated by the binder.</param>
    internal D3D12PipelineStateBinder(D3D12CommandList commandList, D3D12BoundResourceSetState graphicsResourceSets, D3D12BoundResourceSetState computeResourceSets, D3D12RootBindingCache rootBindingCache, D3D12CommandListPerfTracker perf) {
        this._commandList = commandList;
        this._graphicsResourceSets = graphicsResourceSets;
        this._computeResourceSets = computeResourceSets;
        this._rootBindingCache = rootBindingCache;
        this._perf = perf;
    }

    /// <summary>
    /// Gets or sets the currently recorded graphics pipeline.
    /// </summary>
    internal D3D12Pipeline CurrentGraphicsPipeline { get; set; }

    /// <summary>
    /// Gets or sets the currently recorded compute pipeline.
    /// </summary>
    internal D3D12Pipeline CurrentComputePipeline { get; set; }

    /// <summary>
    /// Clears current pipeline state for a new command-list recording.
    /// </summary>
    internal void BeginRecording() {
        this.CurrentGraphicsPipeline = null;
        this.CurrentComputePipeline = null;
    }

    /// <summary>
    /// Binds a graphics or compute pipeline if it differs from the current D3D12 state.
    /// </summary>
    /// <param name="pipeline">The pipeline to bind.</param>
    internal void SetPipeline(Pipeline pipeline) {
        if (pipeline.IsComputePipeline) {
            this.SetComputePipeline(Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline));
            return;
        }

        this.SetGraphicsPipeline(Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline));
    }

    /// <summary>
    /// Binds a compute pipeline and updates compute root-signature state when required.
    /// </summary>
    /// <param name="pipeline">The compute pipeline to bind.</param>
    internal void SetComputePipeline(D3D12Pipeline pipeline) {
        if (ReferenceEquals(this.CurrentComputePipeline, pipeline)) {
            return;
        }

        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this._computeResourceSets.EnsureCapacity(pipeline.ResourceSetCount);
        bool rootSignatureChanged = !ReferenceEquals(this.CurrentComputePipeline?.RootSignature, pipeline.RootSignature);
        this.CurrentComputePipeline = pipeline;
        this.CurrentGraphicsPipeline = null;

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.PipelineChanges++;
        }

        this._commandList.SetPipelineStateNoAllocForInternalUse(pipeline.PipelineState);
        if (rootSignatureChanged) {
            this._computeResourceSets.Clear();
            this._computeResourceSets.ResetDirtyRange();
            this._rootBindingCache.InvalidateCompute();
            this._commandList.SetComputeRootSignatureNoAllocForInternalUse(pipeline.RootSignature);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.PipelineSetMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Binds a graphics pipeline and updates graphics root-signature and input-assembler state when required.
    /// </summary>
    /// <param name="pipeline">The graphics pipeline to bind.</param>
    internal void SetGraphicsPipeline(D3D12Pipeline pipeline) {
        if (ReferenceEquals(this.CurrentGraphicsPipeline, pipeline)) {
            return;
        }

        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this._graphicsResourceSets.EnsureCapacity(pipeline.ResourceSetCount);
        bool rootSignatureChanged = !ReferenceEquals(this.CurrentGraphicsPipeline?.RootSignature, pipeline.RootSignature);
        this.CurrentGraphicsPipeline = pipeline;
        this.CurrentComputePipeline = null;

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.PipelineChanges++;
        }

        this._commandList.SetPipelineStateNoAllocForInternalUse(pipeline.PipelineState);
        if (pipeline.UsesStencilReference) {
            this._commandList.SetStencilReferenceForInternalUse(pipeline.StencilReference);
        }

        if (rootSignatureChanged) {
            this._graphicsResourceSets.Clear();
            this._graphicsResourceSets.ResetDirtyRange();
            this._rootBindingCache.InvalidateGraphics();
            this._commandList.SetGraphicsRootSignatureNoAllocForInternalUse(pipeline.RootSignature);
        }

        this._commandList.SetPrimitiveTopologyForInternalUse(pipeline.PrimitiveTopology);
        this._commandList.RebindVertexBuffersForCurrentPipeline();
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.PipelineSetMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }
}
