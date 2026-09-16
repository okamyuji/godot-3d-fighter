using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>試合状態のバイト列からFNV-1a（64bit）を計算する。乗算はunchecked（ADR-0005）。</summary>
public static class StateHash
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Compute(in MatchState state)
    {
        var span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in state), 1);
        var bytes = MemoryMarshal.AsBytes(span);

        var hash = OffsetBasis;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash = unchecked(hash * Prime);
        }

        return hash;
    }
}
