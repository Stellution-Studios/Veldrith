using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlSwapchain.
/// </summary>
internal class MtlSwapchain : Swapchain {

    /// <summary>
    /// Stores the framebuffer state used by this instance.
    /// </summary>
    private readonly MtlSwapchainFramebuffer _framebuffer;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the drawable state used by this instance.
    /// </summary>
    private CAMetalDrawable _drawable;

    /// <summary>
    /// Stores the metal layer state used by this instance.
    /// </summary>
    private CAMetalLayer _metalLayer;

    /// <summary>
    /// Stores the sync to vertical blank state used by this instance.
    /// </summary>
    private bool _syncToVerticalBlank;

    /// <summary>
    /// Stores the ui view state used by this instance.
    /// </summary>
    private UIView _uiView; // Valid only when a UIViewSwapchainSource is used.

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlSwapchain" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public MtlSwapchain(MtlGraphicsDevice gd, ref SwapchainDescription description) {
        this.gd = gd;
        this._syncToVerticalBlank = description.SyncToVerticalBlank;

        uint width;
        uint height;

        SwapchainSource source = description.Source;

        if (source is NSWindowSwapchainSource nsWindowSource) {
            NSWindow nswindow = new(nsWindowSource.NSWindow);
            NSView contentView = nswindow.ContentView;
            CGSize windowContentSize = contentView.Frame.size;
            width = (uint)windowContentSize.width;
            height = (uint)windowContentSize.height;

            if (!CAMetalLayer.TryCast(contentView.Layer, out this._metalLayer)) {
                this._metalLayer = CAMetalLayer.New();
                contentView.WantsLayer = true;
                contentView.Layer = this._metalLayer.NativePtr;
            }
        }
        else if (source is NSViewSwapchainSource nsViewSource) {
            NSView contentView = new(nsViewSource.NSView);
            CGSize windowContentSize = contentView.Frame.size;
            width = (uint)windowContentSize.width;
            height = (uint)windowContentSize.height;

            if (!CAMetalLayer.TryCast(contentView.Layer, out this._metalLayer)) {
                this._metalLayer = CAMetalLayer.New();
                contentView.WantsLayer = true;
                contentView.Layer = this._metalLayer.NativePtr;
            }
        }
        else if (source is UIViewSwapchainSource uiViewSource) {
            this._uiView = new UIView(uiViewSource.UIView);
            CGSize viewSize = this._uiView.Frame.size;
            width = (uint)viewSize.width;
            height = (uint)viewSize.height;

            if (!CAMetalLayer.TryCast(this._uiView.Layer, out this._metalLayer)) {
                this._metalLayer = CAMetalLayer.New();
                this._metalLayer.frame = this._uiView.Frame;
                this._metalLayer.opaque = true;
            this._uiView.Layer.AddSublayer(this._metalLayer.NativePtr);
            }
        }
        else {
            throw new VeldridException("A Metal Swapchain can only be created from an NSWindow, NSView, or UIView.");
        }

        PixelFormat format = description.ColorSrgb
            ? PixelFormat.B8G8R8A8UNormSRgb
            : PixelFormat.B8G8R8A8UNorm;

        this._metalLayer.device = this.gd.Device;
        this._metalLayer.pixelFormat = MtlFormats.VdToMtlPixelFormat(format, false);
        this._metalLayer.framebufferOnly = true;
        this._metalLayer.drawableSize = new CGSize(width, height);

        this.SetSyncToVerticalBlank(this._syncToVerticalBlank);

        this._framebuffer = new MtlSwapchainFramebuffer(gd, this, description.DepthFormat, format);

        this.GetNextDrawable();
    }

    /// <summary>
    /// Gets or sets Framebuffer.
    /// </summary>
    public override Framebuffer Framebuffer => this._framebuffer;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Stores the current drawable state used by this instance.
    /// </summary>
    public CAMetalDrawable CurrentDrawable => this._drawable;

    /// <summary>
    /// Gets or sets SyncToVerticalBlank.
    /// </summary>
    public override bool SyncToVerticalBlank {
        get => this._syncToVerticalBlank;
        set {
            if (this._syncToVerticalBlank != value) {
                this.SetSyncToVerticalBlank(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._drawable.NativePtr != IntPtr.Zero) {
            ObjectiveCRuntime.Release(this._drawable.NativePtr);
        }

        this._framebuffer.Dispose();
        ObjectiveCRuntime.Release(this._metalLayer.NativePtr);

        this._disposed = true;
    }

    #endregion

    /// <summary>
    /// Executes the resize logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public override void Resize(uint width, uint height) {
        if (this._uiView.NativePtr != IntPtr.Zero) {
            this._metalLayer.frame = this._uiView.Frame;
        }

        this._metalLayer.drawableSize = new CGSize(width, height);

        this.GetNextDrawable();
    }

    /// <summary>
    /// Executes the ensure drawable available logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool EnsureDrawableAvailable() {
        return !this._drawable.IsNull || this.GetNextDrawable();
    }

    /// <summary>
    /// Executes the invalidate drawable logic for this backend.
    /// </summary>
    public void InvalidateDrawable() {
        ObjectiveCRuntime.Release(this._drawable.NativePtr);
        this._drawable = default;
    }

    /// <summary>
    /// Gets the next drawable value.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool GetNextDrawable() {
        if (!this._drawable.IsNull) {
            ObjectiveCRuntime.Release(this._drawable.NativePtr);
        }

        using (NSAutoreleasePool.Begin()) {
            this._drawable = this._metalLayer.NextDrawable();

            if (!this._drawable.IsNull) {
                ObjectiveCRuntime.Retain(this._drawable.NativePtr);
                this._framebuffer.UpdateTextures(this._drawable, this._metalLayer.drawableSize);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Sets the sync to vertical blank value.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    private void SetSyncToVerticalBlank(bool value) {
        this._syncToVerticalBlank = value;

        if (this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v3
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v4
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily2_v1) {
            this._metalLayer.displaySyncEnabled = value;
        }
    }
}
