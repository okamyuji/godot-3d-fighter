using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class CameraSideTests
{
    private static Vec3Fix P(decimal x, decimal z) => new(Fix16.FromDecimal(x), Fix16.Zero, Fix16.FromDecimal(z));

    [Fact]
    public void P1OnLeftStaysLeftWhenBothMoveRightTogether()
    {
        // P1が-X、P2が+Xのまま、2人一緒に右へ動いても左右は入れ替わらない。
        var yaw = new Angle16(0);
        var result = CameraSide.Update(P(-1, 0), P(1, 0), yaw);

        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void SwapsSideAssignmentWhenP1EndsUpOnTheRightWithoutRotatingTheCamera()
    {
        // P1が+X、P2が-Xに入れ替わった（投げで裏へ回した想定）。CameraYawはほぼ変えず、
        // 画面の左に立つプレイヤーだけが入れ替わる。
        var yaw = new Angle16(0);
        var result = CameraSide.Update(P(1, 0), P(-1, 0), yaw);

        Assert.Equal((ushort)0, result.Value);
        Assert.False(CameraSide.IsP1OnLeft(P(1, 0), P(-1, 0), result));
    }

    [Fact]
    public void DoesNotChangeWhenHorizontalDistanceIsZero()
    {
        var yaw = new Angle16(12345);
        var result = CameraSide.Update(P(3, 3), P(3, 3), yaw);

        Assert.Equal(yaw, result);
    }

    [Fact]
    public void ChangeNeverExceedsAQuarterTurnInOneFrame()
    {
        // 円周上をゆっくり回り込んでも、毎フレームの変化は16384を超えない。
        var yaw = new Angle16(0);
        for (var i = 1; i <= 32; i++)
        {
            var angleDegSteps = i; // 32分割で1周
            var x = Fix16.FromDecimal(3m) * Trig.Cos(new Angle16((ushort)(angleDegSteps * (65536 / 32))));
            var z = Fix16.Zero - (Fix16.FromDecimal(3m) * Trig.Sin(new Angle16((ushort)(angleDegSteps * (65536 / 32)))));
            var p2 = new Vec3Fix(x, Fix16.Zero, z);
            var next = CameraSide.Update(P(-3, 0), p2, yaw);

            var diff = unchecked((short)(next.Value - yaw.Value));
            Assert.True(diff > -16384 && diff <= 16384, $"step {i}: diff {diff}");
            yaw = next;
        }
    }
}
