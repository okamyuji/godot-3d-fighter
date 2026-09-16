using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.Sim;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class ThrowEscapeResolverTests
{
    private static MoveData ThrowMove(ThrowSide side = ThrowSide.Front, string command = "P+G") => new()
    {
        Name = "throw",
        Command = command,
        Kind = MoveKind.Throw,
        Posture = Posture.Stand,
        Startup = 6,
        Active = 1,
        Recovery = 20,
        Tracking = 0,
        Height = HitHeight.Mid,
        Damage = 25,
        CounterDamage = 25,
        Hitstun = 0,
        CounterHitstun = 0,
        Blockstun = 0,
        Hitstop = 0,
        Knockdown = true,
        CounterKnockdown = true,
        HitsDown = false,
        Pushback = Fix16.Zero,
        Motion = [],
        Windows = [],
        ThrowRange = Fix16.FromDecimal(1m),
        ThrowSide = side,
        ThrowEndOffset = new Vec3Fix(Fix16.FromInt(1), Fix16.Zero, Fix16.Zero),
    };

    private static InputBuffer With(params byte[] bitsInOrder)
    {
        var buffer = default(InputBuffer);
        foreach (var bits in bitsInOrder)
        {
            buffer = buffer.Push(new InputFrame(bits));
        }

        return buffer;
    }

    [Fact]
    public void MatchingCommandEscapes()
    {
        var buffer = With(InputFrame.Guard, (byte)(InputFrame.Guard | InputFrame.Punch));
        var move = ThrowMove(command: "P+G");

        Assert.True(ThrowEscapeResolver.TryEscape(buffer, true, move));
    }

    [Fact]
    public void WrongDirectionDoesNotEscape()
    {
        var buffer = With(0, InputFrame.Right, InputFrame.Punch);
        var move = ThrowMove(command: "4P");

        Assert.False(ThrowEscapeResolver.TryEscape(buffer, true, move));
    }

    [Fact]
    public void MatchingDirectionEscapes()
    {
        var buffer = With(0, InputFrame.Left, (byte)(InputFrame.Left | InputFrame.Punch));
        var move = ThrowMove(command: "4P");

        Assert.True(ThrowEscapeResolver.TryEscape(buffer, true, move));
    }

    [Fact]
    public void BackThrowCanNeverBeEscaped()
    {
        var buffer = With(InputFrame.Guard, (byte)(InputFrame.Guard | InputFrame.Punch));
        var move = ThrowMove(side: ThrowSide.Back, command: "P+G");

        Assert.False(ThrowEscapeResolver.TryEscape(buffer, true, move));
    }

    [Fact]
    public void NoInputDoesNotEscape()
    {
        var buffer = With(0);
        var move = ThrowMove(command: "P+G");

        Assert.False(ThrowEscapeResolver.TryEscape(buffer, true, move));
    }
}
