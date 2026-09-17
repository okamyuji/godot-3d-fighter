using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game.E2e;

/// <summary>主要導線の走破を実行する（03-screens-and-e2e.md）。オートロードとして登録する。</summary>
public sealed partial class E2eRunner : Node
{
    private const int MaxFrames = 36000;
    private const int DefaultWithinFrames = 60;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private E2eScenario? _scenario;
    private int _stepIndex;
    private int _waitedFrames;
    private int _framesRemaining;
    private bool _active;
    private int _totalFrames;

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();

        var checkFlowsIndex = Array.IndexOf(args, "--check-flows");
        if (checkFlowsIndex >= 0)
        {
            RunCheckFlows();
            return;
        }

        var e2eIndex = Array.IndexOf(args, "--e2e");
        if (e2eIndex < 0 || e2eIndex + 1 >= args.Length)
        {
            return;
        }

        StartScenario(args[e2eIndex + 1]);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_active)
        {
            return;
        }

        _totalFrames++;
        if (_totalFrames > MaxFrames)
        {
            Fail("シナリオが36000フレームを超えました。");
            return;
        }

        RunCurrentStep();
    }

    private void RunCheckFlows()
    {
        var flows = ReadJson<List<E2eFlow>>("res://tests/e2e/flows.json") ?? [];
        var scenarioDir = ProjectSettings.GlobalizePath("res://tests/e2e/scenarios");
        var coveredFlows = new HashSet<string>(StringComparer.Ordinal);

        if (Directory.Exists(scenarioDir))
        {
            foreach (var file in Directory.EnumerateFiles(scenarioDir, "*.json"))
            {
                var scenario = ReadJsonFile<E2eScenario>(file);
                if (scenario is not null)
                {
                    coveredFlows.Add(scenario.Flow);
                }
            }
        }

        var missing = flows.Select(f => f.Id).Where(id => !coveredFlows.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            foreach (var id in missing)
            {
                GD.PrintErr("導線" + id + "に対応するシナリオがありません。");
            }

            GetTree().Quit(1);
            return;
        }

        GetTree().Quit(0);
    }

    private void StartScenario(string scenarioPath)
    {
        var scenario = Path.IsPathRooted(scenarioPath)
            ? ReadJsonFile<E2eScenario>(scenarioPath)
            : ReadJson<E2eScenario>(ToResPath(scenarioPath));
        if (scenario is null)
        {
            GD.PrintErr("シナリオ'" + scenarioPath + "'を読み込めませんでした。");
            GetTree().Quit(1);
            return;
        }

        _scenario = scenario;
        ApplyOverrides(scenario);
        _active = true;
    }

    private void ApplyOverrides(E2eScenario scenario)
    {
        var gameState = GetNode<GameState>("/root/GameState");
        if (scenario.Rules is { } rules)
        {
            gameState.RulesOverridePath = ToResPath(rules);
        }

        if (scenario.Stage is { } stage)
        {
            gameState.StageOverridePath = ToResPath(stage);
        }

        if (scenario.Characters is { Count: 2 } characters)
        {
            gameState.P1CharacterOverridePath = ToResPath(characters[0]);
            gameState.P2CharacterOverridePath = ToResPath(characters[1]);
        }
    }

    private void RunCurrentStep()
    {
        if (_scenario is null)
        {
            return;
        }

        if (_stepIndex >= _scenario.Steps.Count)
        {
            GetTree().Quit(0);
            _active = false;
            return;
        }

        var step = _scenario.Steps[_stepIndex];

        if (step.Frames is { } frames)
        {
            RunFramesStep(step, frames);
            return;
        }

        if (step.Action is { } action)
        {
            RunActionStep(action);
            return;
        }

        if (step.ExpectScreen is { } expectScreen)
        {
            RunWaitStep(step, () => FindScreen()?.ScreenName == expectScreen, "画面'" + expectScreen + "'");
            return;
        }

        if (step.ExpectPhase is { } expectPhase)
        {
            RunWaitStep(step, () => FindMatchState()?.Phase.ToString() == expectPhase, "段階'" + expectPhase + "'");
            return;
        }

        if (step.ExpectHud is { } expectHud)
        {
            RunWaitStep(
                step,
                () => FindMatchState()?.HudText.Contains(expectHud, StringComparison.Ordinal) == true,
                "表示'" + expectHud + "'");
            return;
        }

        if (step.ExpectRoundEnd is { } roundEnd)
        {
            var winnersBits = ToWinnersBits(roundEnd.Winners);
            RunWaitStep(
                step,
                () => FindMatchState() is { } m && m.LastRoundReason.ToString() == roundEnd.Reason && m.LastRoundWinners == winnersBits,
                "ラウンドの決着'" + roundEnd.Reason + "'");
            return;
        }

        if (step.ExpectMatchEnd is { } matchEnd)
        {
            RunWaitStep(
                step,
                () => FindMatchState()?.Phase == RoundPhase.MatchEnd && WinnersEqual(ComputeOverallWinners(), matchEnd.Winners),
                "試合の決着");
            return;
        }

        Fail("手順" + (_stepIndex + 1).ToString(CultureInfo.InvariantCulture) + "の種類が不明です。");
    }

    private void RunFramesStep(E2eStep step, int frames)
    {
        if (_framesRemaining == 0 && _waitedFrames == 0)
        {
            InputMapper.SetOverride(1, E2eInputToken.Parse(step.P1 ?? "5"));
            InputMapper.SetOverride(2, E2eInputToken.Parse(step.P2 ?? "5"));
            _framesRemaining = frames;
            _waitedFrames = 1;
        }

        _framesRemaining--;
        if (_framesRemaining <= 0)
        {
            AdvanceStep();
        }
    }

    private void RunActionStep(string action)
    {
        var screen = FindScreen();
        if (screen is null)
        {
            Fail("手順" + (_stepIndex + 1).ToString(CultureInfo.InvariantCulture) + ": 画面が見つかりません。");
            return;
        }

        if (!screen.TryInvoke(action))
        {
            Fail("手順" + (_stepIndex + 1).ToString(CultureInfo.InvariantCulture) + ": 操作'" + action + "'が失敗しました。");
            return;
        }

        AdvanceStep();
    }

    private void RunWaitStep(E2eStep step, Func<bool> isSatisfied, string description)
    {
        if (isSatisfied())
        {
            AdvanceStep();
            return;
        }

        _waitedFrames++;
        if (_waitedFrames > (step.WithinFrames ?? DefaultWithinFrames))
        {
            Fail("手順" + (_stepIndex + 1).ToString(CultureInfo.InvariantCulture) + ": " + description + "を待てませんでした。");
        }
    }

    private void AdvanceStep()
    {
        _stepIndex++;
        _waitedFrames = 0;
        _framesRemaining = 0;
    }

    private void Fail(string message)
    {
        GD.PrintErr(message);
        _active = false;
        GetTree().Quit(1);
    }

    private IE2eScreen? FindScreen() => GetTree().GetFirstNodeInGroup("e2e_screen") as IE2eScreen;

    private IE2eMatchState? FindMatchState() => GetTree().GetFirstNodeInGroup("e2e_screen") as IE2eMatchState;

    private IReadOnlyList<int> ComputeOverallWinners()
    {
        var gameState = GetNode<GameState>("/root/GameState");
        var p1Wins = 0;
        var p2Wins = 0;
        foreach (var (_, winners) in gameState.RoundHistory)
        {
            if ((winners & 1) != 0)
            {
                p1Wins++;
            }

            if ((winners & 2) != 0)
            {
                p2Wins++;
            }
        }

        if (p1Wins == p2Wins)
        {
            return [];
        }

        return p1Wins > p2Wins ? [1] : [2];
    }

    private static bool WinnersEqual(IReadOnlyList<int> a, IReadOnlyList<int> b) =>
        new HashSet<int>(a).SetEquals(new HashSet<int>(b));

    private static byte ToWinnersBits(IReadOnlyList<int> winners)
    {
        byte bits = 0;
        foreach (var w in winners)
        {
            bits |= (byte)w;
        }

        return bits;
    }

    private static string ToResPath(string path) => path.StartsWith("res://", StringComparison.Ordinal) ? path : "res://" + path;

    private static T? ReadJson<T>(string resPath)
        where T : class
    {
        var absolute = ProjectSettings.GlobalizePath(resPath);
        return ReadJsonFile<T>(absolute);
    }

    private static T? ReadJsonFile<T>(string absolutePath)
        where T : class
    {
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        using var stream = File.OpenRead(absolutePath);
        return JsonSerializer.Deserialize<T>(stream, JsonOptions);
    }
}
