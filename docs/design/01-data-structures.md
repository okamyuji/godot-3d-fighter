# 格闘コアのデータ構造

この文書は、格闘コア（`Core`）のデータ構造と、それを実装する順序を定めます。ADR-0001からADR-0014で決めた内容を、実装者がそのまま型とテストに落とせる粒度で書いたものです。ADRと矛盾する記述があれば、ADRが正です。

## 目的と範囲

`Core`は「試合状態と2人分の入力から、次のフレームの試合状態を作る」ライブラリです。Godotの型を参照せず、`float`と`double`も使いません。描画、音、画面遷移、入力機器の読み取りはGame側が担当します。

この文書の範囲は、箱同士の格闘コア（READMEの手順3）に必要な型、技データの形式、状態遷移、フレーム処理の順序、テストの対応表です。3D化、カメラ、CPU対戦、ネット対戦は扱いません。

## 定数と上限

定数は`src/Core/Limits.cs`に`public static class Limits`として置きます。値を変える時はこの表も直します。

| 名前 | 値 | 理由 |
|---|---|---|
| `FramesPerSecond` | 60 | 固定タイムステップ。フレームデータの単位 |
| `InputBufferFrames` | 64 | コマンド猶予は1秒以内。2の冪で添字が`& 63`になる（ADR-0003） |
| `PlayerCount` | 2 | 1対1 |
| `MaxHitSpheresPerWindow` | 4 | 1フレームの攻撃判定の上限（ADR-0006） |
| `MaxHurtSpheresPerWindow` | 8 | 1フレームのやられ判定の上限（ADR-0006） |
| `MaxWindowsPerMove` | 16 | 1技あたりの判定区間の上限 |
| `MaxMovesPerCharacter` | 128 | 技番号を`byte`に収める |
| `MaxMotionSegmentsPerMove` | 8 | 1技あたりの移動区間の上限 |
| `PositionLimitMeters` | 1024 | 距離の二乗が`long`に収まる範囲（ADR-0001、ADR-0002） |
| `SinTableSize` | 1024 | 角度の刻み約0.35度（ADR-0014） |
| `AtanTableSize` | 256 | atan2の比の刻み1/256（ADR-0014） |
| `CommandWindowFrames` | 10 | コマンドの隣り合う要素の間に許す最大フレーム数 |
| `MaxCommandElements` | 4 | 1コマンドの要素数の上限（例「236P」は4要素） |
| `NoMove` | 255 | 「技を出していない」を表す技番号 |

試合の規則（ラウンド時間、先取ラウンド数、初期体力）はデータ項目で、`data/rules.json`に持ちます。

## プロジェクト配置

```text
godot-3d-fighter/
  Godot3dFighter.csproj      Game側。Godot.NET.Sdk/4.7.2。src/、tests/、tools/を除外
  Godot3dFighter.sln
  Directory.Build.props      アナライザと警告即エラーの共通設定
  .editorconfig
  project.godot
  game/                      Game側のC#（FixConvert、InputMapper、E2eRunner、シーン用スクリプト）
  scenes/                    Godotのシーン
  data/
    rules.json
    characters/<name>.json
    stages/<name>.json
  src/Core/
    Core.csproj              Microsoft.NET.Sdk、net10.0、CheckForOverflowUnderflow=true
    BannedSymbols.txt
    Limits.cs
    Math/  Fix16.cs Vec3Fix.cs Angle16.cs IntMath.cs SinTable.g.cs AtanTable.g.cs Trig.cs
    Input/ InputFrame.cs InputBuffer.cs CommandParser.cs
    Data/  MoveTable.cs MoveData.cs HitSphere.cs StageData.cs Rules.cs MoveTableLoader.cs JsonContext.cs
    State/ StateKind.cs PlayerState.cs MatchState.cs MatchContext.cs RoundPhase.cs
    Sim/   MatchSimulator.cs Collision.cs RingOut.cs Xorshift32.cs StateHash.cs
  tests/Core.Tests/          xUnit v3
  tests/e2e/flows.json       主要導線の一覧
  tests/e2e/scenarios/*.json 走破シナリオ
  tools/doclint.sh
  tools/GenTables/           sin表とatan表の生成
```

## 型の定義

すべての型は`namespace Godot3dFighter.Core.*`に置きます。`Math/`、`Input/`、`State/`の構造体は`readonly`または`[StructLayout(LayoutKind.Sequential, Pack = 1)]`で、参照型の項目を持たない形です。`Data/`の型と`MatchContext`はクラスで、文字列と配列を持ちます。

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

### Vec3Fix（ADR-0002）

`X`、`Y`、`Z`の3つの`Fix16`を持つ`readonly record struct`です。12バイトです。加減算、スカラー倍、`Clamp`、`HorizontalDistanceSquared(Vec3Fix other)`（XとZだけの距離の二乗を`long`で返す）を持ちます。

### Angle16とTrig（ADR-0014）

`Angle16`は`ushort`を包む`readonly record struct`です。加減算は`ushort`の桁あふれをそのまま使う（`unchecked`）ので、1周が65536になります。`Trig.Sin(Angle16)`と`Trig.Cos(Angle16)`は`Fix16`を返し、`Trig.Atan2(Fix16 y, Fix16 x)`は`Angle16`を返す関数です。`Trig.Rotate(Vec3Fix local, Angle16 facing)`は、キャラ座標（前が+X、右が+Z、上が+Y）の点をワールド座標の向きへ回します。

```text
world.X = local.X * cos(θ) + local.Z * sin(θ)
world.Y = local.Y
world.Z = -local.X * sin(θ) + local.Z * cos(θ)
```

### InputFrame（ADR-0003）

```csharp
public readonly record struct InputFrame(byte Bits)
{
    public const byte Up = 1, Down = 2, Left = 4, Right = 8, Punch = 16, Kick = 32, Guard = 64;
    public bool IsNormalized => (Bits & 0x80) == 0 && (Bits & 3) != 3 && (Bits & 12) != 12;
    public bool Has(byte flag);
}
```

`MatchSimulator.Step`は`IsNormalized`が偽の入力を受け取ると`ArgumentException`を投げます。正規化はGame側の`InputMapper`が行います。

### InputBuffer（ADR-0003）

```csharp
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

### PlayerState（ADR-0005）

サイズの大きい項目から順に並べます。合計は104バイトです。

| 項目 | 型 | バイト | 内容 |
|---|---|---|---|
| `Inputs` | `InputBuffer` | 65 | 直近64フレームの入力と書き込み位置 |
| `Position` | `Vec3Fix` | 12 | 足元の座標（m） |
| `Velocity` | `Vec3Fix` | 12 | 1フレームあたりの移動量（m） |
| `Health` | `int` | 4 | 残り体力 |
| `MoveFrame` | `ushort` | 2 | 現在の技の経過フレーム。0始まり |
| `Facing` | `Angle16` | 2 | 相手への向き |
| `StunFrames` | `ushort` | 2 | ヒットまたはガード硬直の残りフレーム |
| `HitstopFrames` | `byte` | 1 | ヒットストップの残りフレーム |
| `State` | `StateKind` | 1 | 状態遷移の現在状態 |
| `CurrentMove` | `byte` | 1 | 技番号。`Limits.NoMove`は技なし |
| `HasHitThisMove` | `byte` | 1 | 0または1。現在の技で相手に当たったか |
| `Slot` | `byte` | 1 | 0がP1、1がP2 |

### MatchState（ADR-0005）

| 項目 | 型 | バイト | 内容 |
|---|---|---|---|
| `Players` | `PlayerState`×2（`[InlineArray(2)]`） | 208 | 2人分の状態 |
| `FrameNumber` | `uint` | 4 | 試合開始からのフレーム数 |
| `RngState` | `uint` | 4 | xorshift32の状態。初期値は`Rules.Seed`で、0にはならない |
| `RoundTimerFrames` | `ushort` | 2 | ラウンドの残りフレーム |
| `PhaseFrames` | `ushort` | 2 | 現在の段階の経過フレーム |
| `RoundWins` | `byte`×2（`[InlineArray(2)]`） | 2 | 各プレイヤーの先取数 |
| `RoundNumber` | `byte` | 1 | 1始まり |
| `Phase` | `RoundPhase` | 1 | `Intro`、`Fight`、`RoundEnd`、`MatchEnd` |

合計は224バイトです。`Unsafe.SizeOf<MatchState>()`がこの値と一致することをテストで確かめます（C-08）。

### MatchContext

`Step`に渡す読み取り専用の参照データです。試合状態には含めません。

| 項目 | 型 | 内容 |
|---|---|---|
| `Characters` | `CharacterData`×2 | 各プレイヤーのキャラデータ（技表、やられ判定、歩行速度） |
| `Stage` | `StageData` | リング半径 |
| `Rules` | `Rules` | ラウンド時間（秒）、先取ラウンド数、初期体力 |

### 技データ（ADR-0004、ADR-0006）

`CharacterData`は次を持ちます。

| 項目 | 型 | 内容 |
|---|---|---|
| `Name` | `string` | 表示名 |
| `WalkSpeed` | `Fix16` | 前後歩行の1フレームの移動量（m） |
| `SidestepSpeed` | `Fix16` | 横移動の1フレームの移動量（m） |
| `IdleHurtSpheres` | `HitSphere`の配列（最大8） | 技を出していない時のやられ判定 |
| `Moves` | `MoveData`の配列（最大128） | 技表。配列の添字が技番号 |

`MoveData`は次を持ちます。

| 項目 | 型 | 内容 |
|---|---|---|
| `Name` | `string` | 技名 |
| `Command` | `string` | テンキー表記。数字は6が前、4が後ろ、8が上、2が下。P、K、Gがボタン。`+`は同時押し。例「P」「6P」「236P」「P+K」 |
| `Startup` | `ushort` | 発生フレーム数。技の0フレーム目からこの数だけ経つと攻撃判定が出る |
| `Active` | `ushort` | 持続フレーム数 |
| `Recovery` | `ushort` | 硬直フレーム数 |
| `Damage` | `int` | ダメージ |
| `Hitstop` | `byte` | 命中時に両者に掛かるヒットストップのフレーム数 |
| `Hitstun` | `ushort` | 命中時に相手に掛かる硬直のフレーム数 |
| `Blockstun` | `ushort` | ガード時に相手に掛かる硬直のフレーム数 |
| `Height` | `HitHeight` | `High`、`Mid`、`Low`。ガードの成否に使う |
| `Knockdown` | `bool` | 命中時にダウンさせるか |
| `Pushback` | `Fix16` | 命中またはガード時に相手を後ろへ押す距離（m） |
| `Motion` | `MotionSegment`の配列（最大8） | 技中の移動。`From`、`To`（フレーム）、`ForwardPerFrame`（m） |
| `Windows` | `HitWindow`の配列（最大16） | 判定区間。`From`、`To`（フレーム）、`Hit`（最大4）、`Hurt`（最大8） |

`HitWindow.Hurt`が空の区間では`IdleHurtSpheres`を使います。`HitSphere`は`Center`（`Vec3Fix`、キャラ座標）と`Radius`（`Fix16`）です。

JSONの例（`data/characters/box.json`の一部）は次のとおりです。

```json
{
  "name": "box",
  "walkSpeed": 0.05,
  "sidestepSpeed": 0.04,
  "idleHurtSpheres": [
    { "x": 0, "y": 0.4, "z": 0, "r": 0.4 },
    { "x": 0, "y": 1.2, "z": 0, "r": 0.4 }
  ],
  "moves": [
    {
      "name": "punch",
      "command": "P",
      "startup": 10, "active": 3, "recovery": 15,
      "damage": 15, "hitstop": 6, "hitstun": 18, "blockstun": 8,
      "height": "High", "knockdown": false, "pushback": 0.3,
      "motion": [],
      "windows": [
        { "from": 10, "to": 12, "hit": [ { "x": 0.7, "y": 1.3, "z": 0, "r": 0.25 } ], "hurt": [] }
      ]
    }
  ]
}
```

実数の項目（`walkSpeed`、`x`、`y`、`z`、`r`、`pushback`、`forwardPerFrame`）はDTOで`decimal`として受け、`Fix16.FromDecimal`で変換します。`MoveTableLoader`は上限（`Limits`）を超える配列、未知の項目、欠けた必須項目を`MoveTableFormatException`にします。

### StageDataとRules

`StageData`は`RingRadius`（`Fix16`）と`StartDistance`（`Fix16`、開始時の2人の距離）を持ちます。`Rules`は`RoundTimeSeconds`（`ushort`）、`RoundsToWin`（`byte`）、`InitialHealth`（`int`）、`RiseFrames`（`ushort`、起き上がりのフレーム数）、`Seed`（`uint`、既定1、0は読み込み時に例外）を持ちます。

## 状態遷移

`StateKind`は`byte`の列挙です。

| 状態 | 入る条件 | 出る条件 |
|---|---|---|
| `Idle` | 初期状態。硬直が切れた時 | 入力または被弾 |
| `Walk` | `Idle`で前か後ろの入力 | 入力が無くなる、技の入力、被弾 |
| `Sidestep` | `Idle`または`Walk`で上か下の入力 | 入力が無くなる、技の入力、被弾 |
| `Attack` | コマンドが成立し、`Idle`、`Walk`、`Sidestep`のいずれか | 技の総フレーム（`Startup + Active + Recovery`）が経過。被弾 |
| `Guard` | `Idle`または`Walk`でGを押している | Gを離す、`Guard`中に被弾（`Blockstun`へ） |
| `Blockstun` | `Guard`中に攻撃を受けた | `StunFrames`がゼロ |
| `Hitstun` | ガード不成立で攻撃を受けた（`Knockdown`が偽） | `StunFrames`がゼロ |
| `Down` | `Knockdown`が真の攻撃を受けた。リングアウト | `StunFrames`がゼロで`Rise`へ |
| `Rise` | `Down`から起き上がり | `Rules.RiseFrames`が経過 |
| `Dead` | 体力がゼロ以下 | ラウンド終了まで出ない |

ガードの成否は、攻撃の`Height`と防御側の状態で決めます。`Guard`は`High`と`Mid`を防ぎ、`Low`は防げません。しゃがみガードは箱同士の段階では扱いません。

## フレーム処理の順序

`MatchSimulator.Step(in MatchState state, InputFrame p1, InputFrame p2, in MatchContext ctx)`は、次の順で新しい`MatchState`を作ります。各手順は`Sim/`配下の純粋関数で、状態を書き換えずに新しい値を返します。

1. 入力の検証と保存 両入力の`IsNormalized`を確かめ、各プレイヤーの`Inputs`に`Push`します。
2. 段階の進行 現在の段階（`Phase`）が`Intro`、`RoundEnd`、`MatchEnd`なら経過（`PhaseFrames`）を進め、規定フレームで次の段階へ移します。`Fight`以外では以下を飛ばします。
3. ヒットストップ 残り（`HitstopFrames`）が正なら1減らし、そのプレイヤーの手順4から6を飛ばします。
4. 硬直と技の進行 硬直の残り（`StunFrames`）と技の経過（`MoveFrame`）を進め、状態遷移の「出る条件」を適用します。
5. コマンド判定と状態遷移 判定器（`CommandParser`）が`Inputs`と`Slot`から成立した技を返し、「入る条件」を適用します。同じフレームに複数の技が成立する時は、技番号が大きい方です。
6. 移動 状態（`Walk`、`Sidestep`）と技の`Motion`から`Velocity`を決め、`Position`に加えて`PositionLimitMeters`で`Clamp`します。
7. 向きの更新 両者の足元座標の差から`Facing`を`Atan2`で求めます。距離がゼロなら前のフレームの値を保ちます。
8. 当たり判定 各プレイヤーの現在区間の攻撃判定を`Rotate`でワールド座標にし、相手のやられ判定と`Collision.Overlaps`で調べます。`HasHitThisMove`が1の技は判定しません。
9. 命中の適用 ガードの成否を決め、ダメージ、硬直、ヒットストップ、押し戻し、ダウンを両者に適用します。両者が同じフレームに命中した時は両方に適用する決まりです。
10. 決着の判定 体力ゼロ以下、リングアウト（`RingOut.IsOut`）、`RoundTimerFrames`ゼロの順で調べ、該当すれば`Phase`を`RoundEnd`にし、勝者の`RoundWins`を増やします。時間切れで体力が同じなら両者に加えます。`RoundsToWin`に達したら`MatchEnd`です。
11. フレーム番号の更新 フレーム番号（`FrameNumber`）とラウンドの残り（`RoundTimerFrames`）を更新します。

`StateHash.Compute(in MatchState)`は`MemoryMarshal.AsBytes`で得たバイト列のFNV-1a（64bit）を返します。乗算は`unchecked`です。`Step`は呼びません。

## 常に成り立つ条件とテストの対応

| 番号 | 条件 | ADR | テスト |
|---|---|---|---|
| C-01 | `Fix16`の全演算は整数演算だけで閉じ、同じ入力に同じ結果を返す | 0001 | `Fix16Tests`。乗算の負数丸め、除算のゼロ方向丸め、加算のあふれ、0除算 |
| C-02 | 座標成分の絶対値は1024m以下 | 0002 | `PositionClampTests`。1024を超える速度を与えて`Clamp`される |
| C-03 | リングアウトは距離の二乗の比較で決まり、半径ちょうどはリング内 | 0002 | `RingOutTests`。半径ちょうど、半径+1/65536 |
| C-04 | Coreに入る`InputFrame`は正規化済みで、ビット7は0 | 0003 | `InputFrameTests`。上下同時、左右同時、ビット7で`ArgumentException` |
| C-05 | `InputBuffer`は直近64フレームだけを保持する | 0003 | `InputBufferTests`。65回`Push`して`At(63)`と`At(64)` |
| C-06 | 同じJSONから同じ`MoveTable`。未知と欠損の項目は例外 | 0004 | `MoveTableLoaderTests` |
| C-07 | `Fix16.FromDecimal`は0.5をゼロから遠い方へ丸める | 0004 | `Fix16Tests`。0.5/65536と-0.5/65536 |
| C-08 | `MatchState`に詰め物のバイトが無い（224バイト） | 0005 | `MatchStateLayoutTests` |
| C-09 | 同じ状態と入力から`Step`は同じ状態と同じハッシュを返す | 0005 | `MatchSimulatorTests`。同じ入力列を2回流して比較 |
| C-10 | 球の重なりは距離の二乗の比較で決まり、接触は重なり | 0006 | `HitSphereTests`。半径の和ちょうど、+1/65536 |
| C-11 | 攻撃判定4個、やられ判定8個を超えるデータは読み込み時に例外 | 0006 | `MoveTableLoaderTests` |
| C-12 | `Core`はGodotSharpを参照しない | 0007 | `AssemblyReferenceTests` |
| C-13 | `flows.json`の全導線に対応するシナリオがある | 0008 | `E2eRunner --check-flows` |
| C-14 | `Core`の公開APIに`float`と`double`が現れない | 0009 | `PublicApiTests` |
| C-15 | sin表の代表値と`sin^2 + cos^2`の誤差 | 0014 | `TrigTableTests` |
| C-16 | atan2の代表値 | 0014 | `Atan2Tests` |

## 実装の順序

各項目は「テストを先に書き、失敗を確かめてから実装する」単位です。1項目ごとにコミットします。

1. 土台 共通設定（`Directory.Build.props`、`.editorconfig`）、`Core.csproj`、`Core.Tests.csproj`、`BannedSymbols.txt`、直下の`csproj`の`Compile Remove`、`.githooks/pre-commit`、`.config/dotnet-tools.json`、`CLAUDE.md`を作り、空のテストが通る状態にします。
2. 固定小数点 型`Fix16`を実装し、C-01とC-07を確かめます。
3. ベクトル 型`Vec3Fix`と補助関数`IntMath`を実装します。
4. 三角関数 生成ツール`tools/GenTables`で`SinTable.g.cs`と`AtanTable.g.cs`を生成し、`Trig`を実装してC-15とC-16を確かめます。
5. 入力 型`InputFrame`と`InputBuffer`を実装し、C-04とC-05を確かめます。
6. 判定 型`HitSphere`と`Collision`（C-10）、`RingOut`（C-03）を実装します。
7. 技データ 定数`Limits`、DTO、`JsonContext`、`MoveTableLoader`を実装してC-06とC-11を確かめ、`data/characters/box.json`、`data/stages/default.json`、`data/rules.json`を置きます。
8. 試合状態 型`PlayerState`、`MatchState`、`MatchContext`、`StateHash`を実装し、C-08を確かめます。
9. コマンド判定 判定器`CommandParser`でテンキー表記の解釈と`CommandWindowFrames`を実装します。
10. フレーム処理 関数`MatchSimulator.Step`の手順1から11を実装し、C-02とC-09を確かめます。
11. 参照とAPIの検査 テスト`AssemblyReferenceTests`（C-12）と`PublicApiTests`（C-14）を書きます。
12. Game側 変換`FixConvert`、`InputMapper`、箱を表示するシーン、`E2eRunner`と`flows.json`（C-13）を作ります。
