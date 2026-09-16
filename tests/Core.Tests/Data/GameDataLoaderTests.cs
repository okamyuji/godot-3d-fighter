using System;
using System.IO;
using System.Text;
using Godot3dFighter.Core.Data;
using Xunit;

namespace Godot3dFighter.Core.Tests.Data;

public sealed class GameDataLoaderTests
{
    private static MemoryStream ToStream(string json) => new(Encoding.UTF8.GetBytes(json));

    private const string MinimalCharacterJson = """
        {
          "name": "test",
          "walkSpeed": 0.05, "backWalkSpeed": 0.04,
          "dashSpeed": 0.1, "dashFrames": 10, "backdashSpeed": 0.1, "backdashFrames": 10, "dashCancelFrames": 4,
          "sidestepSpeed": 0.05, "sidestepFrames": 10, "sidestepCancelFrame": 5,
          "rollSpeed": 0.05, "rollFrames": 10,
          "bodyRadius": 0.3,
          "standHurtCapsules": [], "crouchHurtCapsules": [], "downHurtCapsules": [],
          "moves": [%MOVE%]
        }
        """;

    private const string MinimalMoveJson = """
        {
          "name": "m", "command": "P", "kind": "Strike", "posture": "Stand",
          "startup": 5, "active": 2, "recovery": 5, "tracking": 2, "height": "High",
          "damage": 10, "counterDamage": 10, "hitstun": 10, "counterHitstun": 10, "blockstun": 5,
          "hitstop": 3, "knockdown": false, "counterKnockdown": false, "hitsDown": false, "pushback": 0.1,
          "motion": [], "windows": []
        }
        """;

    private static CharacterData LoadWithMove(string moveJson) =>
        GameDataLoader.LoadCharacter(ToStream(MinimalCharacterJson.Replace("%MOVE%", moveJson)));

    [Fact]
    public void LoadsTheCommittedBoxCharacter()
    {
        using var stream = File.OpenRead(FindDataFile("data/characters/box.json"));
        var character = GameDataLoader.LoadCharacter(stream);

        Assert.Equal("box", character.Name);
        Assert.Equal(5, character.Moves.Count);
        Assert.Single(character.StandHurtCapsules);
    }

    [Fact]
    public void LoadsTheCommittedDefaultStage()
    {
        using var stream = File.OpenRead(FindDataFile("data/stages/default.json"));
        var stage = GameDataLoader.LoadStage(stream);

        Assert.Equal(RingShape.Square, stage.Shape);
        Assert.Equal(4, stage.Edges.Count);
    }

    [Fact]
    public void LoadsTheCommittedRules()
    {
        using var stream = File.OpenRead(FindDataFile("data/rules.json"));
        var rules = GameDataLoader.LoadRules(stream);

        Assert.Equal((byte)2, rules.RoundsToWin);
        Assert.False(rules.Training);
    }

    [Fact]
    public void LoadsTheCommittedTrainingRules()
    {
        using var stream = File.OpenRead(FindDataFile("data/rules-training.json"));
        var rules = GameDataLoader.LoadRules(stream);

        Assert.True(rules.Training);
    }

    [Fact]
    public void UnknownPropertyThrows()
    {
        const string json = """{ "name": "x", "unknownField": 1 }""";
        Assert.Throws<GameDataFormatException>(() => GameDataLoader.LoadRules(ToStream(json)));
    }

    [Fact]
    public void MissingRequiredPropertyThrows()
    {
        const string json = """{ "roundTimeSeconds": 60 }""";
        Assert.Throws<GameDataFormatException>(() => GameDataLoader.LoadRules(ToStream(json)));
    }

    [Fact]
    public void MalformedCommandThrows()
    {
        var move = MinimalMoveJson.Replace("\"command\": \"P\"", "\"command\": \"9P\"");
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void CommandWithUpDirectionThrows()
    {
        var move = MinimalMoveJson.Replace("\"command\": \"P\"", "\"command\": \"8P\"");
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void RisingAttackWithGuardCommandThrows()
    {
        var move = MinimalMoveJson
            .Replace("\"kind\": \"Strike\"", "\"kind\": \"RisingAttack\"")
            .Replace("\"command\": \"P\"", "\"command\": \"G\"");
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void TrackingGreaterThanStartupThrows()
    {
        var move = MinimalMoveJson.Replace("\"tracking\": 2", "\"tracking\": 99");
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void OverlappingWindowsThrow()
    {
        const string windows = "\"windows\": [{\"from\":5,\"to\":6,\"hit\":[],\"hurt\":[]},{\"from\":6,\"to\":7,\"hit\":[],\"hurt\":[]}]";
        var move = MinimalMoveJson.Replace("\"windows\": []", windows);
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void HitOutsideActiveFramesThrows()
    {
        const string windows = "\"windows\": [{\"from\":7,\"to\":7,\"hit\":[{\"a\":[0,0,0],\"r\":0.1}],\"hurt\":[]}]";
        var move = MinimalMoveJson.Replace("\"windows\": []", windows);
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void ThrowWithoutThrowRangeThrows()
    {
        var move = MinimalMoveJson.Replace("\"kind\": \"Strike\"", "\"kind\": \"Throw\"");
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void ThrowWithHitWindowThrows()
    {
        const string windows = "\"windows\": [{\"from\":5,\"to\":5,\"hit\":[{\"a\":[0,0,0],\"r\":0.1}],\"hurt\":[]}], \"throwRange\": 1.0";
        var move = MinimalMoveJson
            .Replace("\"kind\": \"Strike\"", "\"kind\": \"Throw\"")
            .Replace("\"windows\": []", windows);
        Assert.Throws<GameDataFormatException>(() => LoadWithMove(move));
    }

    [Fact]
    public void ThrowWithValidRangeSucceeds()
    {
        var move = MinimalMoveJson
            .Replace("\"kind\": \"Strike\"", "\"kind\": \"Throw\"")
            .Replace("\"pushback\": 0.1", "\"pushback\": 0.1, \"throwRange\": 1.0");
        var character = LoadWithMove(move);

        Assert.Equal(Godot3dFighter.Core.Math.Fix16.FromDecimal(1.0m), character.Moves[0].ThrowRange);
    }

    [Fact]
    public void CapsuleWithoutBUsesAAsBothEndpoints()
    {
        const string windows = "\"windows\": [{\"from\":5,\"to\":6,\"hit\":[{\"a\":[0.1,0.2,0.3],\"r\":0.5}],\"hurt\":[]}]";
        var move = MinimalMoveJson.Replace("\"windows\": []", windows);
        var character = LoadWithMove(move);

        var capsule = character.Moves[0].Windows[0].Hit[0];
        Assert.Equal(capsule.A, capsule.B);
    }

    [Fact]
    public void TooManyMovesThrows()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 129; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(MinimalMoveJson);
        }

        Assert.Throws<GameDataFormatException>(() => LoadWithMove(sb.ToString().Replace("[", string.Empty).Replace("]", string.Empty)));
    }

    [Fact]
    public void StageSizeOfZeroThrows()
    {
        const string json = """
            { "name": "x", "shape": "Circle", "size": 0, "edges": ["RingOut"], "startDistance": 1.0 }
            """;
        Assert.Throws<GameDataFormatException>(() => GameDataLoader.LoadStage(ToStream(json)));
    }

    [Fact]
    public void StageEdgeCountMismatchThrows()
    {
        const string json = """
            { "name": "x", "shape": "Square", "size": 5.0, "edges": ["RingOut"], "startDistance": 1.0 }
            """;
        Assert.Throws<GameDataFormatException>(() => GameDataLoader.LoadStage(ToStream(json)));
    }

    [Fact]
    public void StageStartDistanceTooLargeThrows()
    {
        const string json = """
            { "name": "x", "shape": "Circle", "size": 2.0, "edges": ["RingOut"], "startDistance": 5.0 }
            """;
        Assert.Throws<GameDataFormatException>(() => GameDataLoader.LoadStage(ToStream(json)));
    }

    [Fact]
    public void RulesWithSeedZeroThrows()
    {
        const string json = """
            {
              "roundTimeSeconds": 60, "roundsToWin": 2, "maxRounds": 5, "initialHealth": 200,
              "introFrames": 1, "roundEndFrames": 1, "techFrames": 1, "techRecoveryFrames": 1,
              "downMinFrames": 1, "downMaxFrames": 1, "riseFrames": 1,
              "throwEscapeFrames": 1, "throwEscapeRecoveryFrames": 1, "throwEscapeDistance": 1.0,
              "wallStunFrames": 1, "seed": 0, "training": false
            }
            """;
        Assert.Throws<GameDataFormatException>(() => GameDataLoader.LoadRules(ToStream(json)));
    }

    [Fact]
    public void RulesWithMaxRoundsBelowRequiredMinimumThrows()
    {
        const string json = """
            {
              "roundTimeSeconds": 60, "roundsToWin": 3, "maxRounds": 4, "initialHealth": 200,
              "introFrames": 1, "roundEndFrames": 1, "techFrames": 1, "techRecoveryFrames": 1,
              "downMinFrames": 1, "downMaxFrames": 1, "riseFrames": 1,
              "throwEscapeFrames": 1, "throwEscapeRecoveryFrames": 1, "throwEscapeDistance": 1.0,
              "wallStunFrames": 1, "seed": 1, "training": false
            }
            """;
        Assert.Throws<GameDataFormatException>(() => GameDataLoader.LoadRules(ToStream(json)));
    }

    [Fact]
    public void NullStreamThrows()
    {
        Assert.Throws<ArgumentNullException>(() => GameDataLoader.LoadRules(null!));
    }

    private static string FindDataFile(string relativePath)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.Combine(dir, "..");
        }

        throw new FileNotFoundException(relativePath);
    }
}
