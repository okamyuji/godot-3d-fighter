# 格闘コアのデータ構造

この文書は、格闘コア（`Core`）が持つ型、定数、技データの形式を定めます。試合の規則とフレーム処理の手順は`02-match-rules.md`、画面と主要導線の走破は`03-screens-and-e2e.md`に分けてあります。ADRと矛盾する記述があれば、ADRが正です。

## 目的と範囲

`Core`は「試合状態と2人分の入力から、次のフレームの試合状態を作る」ライブラリです。Godotの型を参照せず、`float`と`double`も使いません。描画、音、画面遷移、入力機器の読み取りはGame側が担当します。

## 定数と上限

定数は`src/Core/Limits.cs`の`public static class Limits`に置きます。値を変える時は、この表も合わせて直してください。対戦のバランスに関わる値は定数にせず、`Rules`と`CharacterData`のデータ項目にします。

| 名前 | 値 | 理由 |
|---|---|---|
| `FramesPerSecond` | 60 | 固定のフレーム更新。フレームデータの単位 |
| `InputBufferFrames` | 64 | 最長のコマンド判定（45フレーム）を収める。2の冪で添字が`& 63`になる（ADR-0003、ADR-0017） |
| `PlayerCount` | 2 | 1対1 |
| `CommandWindowFrames` | 10 | コマンドの方向どうしの間に許す最大フレーム数（ADR-0017） |
| `SimultaneousPressFrames` | 2 | 同時押しとみなすボタンの押下の差（ADR-0017） |
| `BufferFrames` | 5 | 先行入力を受け付けるフレーム数（ADR-0017） |
| `TapFrames` | 6 | 下方向を短く入れて横移動になる上限（ADR-0015） |
| `MaxCommandDirections` | 4 | コマンドの方向の数の上限 |
| `MaxCommandButtons` | 3 | コマンドのボタンの数の上限 |
| `MaxHitCapsulesPerWindow` | 4 | 1判定区間の攻撃判定の上限（ADR-0006） |
| `MaxHurtCapsulesPerWindow` | 8 | 1判定区間、1姿勢のやられ判定の上限（ADR-0006） |
| `MaxWindowsPerMove` | 16 | 1技あたりの判定区間の上限 |
| `MaxMovesPerCharacter` | 128 | 技番号を`byte`に収める |
| `MaxMotionSegmentsPerMove` | 8 | 1技あたりの移動区間の上限 |
| `MaxRingEdges` | 8 | リングの縁の数の上限（正八角形） |
| `PositionLimitMeters` | 1024 | 座標の絶対値の上限。距離の二乗が`long`に収まる（ADR-0001、ADR-0002） |
| `SinTableSize` | 1024 | 角度の刻み約0.35度（ADR-0014） |
| `AtanTableSize` | 257 | atan2の比の刻み1/256。比1を含むため添字0から256（ADR-0014） |
| `NoMove` | 255 | 技を出していないことを表す技番号 |

## プロジェクト配置

```text
godot-3d-fighter/
  Godot3dFighter.csproj      Game側。Godot.NET.Sdk/4.7.2。src/、tests/、tools/を除外
  Godot3dFighter.sln
  Directory.Build.props      アナライザと警告即エラーの共通設定
  .editorconfig
  .githooks/pre-commit
  .config/dotnet-tools.json
  CLAUDE.md
  project.godot
  game/                      Game側のC#（画面、FixConvert、InputMapper、E2eRunner）
  scenes/                    Godotのシーン
  data/
    rules.json
    rules-training.json
    characters/<name>.json
    stages/<name>.json
  src/Core/
    Core.csproj              Microsoft.NET.Sdk、net10.0、CheckForOverflowUnderflow=true
    BannedSymbols.txt
    Limits.cs
    Math/  Fix16.cs Vec3Fix.cs Angle16.cs IntMath.cs SinTable.g.cs AtanTable.g.cs Trig.cs
    Input/ InputFrame.cs InputBuffer.cs Command.cs CommandParser.cs
    Data/  CharacterData.cs MoveData.cs MoveKind.cs Posture.cs HitHeight.cs ThrowSide.cs HitWindow.cs
           MotionSegment.cs HitCapsule.cs RingShape.cs EdgeKind.cs StageData.cs Rules.cs
           GameDataLoader.cs GameDataFormatException.cs JsonDtos.cs JsonContext.cs
    State/ StateKind.cs PlayerFlags.cs HitKind.cs RoundPhase.cs RoundEndReason.cs
           PlayerState.cs MatchState.cs MatchContext.cs
    Sim/   MatchSimulator.cs ActionSelector.cs Movement.cs BodyPush.cs Facing.cs CameraSide.cs
           Collision.cs RingBounds.cs HitResolver.cs ThrowResolver.cs WakeUp.cs RoundResolver.cs Xorshift32.cs StateHash.cs
  tests/Core.Tests/          xUnit v3
  tests/e2e/flows.json       主要導線の一覧
  tests/e2e/scenarios/*.json 走破シナリオ
  tests/e2e/data/*.json      走破シナリオ専用の規則とステージ
  tools/doclint.sh
  tools/GenTables/           sin表とatan表の生成
```

## 型の定義

すべての型は`namespace Godot3dFighter.Core.*`に置きます。`Math/`、`Input/`、`State/`の構造体は`readonly`または`[StructLayout(LayoutKind.Sequential, Pack = 1)]`で、参照型の項目を持たない形です。`Data/`の型のうち`HitCapsule`は`readonly record struct`で、それ以外と`MatchContext`はクラスです。列挙型はすべて基底型を`byte`にします。

### Fix16（ADR-0001）

```csharp
public readonly record struct Fix16(int Raw)
{
    public const int Shift = 16;
    public static readonly Fix16 Zero = new(0);
    public static readonly Fix16 One = new(1 << Shift);
    public static Fix16 FromInt(int v);            // checked。v << 16
    public static Fix16 FromDecimal(decimal v);    // decimal.Round(v * 65536m, 0, MidpointRounding.AwayFromZero)
    public static Fix16 operator +(Fix16 a, Fix16 b); // checked
    public static Fix16 operator -(Fix16 a, Fix16 b); // checked
    public static Fix16 operator *(Fix16 a, Fix16 b); // (int)(((long)a.Raw * b.Raw) >> 16)
    public static Fix16 operator /(Fix16 a, Fix16 b); // (int)(((long)a.Raw << 16) / b.Raw)
    public Fix16 Abs();
    public Fix16 Clamp(Fix16 min, Fix16 max);
    public long RawSquared();                      // (long)Raw * Raw。Q32.32
}
```

乗算は負の無限大方向、除算はゼロ方向へ丸まります。加減算と`FromInt`は`checked`で、あふれは`OverflowException`です。乗算と除算の結果を`int`へ戻すキャストも`checked`で、収まらなければ同じ例外になります。

### Vec3FixとIntMath（ADR-0002、ADR-0015）

`Vec3Fix`は`X`、`Y`、`Z`の3つの`Fix16`を持つ`readonly record struct`で、12バイトです。加減算、スカラー倍、`Clamp`、`HorizontalDistanceSquared(Vec3Fix other)`（XとZだけの距離の二乗を`long`で返す）、`HorizontalDot(Vec3Fix n)`（XとZの内積を`long`のQ32.32で返す）を持ちます。

`IntMath.Sqrt(long v)`は0以上の`long`の平方根を、小数点以下を切り捨てた`long`で返します。負の値は`ArgumentOutOfRangeException`です。水平距離は`Fix16(checked((int)IntMath.Sqrt(distanceSquared)))`で求まります。Q32.32の平方根はQ16.16になるためです。

### Angle16とTrig（ADR-0014）

`Angle16`は`ushort`を包む`readonly record struct`です。加減算は`ushort`の桁あふれをそのまま使う（`unchecked`）ので、1周が65536になります。`Trig.Sin(Angle16)`と`Trig.Cos(Angle16)`は`Fix16`を返し、`Trig.Atan2(Fix16 y, Fix16 x)`は`Angle16`を返す関数です。`Trig.Rotate(Vec3Fix local, Angle16 facing)`は、キャラ座標（前が+X、右が+Z、上が+Y）の点をワールド座標の向きへ回します。`Trig.Direction(Angle16 a)`は向きベクトル`(cos a, 0, -sin a)`を返します。

```text
world.X = local.X * cos(θ) + local.Z * sin(θ)
world.Y = local.Y
world.Z = -local.X * sin(θ) + local.Z * cos(θ)
```

### InputFrameとInputBuffer（ADR-0003）

```csharp
public readonly record struct InputFrame(byte Bits)
{
    public const byte Up = 1, Down = 2, Left = 4, Right = 8, Punch = 16, Kick = 32, Guard = 64;
    public bool IsNormalized => (Bits & 0x80) == 0 && (Bits & 3) != 3 && (Bits & 12) != 12;
    public bool Has(byte flag);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InputBuffer
{
    [InlineArray(Limits.InputBufferFrames)] private struct Slots { private byte _e0; }
    private Slots _slots;   // 64バイト
    private byte _head;     // 次に書く位置
    public InputBuffer Push(InputFrame f);        // 新しい値を返す
    public InputFrame At(int framesAgo);          // 0が最新。64以上はArgumentOutOfRangeException
}
```

`InputFrame`の左右は画面上の絶対的な左右です。コマンド判定で前後へ読み替えます（ADR-0016、ADR-0017）。

### 列挙型

| 型 | 値 |
|---|---|
| `StateKind` | `Idle`、`Walk`、`BackWalk`、`Crouch`、`Guard`、`CrouchGuard`、`SidestepIn`、`SidestepOut`、`Dash`、`Backdash`、`Attack`、`Throwing`、`Thrown`、`ThrowEscape`、`Blockstun`、`CrouchBlockstun`、`Hitstun`、`WallStun`、`Down`、`Tech`、`RollIn`、`RollOut`、`Rise`、`Dead` |
| `PlayerFlags` | `[Flags]`。`HasHitThisMove`=1、`DownHitTaken`=2、`ThrowEscapeTried`=4、`WallHitTaken`=8、`TechQueued`=16 |
| `HitKind` | `None`、`Hit`、`CounterHit`、`WallHit`、`CounterWallHit`、`Guarded`、`Thrown`、`ThrowEscaped` |
| `RoundPhase` | `Intro`、`Fight`、`RoundEnd`、`MatchEnd` |
| `RoundEndReason` | `None`、`KnockOut`、`RingOut`、`TimeUp` |
| `MoveKind` | `Strike`、`Throw`、`RisingAttack` |
| `Posture` | `Stand`、`Crouch` |
| `HitHeight` | `High`、`Mid`、`Low` |
| `ThrowSide` | `Front`、`Back` |
| `RingShape` | `Circle`、`Square`、`Octagon` |
| `EdgeKind` | `RingOut`、`Wall` |

### PlayerState（ADR-0005）

サイズの大きい項目から順に並べます。合計は105バイトです。

| 項目 | 型 | バイト | 内容 |
|---|---|---|---|
| `Inputs` | `InputBuffer` | 65 | 直近64フレームの入力と書き込み位置 |
| `Position` | `Vec3Fix` | 12 | 足元の座標（m） |
| `Velocity` | `Vec3Fix` | 12 | このフレームの移動量（m） |
| `Health` | `int` | 4 | 残り体力 |
| `StateFrame` | `ushort` | 2 | 現在の状態に入ってからの経過フレーム。0始まり |
| `Facing` | `Angle16` | 2 | 向き |
| `StunFrames` | `ushort` | 2 | 硬直の残りフレーム |
| `HitstopFrames` | `byte` | 1 | ヒットストップの残りフレーム |
| `State` | `StateKind` | 1 | 状態 |
| `CurrentMove` | `byte` | 1 | 技番号。`Limits.NoMove`は技なし |
| `Flags` | `PlayerFlags` | 1 | 技とダウンの間だけ使う印 |
| `LastHitKind` | `HitKind` | 1 | このフレームに受けた結果。毎フレームの最初に`None`へ戻す |
| `Slot` | `byte` | 1 | 0がP1、1がP2 |

### MatchState（ADR-0005、ADR-0016、ADR-0022）

| 項目 | 型 | バイト | 内容 |
|---|---|---|---|
| `Players` | `PlayerState`×2（`[InlineArray(2)]`） | 210 | 2人分の状態 |
| `FrameNumber` | `uint` | 4 | 試合開始からのフレーム数 |
| `RngState` | `uint` | 4 | xorshift32の状態。初期値は`Rules.Seed` |
| `RoundTimerFrames` | `ushort` | 2 | ラウンドの残りフレーム |
| `PhaseFrames` | `ushort` | 2 | 現在の段階の経過フレーム |
| `CameraYaw` | `Angle16` | 2 | 画面の右方向の角度 |
| `RoundWins` | `byte`×2（`[InlineArray(2)]`） | 2 | 各プレイヤーの勝ち数 |
| `RoundNumber` | `byte` | 1 | 1始まり |
| `Phase` | `RoundPhase` | 1 | 段階 |
| `LastRoundReason` | `RoundEndReason` | 1 | 直前のラウンドの決着の理由 |
| `LastRoundWinners` | `byte` | 1 | 直前のラウンドの勝者。P1がビット0、P2がビット1 |

合計は230バイトです。`Unsafe.SizeOf<MatchState>()`がこの値と一致することをテストで確かめます（C-08）。

### MatchContext

`Step`に渡す読み取り専用の参照データで、試合状態には含めません。

| 項目 | 型 | 内容 |
|---|---|---|
| `Characters` | `CharacterData`×2 | 各プレイヤーのキャラデータ |
| `Stage` | `StageData` | ステージ |
| `Rules` | `Rules` | 試合の規則 |

### CharacterData

| 項目 | 型 | 内容 |
|---|---|---|
| `Name` | `string` | 表示名 |
| `WalkSpeed` | `Fix16` | 前歩きの1フレームの移動量（m） |
| `BackWalkSpeed` | `Fix16` | 後ろ歩きの1フレームの移動量（m） |
| `DashSpeed`、`DashFrames` | `Fix16`、`ushort` | 前ダッシュの速さと長さ |
| `BackdashSpeed`、`BackdashFrames` | `Fix16`、`ushort` | 後ろダッシュの速さと長さ |
| `DashCancelFrames` | `ushort` | ダッシュの最後の、行動を受け付けるフレーム数 |
| `SidestepSpeed`、`SidestepFrames` | `Fix16`、`ushort` | 横移動の速さと長さ |
| `SidestepCancelFrame` | `ushort` | 横移動で行動を受け付け始める経過フレーム |
| `RollSpeed`、`RollFrames` | `Fix16`、`ushort` | 転がりと受け身の移動の速さ、転がりの長さ |
| `BodyRadius` | `Fix16` | 押し合いの半径（m） |
| `StandHurtCapsules` | `HitCapsule`の配列（最大8） | 立ちのやられ判定 |
| `CrouchHurtCapsules` | `HitCapsule`の配列（最大8） | しゃがみのやられ判定 |
| `DownHurtCapsules` | `HitCapsule`の配列（最大8） | ダウンと転がりのやられ判定 |
| `Moves` | `MoveData`の配列（最大128） | 技表。配列の添字が技番号 |

### MoveData

| 項目 | 型 | 内容 |
|---|---|---|
| `Name` | `string` | 技名 |
| `Command` | `string` | コマンド（ADR-0017）。`RisingAttack`では`P`か`K`だけ |
| `Kind` | `MoveKind` | 打撃、投げ、起き上がり攻撃 |
| `Posture` | `Posture` | 技の間の姿勢（ADR-0018） |
| `Startup` | `ushort` | 発生フレーム。経過フレームがこの値になったフレームから判定が出る |
| `Active` | `ushort` | 持続フレーム数 |
| `Recovery` | `ushort` | 硬直フレーム数 |
| `Tracking` | `ushort` | 相手の方を向き直す経過フレームの上限。0以上`Startup`以下（ADR-0015） |
| `Height` | `HitHeight` | 段（ADR-0018） |
| `Damage`、`CounterDamage` | `int` | 通常とカウンターヒットのダメージ（ADR-0020） |
| `Hitstun`、`CounterHitstun` | `ushort` | 通常とカウンターヒットの硬直 |
| `Blockstun` | `ushort` | ガードされた時の相手の硬直 |
| `Hitstop` | `byte` | 命中とガードで2人に掛かるヒットストップ |
| `Knockdown`、`CounterKnockdown` | `bool` | 通常とカウンターヒットでダウンさせるか |
| `HitsDown` | `bool` | ダウン中の相手に当たるか（ADR-0021） |
| `Pushback` | `Fix16` | 命中とガードで相手を押す距離（m） |
| `Motion` | `MotionSegment`の配列（最大8） | 技の間の前後移動。`From`、`To`（経過フレーム、両端を含む）、`ForwardPerFrame`（m） |
| `Windows` | `HitWindow`の配列（最大16） | 判定区間。`From`、`To`（経過フレーム、両端を含む）、`Hit`（最大4）、`Hurt`（最大8） |
| `ThrowRange` | `Fix16` | 投げの届く水平距離（m）。`Throw`だけ |
| `ThrowSide` | `ThrowSide` | 正面投げか背中からの投げか。`Throw`だけ |
| `ThrowEndOffset` | `Vec3Fix` | 投げの後の相手の位置。投げた側のキャラ座標。`Throw`だけ |

`HitCapsule`は`A`と`B`（`Vec3Fix`、キャラ座標の線分の両端）と`Radius`（`Fix16`）です。両端が同じなら球です。重なりの計算は`02-match-rules.md`の「カプセルの重なり」にあります。

### StageDataとRules

| `StageData`の項目 | 型 | 内容 |
|---|---|---|
| `Name` | `string` | 表示名 |
| `Shape` | `RingShape` | リングの形（ADR-0002） |
| `Size` | `Fix16` | 円の半径、または中心から辺までの距離（m） |
| `Edges` | `EdgeKind`の配列 | 各辺の縁の種類。円は1、正方形は4、正八角形は8要素 |
| `StartDistance` | `Fix16` | 開始時の2人の距離（m） |

| ステージのファイル | 形 | 大きさ | 縁 | 開始時の距離 |
|---|---|---|---|---|
| `data/stages/default.json` | 正方形 | 5.0 | すべてリングアウト | 2.0 |
| `data/stages/arena.json` | 正八角形 | 6.0 | すべて壁 | 2.0 |
| `data/stages/cliff.json` | 正方形 | 5.0 | 辺0と辺2が壁、辺1と辺3がリングアウト | 2.0 |

| `Rules`の項目 | 型 | `data/rules.json`の値 | 内容 |
|---|---|---|---|
| `RoundTimeSeconds` | `ushort` | 60 | ラウンドの時間（秒） |
| `RoundsToWin` | `byte` | 2 | 先取ラウンド数 |
| `MaxRounds` | `byte` | 5 | 最大ラウンド数（ADR-0022） |
| `InitialHealth` | `int` | 200 | 体力の初期値 |
| `IntroFrames` | `ushort` | 90 | ラウンド開始の演出の長さ |
| `RoundEndFrames` | `ushort` | 150 | ラウンド決着の演出の長さ |
| `TechFrames` | `ushort` | 6 | 受け身を受け付けるフレーム数 |
| `TechRecoveryFrames` | `ushort` | 20 | 受け身の長さ |
| `DownMinFrames` | `ushort` | 20 | ダウン後に入力を受け付けないフレーム数 |
| `DownMaxFrames` | `ushort` | 60 | 自動で起き上がるまでのフレーム数 |
| `RiseFrames` | `ushort` | 24 | 起き上がりの長さ |
| `ThrowEscapeFrames` | `ushort` | 10 | 投げ抜けを受け付けるフレーム数 |
| `ThrowEscapeRecoveryFrames` | `ushort` | 20 | 投げ抜けの後の2人の硬直 |
| `ThrowEscapeDistance` | `Fix16` | 1.2 | 投げ抜けの後の2人の水平距離（m） |
| `WallStunFrames` | `ushort` | 40 | 壁やられの長さ（ADR-0024） |
| `Seed` | `uint` | 1 | 乱数の初期値 |
| `Training` | `bool` | false | トレーニングの規則を使うか（ADR-0022） |

`data/rules-training.json`は`Training`だけをtrueにした内容です。

## 技データのJSON

JSONの項目名は型の項目名の先頭を小文字にしたものです。例（`data/characters/box.json`の一部）は次のとおりです。

```json
{
  "name": "box",
  "walkSpeed": 0.05, "backWalkSpeed": 0.04,
  "dashSpeed": 0.12, "dashFrames": 16, "backdashSpeed": 0.1, "backdashFrames": 18, "dashCancelFrames": 4,
  "sidestepSpeed": 0.06, "sidestepFrames": 18, "sidestepCancelFrame": 10,
  "rollSpeed": 0.06, "rollFrames": 24,
  "bodyRadius": 0.3,
  "standHurtCapsules": [ { "a": [0, 0.2, 0], "b": [0, 1.5, 0], "r": 0.3 } ],
  "crouchHurtCapsules": [ { "a": [0, 0.3, 0], "b": [0, 0.8, 0], "r": 0.35 } ],
  "downHurtCapsules": [ { "a": [-0.8, 0.15, 0], "b": [0.8, 0.15, 0], "r": 0.2 } ],
  "moves": [
    {
      "name": "jab", "command": "P", "kind": "Strike", "posture": "Stand",
      "startup": 10, "active": 3, "recovery": 12, "tracking": 4, "height": "High",
      "damage": 12, "counterDamage": 15, "hitstun": 18, "counterHitstun": 22, "blockstun": 10,
      "hitstop": 6, "knockdown": false, "counterKnockdown": false, "hitsDown": false, "pushback": 0.25,
      "motion": [ { "from": 8, "to": 12, "forwardPerFrame": 0.02 } ],
      "windows": [ { "from": 10, "to": 12, "hit": [ { "a": [0.3, 1.3, 0], "b": [0.8, 1.3, 0], "r": 0.12 } ], "hurt": [] } ]
    }
  ]
}
```

実数の項目はDTOで`decimal`として受け、`Fix16.FromDecimal`で変換します。カプセルの`a`と`b`は3要素の配列（X、Y、Z）で、`b`を省くと`a`と同じ点になります。列挙型の項目は値の名前の文字列です。

`GameDataLoader`は次のデータを`GameDataFormatException`にします。

- 上限（`Limits`）を超える配列
- 形に合わないコマンド（ADR-0017）
- 同じ技の中で範囲の重なる判定区間、`From`が`To`より大きい区間
- 攻撃判定を持つ区間が`Startup`から`Startup + Active - 1`の外にはみ出る技
- `Tracking`が`Startup`より大きい技
- `Throw`で`ThrowRange`が0以下、または`Windows`に攻撃判定を持つ技
- `RisingAttack`でコマンドが`P`か`K`以外の技
- 未知の項目、欠けた必須項目
- `StageData`で、`Size`が0以下、`Edges`の要素数が形と合わない、`StartDistance`の半分が`Size`以上
- `Rules`で、`RoundTimeSeconds`が1未満か1092を超える（60倍して`ushort`に収まらない）、`RoundsToWin`が1未満、`MaxRounds`が`RoundsToWin * 2 - 1`未満、`InitialHealth`が1未満、`DownMinFrames`が`DownMaxFrames`を超える、`Seed`が0

`Down`、`Rise`、`Tech`、横移動、ダッシュなどの長さを決める項目は1以上です。
