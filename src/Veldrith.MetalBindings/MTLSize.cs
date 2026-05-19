using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]
public struct MTLSize {
    public UIntPtr Width;
    public UIntPtr Height;
    public UIntPtr Depth;

    public MTLSize(uint width, uint height, uint depth) {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
    }
}