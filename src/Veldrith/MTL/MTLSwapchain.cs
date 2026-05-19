using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlSwapchain class.
/// </summary>
internal class MtlSwapchain : Swapchain {

    /// <summary>
    /// Stores the value associated with <c>_framebuffer</c>.
    /// </summary>
    private readonly MtlSwapchainFramebuffer _framebuffer;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the value associated with <c>_drawable</c>.
    /// </summary>
    private CAMetalDrawable _drawable;

    /// <summary>
    /// Stores the value associated with <c>_metalLayer</c>.
    /// </summary>
    private CAMetalLayer _metalLayer;

    /// <summary>
    /// Stores the value associated with <c>_syncToVerticalBlank</c>.
    /// </summary>
    private bool _syncToVerticalBlank;

    /// <summary>
    /// Stores the value associated with <c>_uiView</c>.
    /// </summary>
    private UIView _uiView; // Valid only when a UIViewSwapchainSource is used.

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlSwapchain" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
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
    /// Stores the value associated with <c>CurrentDrawable</c>.
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
    /// Executes the Dispose operation.
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
    /// Executes the Resize operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public override void Resize(uint width, uint height) {
        if (this._uiView.NativePtr != IntPtr.Zero) {
            this._metalLayer.frame = this._uiView.frame;
        }

        this._metalLayer.drawableSize = new CGSize(width, height);

        this.GetNextDrawable();
    }

    /// <summary>
    /// Executes the EnsureDrawableAvailable operation.
    /// </summary>
    /// <returns>Returns the result produced by the EnsureDrawableAvailable operation.</returns>
    public bool EnsureDrawableAvailable() {
        return !this._drawable.IsNull || this.GetNextDrawable();
    }

    /// <summary>
    /// Executes the InvalidateDrawable operation.
    /// </summary>
    public void InvalidateDrawable() {
        ObjectiveCRuntime.release(this._drawable.NativePtr);
        this._drawable = default;
    }

    /// <summary>
    /// Executes the GetNextDrawable operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetNextDrawable operation.</returns>
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
    /// Executes the SetSyncToVerticalBlank operation.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    private void SetSyncToVerticalBlank(bool value) {
        this._syncToVerticalBlank = value;

        if (this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v3
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v4
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily2_v1) {
            this._metalLayer.displaySyncEnabled = value;
        }
    }
}