using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlSwapchain class.
/// </summary>
internal class MtlSwapchain : Swapchain {

    /// <summary>
    /// Represents the _framebuffer field.
    /// </summary>
    private readonly MtlSwapchainFramebuffer _framebuffer;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Represents the _drawable field.
    /// </summary>
    private CAMetalDrawable _drawable;

    /// <summary>
    /// Represents the _metalLayer field.
    /// </summary>
    private CAMetalLayer _metalLayer;

    /// <summary>
    /// Represents the _syncToVerticalBlank field.
    /// </summary>
    private bool _syncToVerticalBlank;

    /// <summary>
    /// Represents the _uiView field.
    /// </summary>
    private UIView _uiView; // Valid only when a UIViewSwapchainSource is used.

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlSwapchain" /> type.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    /// <param name="description">The value of description.</param>
    public MtlSwapchain(MtlGraphicsDevice gd, ref SwapchainDescription description) {
        this.gd = gd;
        this._syncToVerticalBlank = description.SyncToVerticalBlank;

        uint width;
        uint height;

        SwapchainSource source = description.Source;

        if (source is NSWindowSwapchainSource nsWindowSource) {
            NSWindow nswindow = new(nsWindowSource.NSWindow);
            NSView contentView = nswindow.contentView;
            CGSize windowContentSize = contentView.frame.size;
            width = (uint)windowContentSize.width;
            height = (uint)windowContentSize.height;

            if (!CAMetalLayer.TryCast(contentView.layer, out this._metalLayer)) {
                this._metalLayer = CAMetalLayer.New();
                contentView.wantsLayer = true;
                contentView.layer = this._metalLayer.NativePtr;
            }
        }
        else if (source is NSViewSwapchainSource nsViewSource) {
            NSView contentView = new(nsViewSource.NSView);
            CGSize windowContentSize = contentView.frame.size;
            width = (uint)windowContentSize.width;
            height = (uint)windowContentSize.height;

            if (!CAMetalLayer.TryCast(contentView.layer, out this._metalLayer)) {
                this._metalLayer = CAMetalLayer.New();
                contentView.wantsLayer = true;
                contentView.layer = this._metalLayer.NativePtr;
            }
        }
        else if (source is UIViewSwapchainSource uiViewSource) {
            this._uiView = new UIView(uiViewSource.UIView);
            CGSize viewSize = this._uiView.frame.size;
            width = (uint)viewSize.width;
            height = (uint)viewSize.height;

            if (!CAMetalLayer.TryCast(this._uiView.layer, out this._metalLayer)) {
                this._metalLayer = CAMetalLayer.New();
                this._metalLayer.frame = this._uiView.frame;
                this._metalLayer.opaque = true;
                this._uiView.layer.addSublayer(this._metalLayer.NativePtr);
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
    /// Represents the CurrentDrawable field.
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
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (this._drawable.NativePtr != IntPtr.Zero) {
            ObjectiveCRuntime.release(this._drawable.NativePtr);
        }

        this._framebuffer.Dispose();
        ObjectiveCRuntime.release(this._metalLayer.NativePtr);

        this._disposed = true;
    }

    #endregion

    /// <summary>
    /// Performs the Resize operation.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    public override void Resize(uint width, uint height) {
        if (this._uiView.NativePtr != IntPtr.Zero) {
            this._metalLayer.frame = this._uiView.frame;
        }

        this._metalLayer.drawableSize = new CGSize(width, height);

        this.GetNextDrawable();
    }

    /// <summary>
    /// Performs the EnsureDrawableAvailable operation.
    /// </summary>
    /// <returns>The result of the EnsureDrawableAvailable operation.</returns>
    public bool EnsureDrawableAvailable() {
        return !this._drawable.IsNull || this.GetNextDrawable();
    }

    /// <summary>
    /// Performs the InvalidateDrawable operation.
    /// </summary>
    public void InvalidateDrawable() {
        ObjectiveCRuntime.release(this._drawable.NativePtr);
        this._drawable = default;
    }

    /// <summary>
    /// Performs the GetNextDrawable operation.
    /// </summary>
    /// <returns>The result of the GetNextDrawable operation.</returns>
    private bool GetNextDrawable() {
        if (!this._drawable.IsNull) {
            ObjectiveCRuntime.release(this._drawable.NativePtr);
        }

        using (NSAutoreleasePool.Begin()) {
            this._drawable = this._metalLayer.nextDrawable();

            if (!this._drawable.IsNull) {
                ObjectiveCRuntime.retain(this._drawable.NativePtr);
                this._framebuffer.UpdateTextures(this._drawable, this._metalLayer.drawableSize);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Performs the SetSyncToVerticalBlank operation.
    /// </summary>
    /// <param name="value">The value of value.</param>
    private void SetSyncToVerticalBlank(bool value) {
        this._syncToVerticalBlank = value;

        if (this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v3
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v4
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily2_v1) {
            this._metalLayer.displaySyncEnabled = value;
        }
    }
}