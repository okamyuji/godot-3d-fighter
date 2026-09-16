using Godot3dFighter.Core;
using Xunit;

namespace Godot3dFighter.Core.Tests;

public sealed class ScaffoldingTests
{
    [Fact]
    public void CoreProjectReferenceIsWired()
    {
        Assert.Equal("scaffold", ScaffoldMarker.Value);
    }
}
