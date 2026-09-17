using System.Collections.Generic;
using System.IO;
using Godot;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game;

/// <summary>画面間で共有する選択状態とデータの読み込み（03-screens-and-e2e.md）。オートロードとして登録する。</summary>
public sealed partial class GameState : Node
{
    private static readonly string[] StageFiles =
    [
        "res://data/stages/default.json",
        "res://data/stages/arena.json",
        "res://data/stages/cliff.json",
    ];

    public bool P1Confirmed { get; set; }

    public bool P2Confirmed { get; set; }

    /// <summary>対戦でP1だけが決定した時にtrueになり、試合画面がP2の入力をCpuPolicyから作る。</summary>
    public bool P2IsCpu { get; set; }

    public int StageIndex { get; private set; }

    public bool IsTraining { get; set; }

    /// <summary>E2eRunnerがシナリオの規則、ステージ、キャラを差し替えるための項目。nullなら既定を使う。</summary>
    public string? RulesOverridePath { get; set; }

    public string? StageOverridePath { get; set; }

    public string? P1CharacterOverridePath { get; set; }

    public string? P2CharacterOverridePath { get; set; }

    private readonly List<(RoundEndReason Reason, byte Winners)> _roundHistory = [];

    public IReadOnlyList<(RoundEndReason Reason, byte Winners)> RoundHistory => _roundHistory;

    public void AddRoundResult(RoundEndReason reason, byte winners) => _roundHistory.Add((reason, winners));

    public string StageName => Path.GetFileNameWithoutExtension(StageFiles[StageIndex]);

    public void NextStage() => StageIndex = (StageIndex + 1) % StageFiles.Length;

    public void ResetSelection()
    {
        P1Confirmed = false;
        P2Confirmed = false;
        P2IsCpu = false;
        StageIndex = 0;
        _roundHistory.Clear();
    }

    public MatchContext LoadContext()
    {
        var p1Character = LoadCharacter(P1CharacterOverridePath ?? "res://data/characters/box.json");
        var p2Character = LoadCharacter(P2CharacterOverridePath ?? "res://data/characters/box.json");
        var stage = LoadStage(StageOverridePath ?? StageFiles[StageIndex]);
        var rules = LoadRules(RulesOverridePath ?? (IsTraining ? "res://data/rules-training.json" : "res://data/rules.json"));

        return new MatchContext
        {
            Characters = [p1Character, p2Character],
            Stage = stage,
            Rules = rules,
        };
    }

    public static CharacterData LoadCharacter(string resPath)
    {
        using var stream = OpenRead(resPath);
        return GameDataLoader.LoadCharacter(stream);
    }

    public static StageData LoadStage(string resPath)
    {
        using var stream = OpenRead(resPath);
        return GameDataLoader.LoadStage(stream);
    }

    public static Rules LoadRules(string resPath)
    {
        using var stream = OpenRead(resPath);
        return GameDataLoader.LoadRules(stream);
    }

    /// <summary>エクスポートした実行ファイルではres://の中身がPCKの中にあり、System.IOでは開けないため、GodotのFileAccessで読む。</summary>
    private static MemoryStream OpenRead(string resPath)
    {
        using var file = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read)
            ?? throw new FileNotFoundException("データファイルを開けません。理由:" + Godot.FileAccess.GetOpenError(), resPath);
        return new MemoryStream(file.GetBuffer((long)file.GetLength()));
    }

    public static void GoTo(SceneTree tree, string scenePath)
    {
        System.ArgumentNullException.ThrowIfNull(tree);
        tree.ChangeSceneToFile(scenePath);
    }
}
