using System;
using System.Text;

namespace Veldrith.D3D12;

/// <summary>
/// Builds the HLSL shaders used to filter mip levels and pack their pixels into a copyable buffer.
/// </summary>
internal sealed partial class D3D12GpuMipmapGenerator {
    
    /// <summary>
    /// Creates a compute shader specialized for the source dimension and destination pixel encoding.
    /// </summary>
    /// <param name="format">The format sampled from the source mip and written to the output buffer.</param>
    /// <param name="type">The dimension of the source texture.</param>
    /// <returns>The complete HLSL source for the buffer-based mipmap pipeline.</returns>
    private static string CreateBufferMipmapShader(PixelFormat format, TextureType type) {
        string name = format.ToString();
        bool compressed = FormatHelpers.IsCompressedFormat(format);
        bool integer = name.EndsWith("Int", StringComparison.Ordinal);

        // Integer textures use typed loads. Other color formats use hardware linear filtering.
        string valueType = "float4";
        if (integer) {
            valueType = name.EndsWith("UInt", StringComparison.Ordinal) ? "uint4" : "int4";
        }

        // Cubemaps are exposed as arrays so filtering stays within each face, matching Vulkan blits.
        string textureType;
        string dimensions;
        string coordinates;
        string loadCoordinates;

        switch (type) {
            case TextureType.Texture1D:
                textureType = "Texture1DArray";
                dimensions = """
                             SourceTexture.GetDimensions(sw, layers);
                             sh = 1;
                             """;
                coordinates = "float2(uv.x, layer)";
                loadCoordinates = "int3(min(uint(uv.x * sw), sw - 1), layer, 0)";
                break;

            case TextureType.Texture3D:
                textureType = "Texture3D";
                dimensions = """
                             SourceTexture.GetDimensions(sw, sh, sd);
                             layers = max(sd / 2, 1);
                             """;
                coordinates = "float3(uv, (layer + 0.5) / max(sd / 2, 1))";
                loadCoordinates = """
                                  int4(
                                      min(uint2(uv * float2(sw, sh)), uint2(sw, sh) - 1),
                                      min(uint((layer + 0.5) * sd / max(sd / 2, 1)), sd - 1),
                                      0
                                  )
                                  """;
                break;

            default:
                textureType = "Texture2DArray";
                dimensions = "SourceTexture.GetDimensions(sw, sh, layers);";
                coordinates = "float3(uv, layer)";
                loadCoordinates = "int4(min(uint2(uv * float2(sw, sh)), uint2(sw, sh) - 1), layer, 0)";
                break;
        }

        StringBuilder shader = new();
        shader.AppendLine($"{textureType}<{valueType}> SourceTexture : register(t0);");
        shader.AppendLine("RWByteAddressBuffer Output : register(u0);");
        shader.AppendLine("SamplerState LinearSampler : register(s0);");
        shader.AppendLine();

        shader.AppendLine($$"""
                            // Sample the center of each destination texel and clamp incomplete 4x4 tiles at the edges.
                            {{valueType}} ReadPixel(uint2 pixel, uint layer, uint width, uint height) {
                                uint sw;
                                uint sh;
                                uint sd = 1;
                                uint layers;
                                {{dimensions}}

                                float2 uv = (float2(min(pixel, uint2(width, height) - 1)) + 0.5) / float2(width, height);
                                return {{(integer ? $"SourceTexture.Load({loadCoordinates})" : $"SourceTexture.SampleLevel(LinearSampler, {coordinates}, 0)")}};
                            }

                            // sRGB source views decode before filtering. Re-encode RGB before storing the destination bits.
                            // Alpha stays linear for both compressed and uncompressed formats.
                            float3 EncodeSrgb(float3 value) {
                                return float3(
                                    value.r <= 0.0031308 ? 12.92 * value.r : 1.055 * pow(max(value.r, 0), 1.0 / 2.4) - 0.055,
                                    value.g <= 0.0031308 ? 12.92 * value.g : 1.055 * pow(max(value.g, 0), 1.0 / 2.4) - 0.055,
                                    value.b <= 0.0031308 ? 12.92 * value.b : 1.055 * pow(max(value.b, 0), 1.0 / 2.4) - 0.055
                                );
                            }

                            """);

        if (compressed) {
            shader.AppendLine(BcEncoderShader);
        }

        shader.AppendLine($$"""
                            [numthreads(8, 8, 1)]
                            void cs_main(uint3 tid : SV_DispatchThreadID) {
                                uint sw;
                                uint sh;
                                uint sd = 1;
                                uint layers;
                                {{dimensions}}

                                uint width = max(sw / 2, 1);
                                uint height = max(sh / 2, 1);

                                if (tid.x * 4 >= width || tid.y * 4 >= height || tid.z >= layers) {
                                    return;
                                }

                            """);

        string srgb = name.EndsWith("SRgb", StringComparison.Ordinal) ? "v.rgb = EncodeSrgb(v.rgb);" : "";

        if (compressed) {
            int bc = name[2] - '0';
            bool signed = name.Contains("SNorm", StringComparison.Ordinal);
            bool alpha = name.Contains("Rgba", StringComparison.Ordinal);
            uint blockBytes = FormatHelpers.GetBlockSizeInBytes(format);

            shader.AppendLine($$"""
                                // BC formats store one block per 4x4 texels. Match D3D12's row and layer alignment.
                                uint rowPitch = (((width + 3) / 4 * {{blockBytes}}) + 255) & ~255u;
                                uint layerPitch = (rowPitch * ((height + 3) / 4) + 511) & ~511u;
                                uint address = tid.z * layerPitch + tid.y * rowPitch + tid.x * {{blockBytes}};
                                float4 pixels[16];

                                [unroll]
                                for (uint y = 0; y < 4; y++) {
                                    [unroll]
                                    for (uint x = 0; x < 4; x++) {
                                        float4 v = ReadPixel(tid.xy * 4 + uint2(x, y), tid.z, width, height);
                                        {{srgb}}
                                        pixels[y * 4 + x] = v;
                                    }
                                }

                                """);

            shader.AppendLine(bc switch {
                1 => $"Output.Store2(address, EncodeBc1(pixels, {(alpha ? "true" : "false")}));",
                2 => """
                     // BC2 stores sixteen explicit 4-bit alpha values, followed by a BC1 color block.
                     uint2 a = 0;

                     [unroll]
                     for (uint i = 0; i < 16; i++) {
                         a[i / 8] |= uint(round(saturate(pixels[i].a) * 15)) << ((i % 8) * 4);
                     }

                     Output.Store2(address, a);
                     Output.Store2(address + 8, EncodeBc1(pixels, false));
                     """,
                3 => """
                     // BC3 combines a BC4-style alpha block with an opaque BC1 color block.
                     Output.Store2(address, EncodeBc4(pixels, 3, false));
                     Output.Store2(address + 8, EncodeBc1(pixels, false));
                     """,
                4 => $"Output.Store2(address, EncodeBc4(pixels, 0, {(signed ? "true" : "false")}));",
                5 => $$"""
                       // BC5 stores independent red and green BC4 blocks.
                       Output.Store2(address, EncodeBc4(pixels, 0, {{(signed ? "true" : "false")}}));
                       Output.Store2(address + 8, EncodeBc4(pixels, 1, {{(signed ? "true" : "false")}}));
                       """,
                7 => "Output.Store4(address, EncodeBc7(pixels));",
                _ => throw new VeldridException($"Unsupported mipmap compression: {format}.")
            });
        }
        else {
            uint bytes = FormatSizeHelpers.GetSizeInBytes(format);
            string encode = GetPixelPackingCode(format, out int components, out int bits);

            shader.AppendLine($$"""
                                // Volume slices are contiguous within a footprint; array layers start on 512-byte boundaries.
                                uint rowPitch = (width * {{bytes}} + 255) & ~255u;
                                uint slicePitch = rowPitch * height;
                                uint layerPitch = {{(type == TextureType.Texture3D ? "slicePitch" : "(slicePitch + 511) & ~511u")}};

                                [unroll]
                                for (uint y = 0; y < 4; y++) {
                                    if (tid.y * 4 + y >= height) {
                                        break;
                                    }

                                    // Pack four adjacent pixels together so sub-32-bit components never race on a buffer word.
                                    uint words[{{bytes}}];

                                    [unroll]
                                    for (uint word = 0; word < {{bytes}}; word++) {
                                        words[word] = 0;
                                    }

                                    [unroll]
                                    for (uint x = 0; x < 4; x++) {
                                        {{valueType}} v = ReadPixel(tid.xy * 4 + uint2(x, y), tid.z, width, height);
                                        {{srgb}}
                                        {{encode}}

                                        [unroll]
                                        for (uint component = 0; component < {{components}}; component++) {
                                            uint bit = (x * {{components}} + component) * {{bits}};
                                            words[bit / 32] |= packed[component] << (bit % 32);
                                        }
                                    }

                                    uint address = tid.z * layerPitch + (tid.y * 4 + y) * rowPitch + tid.x * 4 * {{bytes}};

                                    [unroll]
                                    for (uint word = 0; word < {{bytes}}; word++) {
                                        Output.Store(address + word * 4, words[word]);
                                    }
                                }
                                """);
        }

        shader.AppendLine("}");
        return shader.ToString();
    }

    /// <summary>
    /// Creates the HLSL statements that quantize one filtered pixel and arrange its destination bits.
    /// </summary>
    /// <param name="format">The uncompressed destination format.</param>
    /// <param name="components">The number of independently packed components, or one for a packed pixel format.</param>
    /// <param name="bits">The bit width of each packed component.</param>
    /// <returns>HLSL statements declaring a <c>uint4 packed</c> value for the output writer.</returns>
    private static string GetPixelPackingCode(PixelFormat format, out int components, out int bits) {
        string name = format.ToString();
        components = 1;
        bits = 8;

        if (name.Contains("A", StringComparison.Ordinal)) {
            components = 4;
        }
        else if (name.Contains("G", StringComparison.Ordinal)) {
            components = 2;
        }

        if (name.StartsWith("R32", StringComparison.Ordinal)) {
            bits = 32;
        }
        else if (name.StartsWith("R16", StringComparison.Ordinal)) {
            bits = 16;
        }

        if (format == PixelFormat.R10G10B10A2UNorm || format == PixelFormat.R10G10B10A2UInt) {
            components = 1;
            bits = 32;

            string value = format == PixelFormat.R10G10B10A2UNorm
                ? "uint4 q = uint4(round(saturate(v) * float4(1023, 1023, 1023, 3)));"
                : "uint4 q = min(v, uint4(1023, 1023, 1023, 3));";

            return $$"""
                     {{value}}
                     uint4 packed = uint4(q.r | (q.g << 10) | (q.b << 20) | (q.a << 30), 0, 0, 0);
                     """;
        }

        if (format == PixelFormat.R11G11B10Float) {
            components = 1;
            bits = 32;

            // The unsigned 5-bit exponent matches half precision; round the remaining mantissa bits.
            return """
                   uint3 h = f32tof16(clamp(v.rgb, 0, float3(65024, 65024, 64512)));
                   uint3 q = (h + uint3(15, 15, 31) + ((h >> uint3(4, 4, 5)) & 1)) >> uint3(4, 4, 5);
                   uint4 packed = uint4(q.r | (q.g << 11) | (q.b << 22), 0, 0, 0);
                   """;
        }

        // Preserve the destination channel order and quantize normalized values before packing.
        string swizzle = name.StartsWith("B8", StringComparison.Ordinal) ? "v.bgra" : "v";
        string expression;

        if (name.EndsWith("UInt", StringComparison.Ordinal) || name.EndsWith("SInt", StringComparison.Ordinal)) {
            expression = "uint4(v)";
        }
        else if (name.EndsWith("Float", StringComparison.Ordinal)) {
            expression = bits == 32 ? "asuint(v)" : "f32tof16(v)";
        }
        else if (name.EndsWith("SNorm", StringComparison.Ordinal)) {
            expression = $"uint4(int4(round(clamp(v, -1, 1) * {(bits == 8 ? 127 : 32767)})))";
        }
        else {
            expression = $"uint4(round(saturate({swizzle}) * {(bits == 8 ? 255 : 65535)}))";
        }

        // Mask narrow signed values before combining components into the same 32-bit word.
        return $"uint4 packed = {expression}" + (bits == 32 ? ";" : $" & {(bits == 8 ? "255u" : "65535u")};");
    }

    /// <summary>
    /// Stores shared bit packing, endpoint fitting, and BC1/BC4/BC7 encoding functions.
    /// BC2, BC3, and BC5 combine these building blocks in the generated entry point.
    /// </summary>
    private const string BcEncoderShader = """
                                           // Append a field to a little-endian block, including fields that cross a 32-bit word boundary.
                                           void PutBits(inout uint4 output, inout uint cursor, uint value, uint count) {
                                               uint word = cursor / 32;
                                               uint shift = cursor % 32;
                                               output[word] |= value << shift;

                                               if (shift + count > 32) {
                                                   output[word + 1] |= value >> (32 - shift);
                                               }

                                               cursor += count;
                                           }

                                           // BC1 color endpoints use a packed 5:6:5 RGB representation.
                                           uint Pack565(float3 v) {
                                               uint3 q = uint3(round(saturate(v) * float3(31, 63, 31)));
                                               return (q.r << 11) | (q.g << 5) | q.b;
                                           }

                                           // Reconstruct the endpoint exactly as the hardware decoder expands its color bits.
                                           float3 Unpack565(uint v) {
                                               uint3 q = uint3((v >> 11) & 31, (v >> 5) & 63, v & 31);
                                               return float3((q.r << 3) | (q.r >> 2), (q.g << 2) | (q.g >> 4), (q.b << 3) | (q.b >> 2)) / 255.0;
                                           }

                                           // Fit a line through the block's colors and choose its extreme texels as endpoints.
                                           // BC1 ignores alpha and excludes transparent texels; BC7 fits all four channels.
                                           void FitEndpoints(float4 pixels[16], bool includeAlpha, bool skipTransparent, out float4 lo, out float4 hi) {
                                               float4 mean = 0;
                                               float count = 0;

                                               [unroll]
                                               for (uint i = 0; i < 16; i++) {
                                                   if (!skipTransparent || pixels[i].a >= 0.5) {
                                                       mean += pixels[i];
                                                       count++;
                                                   }
                                               }

                                               mean /= max(count, 1);
                                               float4 variance = 0;

                                               [unroll]
                                               for (uint i = 0; i < 16; i++) {
                                                   float4 delta = pixels[i] - mean;

                                                   if (!includeAlpha) {
                                                       delta.a = 0;
                                                   }

                                                   if (!skipTransparent || pixels[i].a >= 0.5) {
                                                       variance += delta * delta;
                                                   }
                                               }

                                               // Start with the channel that varies most so opposing channels do not cancel the initial axis.
                                               uint channel = 0;

                                               [unroll]
                                               for (uint c = 1; c < 4; c++) {
                                                   if (variance[c] > variance[channel]) {
                                                       channel = c;
                                                   }
                                               }

                                               float4 axis = float4(channel == 0, channel == 1, channel == 2, channel == 3);

                                               // Power iteration on the covariance matrix keeps opposing color
                                               // channels on the fitted line instead of collapsing them to gray.
                                               [unroll]
                                               for (uint iteration = 0; iteration < 4; iteration++) {
                                                   float4 next = 0;

                                                   [unroll]
                                                   for (uint i = 0; i < 16; i++) {
                                                       float4 delta = pixels[i] - mean;

                                                       if (!includeAlpha) {
                                                           delta.a = 0;
                                                       }

                                                       if (!skipTransparent || pixels[i].a >= 0.5) {
                                                           next += delta * dot(delta, axis);
                                                       }
                                                   }

                                                   float lengthSquared = dot(next, next);

                                                   if (lengthSquared > 1e-12) {
                                                       axis = next * rsqrt(lengthSquared);
                                                   }
                                               }

                                               lo = mean;
                                               hi = mean;
                                               float minProjection = 1e20;
                                               float maxProjection = -1e20;

                                               [unroll]
                                               for (uint i = 0; i < 16; i++) {
                                                   if (!skipTransparent || pixels[i].a >= 0.5) {
                                                       float projection = dot(pixels[i] - mean, axis);

                                                       if (projection < minProjection) {
                                                           lo = pixels[i];
                                                           minProjection = projection;
                                                       }

                                                       if (projection > maxProjection) {
                                                           hi = pixels[i];
                                                           maxProjection = projection;
                                                       }
                                                   }
                                               }
                                           }

                                           // Encode RGB with a four-color palette, or a three-color palette and a transparent index.
                                           uint2 EncodeBc1(float4 pixels[16], bool alpha) {
                                               float4 lo;
                                               float4 hi;
                                               FitEndpoints(pixels, false, alpha, lo, hi);
                                               bool transparent = false;

                                               [unroll]
                                               for (uint i = 0; i < 16; i++) {
                                                   transparent = transparent || (alpha && pixels[i].a < 0.5);
                                               }

                                               uint c0 = Pack565(hi.rgb);
                                               uint c1 = Pack565(lo.rgb);

                                               // Endpoint ordering selects BC1's transparent or opaque decoding mode.
                                               if (transparent) {
                                                   uint t = min(c0, c1);
                                                   c1 = max(c0, c1);
                                                   c0 = t;
                                               }
                                               else {
                                                   uint t = max(c0, c1);
                                                   c1 = min(c0, c1);
                                                   c0 = t;

                                                   if (c0 == c1) {
                                                       if (c1 == 65535) {
                                                           c1--;
                                                       }
                                                       else {
                                                           c0 = c1 + 1;
                                                       }
                                                   }
                                               }

                                               float3 palette[4];
                                               palette[0] = Unpack565(c0);
                                               palette[1] = Unpack565(c1);
                                               palette[2] = transparent ? (palette[0] + palette[1]) / 2 : (2 * palette[0] + palette[1]) / 3;
                                               palette[3] = (palette[0] + 2 * palette[1]) / 3;

                                               // Assign each texel the nearest decoded palette color before packing its two-bit index.
                                               uint indices = 0;

                                               [unroll]
                                               for (uint p = 0; p < 16; p++) {
                                                   uint best = 0;
                                                   float error = 1e20;

                                                   [unroll]
                                                   for (uint j = 0; j < 4; j++) {
                                                       float3 delta = pixels[p].rgb - palette[j];
                                                       float e = dot(delta, delta);

                                                       if (e < error && (!transparent || j < 3)) {
                                                           error = e;
                                                           best = j;
                                                       }
                                                   }

                                                   if (transparent && pixels[p].a < 0.5) {
                                                       best = 3;
                                                   }

                                                   indices |= best << (p * 2);
                                               }

                                               return uint2(c0 | (c1 << 16), indices);
                                           }

                                           // Encode one signed or unsigned channel using two endpoints and sixteen three-bit indices.
                                           // The same layout stores BC3 alpha and each of BC5's two channels.
                                           uint2 EncodeBc4(float4 pixels[16], uint channel, bool isSigned) {
                                               float scale = isSigned ? 127 : 255;
                                               float lo = scale;
                                               float hi = -scale;

                                               [unroll]
                                               for (uint i = 0; i < 16; i++) {
                                                   float v = round(clamp(pixels[i][channel], isSigned ? -1 : 0, 1) * scale);
                                                   lo = min(lo, v);
                                                   hi = max(hi, v);
                                               }

                                               // Keep distinct, descending endpoints to select the eight-value interpolation mode.
                                               if (hi <= lo) {
                                                   if (hi < scale) {
                                                       hi++;
                                                   }
                                                   else {
                                                       lo--;
                                                   }
                                               }

                                               float palette[8];
                                               palette[0] = hi;
                                               palette[1] = lo;

                                               [unroll]
                                               for (uint k = 2; k < 8; k++) {
                                                   palette[k] = ((8 - k) * hi + (k - 1) * lo) / 7;
                                               }

                                               uint4 output = 0;
                                               uint cursor = 0;
                                               PutBits(output, cursor, uint(int(hi)) & 255, 8);
                                               PutBits(output, cursor, uint(int(lo)) & 255, 8);

                                               [unroll]
                                               for (uint p = 0; p < 16; p++) {
                                                   uint best = 0;
                                                   float error = 1e20;

                                                   [unroll]
                                                   for (uint j = 0; j < 8; j++) {
                                                       float e = abs(pixels[p][channel] * scale - palette[j]);

                                                       if (e < error) {
                                                           error = e;
                                                           best = j;
                                                       }
                                                   }

                                                   PutBits(output, cursor, best, 3);
                                               }

                                               return output.xy;
                                           }

                                           // BC7 specifies these interpolation weights for its four-bit color indices.
                                           static const uint Bc7Weights[16] = { 0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64 };

                                           // Mode 6 stores one RGBA subset with seven-bit endpoints, one shared low bit per endpoint,
                                           // and four-bit indices. It provides a compact runtime encoder without subset partition searches.
                                           uint4 EncodeBc7(float4 pixels[16]) {
                                               float4 lo;
                                               float4 hi;
                                               FitEndpoints(pixels, true, false, lo, hi);
                                               lo = round(saturate(lo) * 255);
                                               hi = round(saturate(hi) * 255);

                                               uint4 endpoints[2];
                                               uint pbits[2];

                                               // Choose the shared low bit that minimizes the error across all four endpoint channels.
                                               [unroll]
                                               for (uint e = 0; e < 2; e++) {
                                                   float4 target = e == 0 ? lo : hi;
                                                   uint4 q0 = uint4(clamp(round(target / 2), 0, 127));
                                                   uint4 q1 = uint4(clamp(round((target - 1) / 2), 0, 127));
                                                   float4 d0 = target - float4(q0 * 2);
                                                   float4 d1 = target - float4(q1 * 2 + 1);
                                                   pbits[e] = dot(d1, d1) < dot(d0, d0) ? 1 : 0;
                                                   endpoints[e] = pbits[e] == 0 ? q0 : q1;
                                               }

                                               // Compare against the integer palette that the BC7 hardware decoder will reconstruct.
                                               uint indices[16];

                                               [unroll]
                                               for (uint p = 0; p < 16; p++) {
                                                   uint best = 0;
                                                   float error = 1e20;

                                                   [unroll]
                                                   for (uint j = 0; j < 16; j++) {
                                                       uint w = Bc7Weights[j];
                                                       uint4 decoded = ((64 - w) * (endpoints[0] * 2 + pbits[0]) + w * (endpoints[1] * 2 + pbits[1]) + 32) >> 6;
                                                       float4 delta = saturate(pixels[p]) * 255 - float4(decoded);
                                                       float distance = dot(delta, delta);

                                                       if (distance < error) {
                                                           error = distance;
                                                           best = j;
                                                       }
                                                   }

                                                   indices[p] = best;
                                               }

                                               // The anchor texel omits its high index bit; reverse endpoints if needed.
                                               if (indices[0] >= 8) {
                                                   uint4 t = endpoints[0];
                                                   endpoints[0] = endpoints[1];
                                                   endpoints[1] = t;

                                                   uint b = pbits[0];
                                                   pbits[0] = pbits[1];
                                                   pbits[1] = b;

                                                   [unroll]
                                                   for (uint i = 0; i < 16; i++) {
                                                       indices[i] = 15 - indices[i];
                                                   }
                                               }

                                               // Write the mode prefix, channel-major endpoints, shared low bits, and texel indices.
                                               uint4 output = 0;
                                               uint cursor = 0;
                                               PutBits(output, cursor, 64, 7);

                                               [unroll]
                                               for (uint channel = 0; channel < 4; channel++) {
                                                   PutBits(output, cursor, endpoints[0][channel], 7);
                                                   PutBits(output, cursor, endpoints[1][channel], 7);
                                               }

                                               PutBits(output, cursor, pbits[0], 1);
                                               PutBits(output, cursor, pbits[1], 1);

                                               [unroll]
                                               for (uint p = 0; p < 16; p++) {
                                                   PutBits(output, cursor, indices[p], p == 0 ? 3 : 4);
                                               }

                                               return output;
                                           }
                                           """;
}