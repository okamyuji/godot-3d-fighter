using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Godot3dFighter.Core.State;

/// <summary>試合全体の状態（ADR-0005、ADR-0016、ADR-0022）。参照型の項目を持たない。</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MatchState : IEquatable<MatchState>
{
    [InlineArray(Limits.PlayerCount)]
    private struct PlayerPair
    {
        private PlayerState _e0;
    }

    [InlineArray(Limits.PlayerCount)]
    private struct BytePair
    {
        private byte _e0;
    }

    private PlayerPair _players;
    public uint FrameNumber;
    public uint RngState;
    public ushort RoundTimerFrames;
    public ushort PhaseFrames;
    public Math.Angle16 CameraYaw;
    private BytePair _roundWins;
    public byte RoundNumber;
    public RoundPhase Phase;
    public RoundEndReason LastRoundReason;
    public byte LastRoundWinners;

    public readonly PlayerState GetPlayer(int index) => _players[index];

    public readonly MatchState WithPlayer(int index, PlayerState value)
    {
        var result = this;
        result._players[index] = value;
        return result;
    }

    public readonly byte GetRoundWins(int index) => _roundWins[index];

    public readonly MatchState WithRoundWins(int index, byte value)
    {
        var result = this;
        result._roundWins[index] = value;
        return result;
    }

    public readonly bool Equals(MatchState other)
    {
        for (var i = 0; i < Limits.PlayerCount; i++)
        {
            if (!GetPlayer(i).Equals(other.GetPlayer(i)))
            {
                return false;
            }

            if (GetRoundWins(i) != other.GetRoundWins(i))
            {
                return false;
            }
        }

        return FrameNumber == other.FrameNumber
            && RngState == other.RngState
            && RoundTimerFrames == other.RoundTimerFrames
            && PhaseFrames == other.PhaseFrames
            && CameraYaw.Equals(other.CameraYaw)
            && RoundNumber == other.RoundNumber
            && Phase == other.Phase
            && LastRoundReason == other.LastRoundReason
            && LastRoundWinners == other.LastRoundWinners;
    }

    public override readonly bool Equals(object? obj) => obj is MatchState other && Equals(other);

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        for (var i = 0; i < Limits.PlayerCount; i++)
        {
            hash.Add(GetPlayer(i));
            hash.Add(GetRoundWins(i));
        }

        hash.Add(FrameNumber);
        hash.Add(RngState);
        hash.Add(RoundTimerFrames);
        hash.Add(PhaseFrames);
        hash.Add(CameraYaw);
        hash.Add(RoundNumber);
        hash.Add(Phase);
        hash.Add(LastRoundReason);
        hash.Add(LastRoundWinners);
        return hash.ToHashCode();
    }

    public static bool operator ==(MatchState left, MatchState right) => left.Equals(right);

    public static bool operator !=(MatchState left, MatchState right) => !left.Equals(right);
}
