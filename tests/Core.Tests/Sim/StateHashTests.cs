using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.Sim;

public sealed class StateHashTests
{
    [Fact]
    public void SameStateProducesSameHash()
    {
        var a = default(MatchState) with { FrameNumber = 42, RngState = 7 };
        var b = default(MatchState) with { FrameNumber = 42, RngState = 7 };

        Assert.Equal(Godot3dFighter.Core.Sim.StateHash.Compute(a), Godot3dFighter.Core.Sim.StateHash.Compute(b));
    }

    [Fact]
    public void DifferentStateProducesDifferentHash()
    {
        var a = default(MatchState) with { FrameNumber = 42 };
        var b = default(MatchState) with { FrameNumber = 43 };

        Assert.NotEqual(Godot3dFighter.Core.Sim.StateHash.Compute(a), Godot3dFighter.Core.Sim.StateHash.Compute(b));
    }

    [Fact]
    public void DifferentPlayerFieldProducesDifferentHash()
    {
        var a = default(MatchState);
        var b = default(MatchState).WithPlayer(0, new PlayerState { Health = 50 });

        Assert.NotEqual(Godot3dFighter.Core.Sim.StateHash.Compute(a), Godot3dFighter.Core.Sim.StateHash.Compute(b));
    }
}
