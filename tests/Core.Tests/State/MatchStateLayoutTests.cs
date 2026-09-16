using System.Runtime.CompilerServices;
using Godot3dFighter.Core.State;
using Xunit;

namespace Godot3dFighter.Core.Tests.State;

public sealed class MatchStateLayoutTests
{
    [Fact]
    public void PlayerStateIs105Bytes()
    {
        Assert.Equal(105, Unsafe.SizeOf<PlayerState>());
    }

    [Fact]
    public void MatchStateIs230Bytes()
    {
        Assert.Equal(230, Unsafe.SizeOf<MatchState>());
    }
}
