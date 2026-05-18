using System.Runtime.InteropServices;
using Veldrid;

internal static class Program
{
    private static int Main()
    {
        if (!OperatingSystem.IsWindows() || !GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D12))
        {
            Console.WriteLine("SKIP: D3D12 backend is not available on this machine.");
            return 0;
        }

        int failures = 0;
        failures += run("FormatSupport_UnsupportedEtc2_ReturnsFalse", formatSupportUnsupportedEtc2ReturnsFalse);
        failures += run("FormatSupport_StorageSrgb_ReturnsFalse", formatSupportStorageSrgbReturnsFalse);
        failures += run("BackendInfoD3D12_DevicePointer_IsNonZero", backendInfoD3D12DevicePointerIsNonZero);
        failures += run("SampleCountLimit_UnsupportedFormat_ReturnsCount1", sampleCountLimitUnsupportedFormatReturnsCount1);
        failures += run("Map_DynamicBuffer_ReadMode_Throws", mapDynamicBufferReadModeThrows);
        failures += run("BufferCopy_DefaultToDynamic_FallbackProducesExpectedBytes", bufferCopyDefaultToDynamicFallbackProducesExpectedBytes);
        failures += run("BufferCopy_DefaultToDynamic_WithOffsets_ProducesExpectedBytes", bufferCopyDefaultToDynamicWithOffsetsProducesExpectedBytes);
        failures += run("GenerateMipmaps_CapabilityConsistency", generateMipmapsCapabilityConsistency);

        Console.WriteLine(failures == 0
            ? "All D3D12 tests passed."
            : $"D3D12 tests failed: {failures}.");
        return failures == 0 ? 0 : 1;
    }

    private static int run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"PASS: {name}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {name}");
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static GraphicsDevice createDevice()
    {
        var options = new GraphicsDeviceOptions(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: false,
            resourceBindingModel: ResourceBindingModel.Improved,
            preferDepthRangeZeroToOne: true,
            preferStandardClipSpaceYDirection: true);
        return GraphicsDevice.CreateD3D12(options);
    }

    private static void formatSupportUnsupportedEtc2ReturnsFalse()
    {
        using GraphicsDevice gd = createDevice();
        bool supported = gd.GetPixelFormatSupport(
            PixelFormat.Etc2R8G8B8UNorm,
            TextureType.Texture2D,
            TextureUsage.Sampled);
        assertFalse(supported, "ETC2 should not be reported as supported by D3D12.");
    }

    private static void formatSupportStorageSrgbReturnsFalse()
    {
        using GraphicsDevice gd = createDevice();
        bool supported = gd.GetPixelFormatSupport(
            PixelFormat.R8G8B8A8UNormSRgb,
            TextureType.Texture2D,
            TextureUsage.Storage);
        assertFalse(supported, "sRGB texture storage should not be reported as supported.");
    }

    private static void backendInfoD3D12DevicePointerIsNonZero()
    {
        using GraphicsDevice gd = createDevice();
        bool ok = gd.GetD3D12Info(out BackendInfoD3D12 info);
        assertTrue(ok, "GetD3D12Info should succeed on a D3D12 device.");
        if (info.Device == IntPtr.Zero)
        {
            throw new InvalidOperationException("BackendInfoD3D12.Device must not be zero.");
        }
    }

    private static void sampleCountLimitUnsupportedFormatReturnsCount1()
    {
        using GraphicsDevice gd = createDevice();
        TextureSampleCount count = gd.GetSampleCountLimit(PixelFormat.Etc2R8G8B8UNorm, depthFormat: false);
        if (count != TextureSampleCount.Count1)
        {
            throw new InvalidOperationException($"Expected Count1 for unsupported format, got {count}.");
        }
    }

    private static void mapDynamicBufferReadModeThrows()
    {
        using GraphicsDevice gd = createDevice();
        ResourceFactory rf = gd.ResourceFactory;
        using DeviceBuffer buffer = rf.CreateBuffer(new BufferDescription(64, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        expectThrows<VeldridException>(() => gd.Map(buffer, MapMode.Read), "Map(Read) on dynamic buffer should throw.");
    }

    private static void bufferCopyDefaultToDynamicFallbackProducesExpectedBytes()
    {
        using GraphicsDevice gd = createDevice();
        ResourceFactory rf = gd.ResourceFactory;
        using DeviceBuffer source = rf.CreateBuffer(new BufferDescription(64, BufferUsage.VertexBuffer));
        using DeviceBuffer destination = rf.CreateBuffer(new BufferDescription(64, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        using CommandList cl = rf.CreateCommandList();

        byte[] expected = new byte[64];
        for (int i = 0; i < expected.Length; i++)
        {
            expected[i] = (byte)(i * 3 + 1);
        }

        gd.UpdateBuffer(source, 0, expected);

        cl.Begin();
        cl.CopyBuffer(source, 0, destination, 0, 64);
        cl.End();
        gd.SubmitCommands(cl);
        gd.WaitForIdle();

        MappedResource mapped = gd.Map(destination, MapMode.Write);
        try
        {
            byte[] actual = new byte[64];
            Marshal.Copy(mapped.Data, actual, 0, actual.Length);
            assertSequenceEqual(expected, actual, "Default->Dynamic buffer copy mismatch.");
        }
        finally
        {
            gd.Unmap(destination);
        }
    }

    private static void bufferCopyDefaultToDynamicWithOffsetsProducesExpectedBytes()
    {
        using GraphicsDevice gd = createDevice();
        ResourceFactory rf = gd.ResourceFactory;
        using DeviceBuffer source = rf.CreateBuffer(new BufferDescription(128, BufferUsage.VertexBuffer));
        using DeviceBuffer destination = rf.CreateBuffer(new BufferDescription(128, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        using CommandList cl = rf.CreateCommandList();

        byte[] src = new byte[128];
        for (int i = 0; i < src.Length; i++)
        {
            src[i] = (byte)(255 - i);
        }

        gd.UpdateBuffer(source, 0, src);

        cl.Begin();
        cl.CopyBuffer(source, 16, destination, 32, 40);
        cl.End();
        gd.SubmitCommands(cl);
        gd.WaitForIdle();

        MappedResource mapped = gd.Map(destination, MapMode.Write);
        try
        {
            byte[] actual = new byte[128];
            Marshal.Copy(mapped.Data, actual, 0, actual.Length);

            for (int i = 0; i < 40; i++)
            {
                byte expected = src[16 + i];
                byte got = actual[32 + i];
                if (expected != got)
                {
                    throw new InvalidOperationException($"Offset copy mismatch at {i}: expected {expected}, got {got}.");
                }
            }
        }
        finally
        {
            gd.Unmap(destination);
        }
    }

    private static void generateMipmapsCapabilityConsistency()
    {
        using GraphicsDevice gd = createDevice();
        bool depthMips = gd.GetPixelFormatSupport(
            PixelFormat.R32Float,
            TextureType.Texture2D,
            TextureUsage.DepthStencil | TextureUsage.GenerateMipmaps);
        assertFalse(depthMips, "Depth textures should not report runtime mip generation support.");

        bool compressedMips = gd.GetPixelFormatSupport(
            PixelFormat.Bc1RgbaUNorm,
            TextureType.Texture2D,
            TextureUsage.Sampled | TextureUsage.GenerateMipmaps);
        assertFalse(compressedMips, "Compressed textures should not report runtime mip generation support.");

        bool cubemapMips = gd.GetPixelFormatSupport(
            PixelFormat.R8G8B8A8UNorm,
            TextureType.Texture2D,
            TextureUsage.Sampled | TextureUsage.Cubemap | TextureUsage.GenerateMipmaps);
        assertFalse(cubemapMips, "Cubemap textures should not report runtime mip generation support in current D3D12 path.");

        _ = gd.GetPixelFormatSupport(
            PixelFormat.R8G8B8A8UNorm,
            TextureType.Texture2D,
            TextureUsage.Sampled | TextureUsage.GenerateMipmaps);
    }

    private static void assertFalse(bool value, string message)
    {
        if (value)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void assertTrue(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void expectThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void assertSequenceEqual(byte[] expected, byte[] actual, string message)
    {
        if (expected.Length != actual.Length)
        {
            throw new InvalidOperationException($"{message} Length mismatch.");
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                throw new InvalidOperationException($"{message} Byte mismatch at {i}: expected {expected[i]}, got {actual[i]}.");
            }
        }
    }
}
