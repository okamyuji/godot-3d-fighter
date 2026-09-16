using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Data;

/// <summary>試合の規則（ADR-0010、ADR-0022、ADR-0024）。</summary>
public sealed class Rules
{
    public required ushort RoundTimeSeconds { get; init; }

    public required byte RoundsToWin { get; init; }

    public required byte MaxRounds { get; init; }

    public required int InitialHealth { get; init; }

    public required ushort IntroFrames { get; init; }

    public required ushort RoundEndFrames { get; init; }

    public required ushort TechFrames { get; init; }

    public required ushort TechRecoveryFrames { get; init; }

    public required ushort DownMinFrames { get; init; }

    public required ushort DownMaxFrames { get; init; }

    public required ushort RiseFrames { get; init; }

    public required ushort ThrowEscapeFrames { get; init; }

    public required ushort ThrowEscapeRecoveryFrames { get; init; }

    public required Fix16 ThrowEscapeDistance { get; init; }

    public required ushort WallStunFrames { get; init; }

    public required uint Seed { get; init; }

    public required bool Training { get; init; }
}
