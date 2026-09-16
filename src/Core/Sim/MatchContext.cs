using System.Collections.Generic;
using Godot3dFighter.Core.Data;

namespace Godot3dFighter.Core.Sim;

/// <summary>Stepに渡す読み取り専用の参照データ。試合状態には含めない。</summary>
public sealed class MatchContext
{
    public required IReadOnlyList<CharacterData> Characters { get; init; }

    public required StageData Stage { get; init; }

    public required Rules Rules { get; init; }
}
