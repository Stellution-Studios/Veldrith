using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlSwapchain : Swapchain {
    private readonly MtlSwapchainFramebuffer _framebuffer;
    private readonly MtlGraphicsDevice gd;
    private bool _disposed;

    private CAMetalDrawable _drawable;
    private CAMetalLayer _metalLayer;
    private bool _syncToVerticalBlank;
    private UIView _uiView; // Valid only when a UIViewSwapchainSource is used.

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

        this._framebuffer = new MtlSwapchainFramebuffer(
            gd,
            this,
            description.DepthFormat,
            format);

        this.GetNextDrawable();
    }

    public override Framebuffer Framebuffer => this._framebuffer;

    public override bool IsDisposed => this._disposed;

    public CAMetalDrawable CurrentDrawable => this._drawable;

    public override bool SyncToVerticalBlank {
        get => this._syncToVerticalBlank;
        set {
            if (this._syncToVerticalBlank != value) {
                this.SetSyncToVerticalBlank(value);
            }
        }
    }

    public override string Name { get; set; }

    #region Disposal

    public override void Dispose() {
        if (this._drawable.NativePtr != IntPtr.Zero) {
            ObjectiveCRuntime.release(this._drawable.NativePtr);
        }

        this._framebuffer.Dispose();
        ObjectiveCRuntime.release(this._metalLayer.NativePtr);

        this._disposed = true;
    }

    #endregion

    public override void Resize(uint width, uint height) {
        if (this._uiView.NativePtr != IntPtr.Zero) {
            this._metalLayer.frame = this._uiView.frame;
        }

        this._metalLayer.drawableSize = new CGSize(width, height);

        this.GetNextDrawable();
    }

    public bool EnsureDrawableAvailable() {
        return !this._drawable.IsNull || this.GetNextDrawable();
    }

    public void InvalidateDrawable() {
        ObjectiveCRuntime.release(this._drawable.NativePtr);
        this._drawable = default;
    }

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

    private void SetSyncToVerticalBlank(bool value) {
        this._syncToVerticalBlank = value;

        if (this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v3
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v4
            || this.gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily2_v1) {
            this._metalLayer.displaySyncEnabled = value;
        }
    }
}