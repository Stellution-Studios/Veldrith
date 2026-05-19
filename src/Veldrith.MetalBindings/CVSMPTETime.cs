// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Veldrith.MetalBindings;

public struct CVSMPTETime {
    public ulong flags;
    public ulong hostTime;
    public double rateScalar;
    public ulong reserved;
    public uint version;
    public long videoRefreshPeriod;
    public long videoTime;
    public int videoTimeScale;
}