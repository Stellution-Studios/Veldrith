using System;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Lazily creates and owns the D3D12 command signatures used for indirect execution.
/// </summary>
internal sealed class D3D12IndirectCommandSignatures : IDisposable {

    /// <summary>
    /// Stores the graphics device used to create command signatures.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores the command signature used for non-indexed indirect draws.
    /// </summary>
    private ID3D12CommandSignature _drawIndirectSignature;

    /// <summary>
    /// Stores the command signature used for indexed indirect draws.
    /// </summary>
    private ID3D12CommandSignature _drawIndexedIndirectSignature;

    /// <summary>
    /// Stores the command signature used for indirect dispatches.
    /// </summary>
    private ID3D12CommandSignature _dispatchIndirectSignature;

    /// <summary>
    /// Stores whether command signature creation has already been attempted.
    /// </summary>
    private bool _initialized;

    /// <summary>
    /// Stores whether all required command signatures are available.
    /// </summary>
    private bool _available;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12IndirectCommandSignatures" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns the command signatures.</param>
    internal D3D12IndirectCommandSignatures(D3D12GraphicsDevice gd) {
        this._gd = gd;
    }

    /// <summary>
    /// Gets the command signature used for non-indexed indirect draws.
    /// </summary>
    internal ID3D12CommandSignature Draw => this._drawIndirectSignature;

    /// <summary>
    /// Gets the command signature used for indexed indirect draws.
    /// </summary>
    internal ID3D12CommandSignature DrawIndexed => this._drawIndexedIndirectSignature;

    /// <summary>
    /// Gets the command signature used for indirect dispatches.
    /// </summary>
    internal ID3D12CommandSignature Dispatch => this._dispatchIndirectSignature;

    /// <summary>
    /// Ensures all indirect command signatures have been created.
    /// </summary>
    /// <returns><see langword="true" /> when indirect command signatures are available.</returns>
    internal bool EnsureAvailable() {
        if (this._initialized) {
            return this._available;
        }

        this._initialized = true;
        try {
            IndirectArgumentDescription drawArgument = default;
            drawArgument.Type = IndirectArgumentType.Draw;
            CommandSignatureDescription drawDescription = default;
            drawDescription.ByteStride = Unsafe.SizeOf<IndirectDrawArguments>();
            drawDescription.IndirectArguments = [drawArgument];
            this._drawIndirectSignature = this.CreateCommandSignature(drawDescription);

            IndirectArgumentDescription drawIndexedArgument = default;
            drawIndexedArgument.Type = IndirectArgumentType.DrawIndexed;
            CommandSignatureDescription drawIndexedDescription = default;
            drawIndexedDescription.ByteStride = Unsafe.SizeOf<IndirectDrawIndexedArguments>();
            drawIndexedDescription.IndirectArguments = [drawIndexedArgument];
            this._drawIndexedIndirectSignature = this.CreateCommandSignature(drawIndexedDescription);

            IndirectArgumentDescription dispatchArgument = default;
            dispatchArgument.Type = IndirectArgumentType.Dispatch;
            CommandSignatureDescription dispatchDescription = default;
            dispatchDescription.ByteStride = Unsafe.SizeOf<IndirectDispatchArguments>();
            dispatchDescription.IndirectArguments = [dispatchArgument];
            this._dispatchIndirectSignature = this.CreateCommandSignature(dispatchDescription);

            this._available = this._drawIndirectSignature != null
                              && this._drawIndexedIndirectSignature != null
                              && this._dispatchIndirectSignature != null;
        }
        catch {
            this._available = false;
        }

        return this._available;
    }

    /// <summary>
    /// Releases command signature resources held by this instance.
    /// </summary>
    public void Dispose() {
        this._drawIndirectSignature?.Dispose();
        this._drawIndexedIndirectSignature?.Dispose();
        this._dispatchIndirectSignature?.Dispose();
    }

    /// <summary>
    /// Creates one D3D12 command signature and validates the result.
    /// </summary>
    /// <param name="description">The command signature description.</param>
    /// <returns>The created command signature.</returns>
    private ID3D12CommandSignature CreateCommandSignature(CommandSignatureDescription description) {
        ID3D12CommandSignature signature = this._gd.Device.CreateCommandSignature<ID3D12CommandSignature>(description, null);

        if (signature == null) {
            throw new VeldridException("Unable to create D3D12 command signature.");
        }

        return signature;
    }
}
