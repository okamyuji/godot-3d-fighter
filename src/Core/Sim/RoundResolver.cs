using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>ラウンドと試合の決着を判定する（ADR-0022）。</summary>
public static class RoundResolver
{
    public static RoundOutcome CheckRoundEnd(in PlayerState p1, in PlayerState p2, StageData stage, ushort roundTimerFrames)
    {
        var p1Ko = p1.Health <= 0;
        var p2Ko = p2.Health <= 0;
        var p1Out = RingBounds.IsOut(p1.Position, stage);
        var p2Out = RingBounds.IsOut(p2.Position, stage);
        var p1Lost = p1Ko || p1Out;
        var p2Lost = p2Ko || p2Out;

        if (p1Lost || p2Lost)
        {
            var reason = p1Ko || p2Ko ? RoundEndReason.KnockOut : RoundEndReason.RingOut;
            return new RoundOutcome(true, reason, p2Lost, p1Lost);
        }

        if (roundTimerFrames == 0)
        {
            return new RoundOutcome(true, RoundEndReason.TimeUp, p1.Health >= p2.Health, p2.Health >= p1.Health);
        }

        return default;
    }

    public static bool IsMatchOver(byte p1Wins, byte p2Wins, byte roundNumber, byte roundsToWin, byte maxRounds) =>
        (p1Wins >= roundsToWin && p1Wins > p2Wins)
        || (p2Wins >= roundsToWin && p2Wins > p1Wins)
        || roundNumber >= maxRounds;
}
