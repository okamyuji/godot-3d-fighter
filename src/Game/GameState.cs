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

    public int StageIndex { get; private set; }

    public bool IsTraining { get; set; }

    private readonly List<(RoundEndReason Reason, byte Winners)> _roundHistory = [];

    public IReadOnlyList<(RoundEndReason Reason, byte Winners)> RoundHistory => _roundHistory;

    public void AddRoundResult(RoundEndReason reason, byte winners) => _roundHistory.Add((reason, winners));

    public string StageName => Path.GetFileNameWithoutExtension(StageFiles[StageIndex]);

    public void NextStage() => StageIndex = (StageIndex + 1) % StageFiles.Length;

    public void ResetSelection()
    {
        P1Confirmed = false;
        P2Confirmed = false;
        StageIndex = 0;
        _roundHistory.Clear();
    }

    public MatchContext LoadContext()
    {
        var character = LoadCharacter("res://data/characters/box.json");
        var stage = LoadStage(StageFiles[StageIndex]);
        var rules = LoadRules(IsTraining ? "res://data/rules-training.json" : "res://data/rules.json");

        return new MatchContext
        {
            Characters = [character, character],
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

    private static FileStream OpenRead(string resPath) =>
        File.OpenRead(ProjectSettings.GlobalizePath(resPath));

    public static void GoTo(SceneTree tree, string scenePath)
    {
        System.ArgumentNullException.ThrowIfNull(tree);
        tree.ChangeSceneToFile(scenePath);
    }
}
