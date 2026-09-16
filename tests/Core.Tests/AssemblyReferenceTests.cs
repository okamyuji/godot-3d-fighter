using System.Linq;
using System.Reflection;
using Godot3dFighter.Core.Math;
using Xunit;

namespace Godot3dFighter.Core.Tests;

/// <summary>C-12。Coreの組み立て済みアセンブリがGodotSharpを参照しないことを確かめる。</summary>
public sealed class AssemblyReferenceTests
{
    [Fact]
    public void CoreDoesNotReferenceGodotSharp()
    {
        var coreAssembly = typeof(Fix16).Assembly;

        var references = coreAssembly.GetReferencedAssemblies().Select(a => a.Name);

        Assert.DoesNotContain(references, name => name is not null && name.Contains("Godot", System.StringComparison.Ordinal));
    }
}
