using System.Runtime.InteropServices;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.State;

/// <summary>1人分の試合状態（ADR-0005）。サイズの大きい項目から順に並べ、詰め物のバイトを作らない。</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct PlayerState
{
    public InputBuffer Inputs { get; init; }

    public Vec3Fix Position { get; init; }

    public Vec3Fix Velocity { get; init; }

    public int Health { get; init; }

    public ushort StateFrame { get; init; }

    public Angle16 Facing { get; init; }

    public ushort StunFrames { get; init; }

    public byte HitstopFrames { get; init; }

    public StateKind State { get; init; }

    public byte CurrentMove { get; init; }

    public PlayerFlags Flags { get; init; }

    public HitKind LastHitKind { get; init; }

    public byte Slot { get; init; }
}
