# godot-3d-fighter

Virtua Fighter級の3D格闘ゲームを、Godot 4とC#で作るプロジェクトです。格闘ロジック（Core）はGodotに依存しないC#クラスライブラリとして実装し、Game側は描画と入力の受け渡しだけを担当します。設計の決定は`docs/adr/`に、型、試合の処理、画面の詳細は`docs/design/`にあります。

## 画面

タイトルから対戦開始まで、実際にGodot上で動作している画面です。

![タイトル画面](docs/images/title-screen.png)

対戦画面では、`MatchSimulator.Step`を毎物理フレーム呼び出し、体力、残り時間、ラウンド数、段階をHUDに表示します。

![対戦画面](docs/images/match-screen.png)

## 方針

格闘ゲームの手応えは、判定とフレームデータの正確さと、読み合いの規則で決まります。ロジックは固定60fpsで更新し、同じ入力列から機種を問わず同じ結果になるよう、整数演算だけで書いています。判断の基準は、アーケードや家庭用機の格闘ゲームとしてお客様が楽しめるかどうかです。

## エンジンと言語

エンジンはGodot 4.7.2の.NET版、言語はC#です。対象フレームワークはnet10.0です（ADR-0007）。

固定小数点の型`Fix16`（Q16.16）を全ての実数値に使い、`float`、`double`、`System.Math`、`System.MathF`、`System.Random`はCore（`src/Core/`）では使いません（ADR-0001、ADR-0009）。この制約は`Microsoft.CodeAnalysis.BannedApiAnalyzers`で機械的に守らせています。

## プロジェクト構成

| プロジェクト | 場所 | 役割 |
|---|---|---|
| `Godot3dFighter` | リポジトリ直下、`src/Game/` | Game側。画面（`scenes/`）、入力の受け渡し、`MatchSimulator`の呼び出し、主要導線の走破の実行器（`E2eRunner`） |
| `Core` | `src/Core/` | 格闘ロジック。Godotの型を参照しない |
| `Core.Tests` | `tests/Core.Tests/` | Coreのユニットテスト（xUnit v3、MTPモード） |
| 走破シナリオ | `tests/e2e/` | 主要導線の一覧（`flows.json`）とシナリオ（`scenarios/*.json`） |

全プロジェクトの対象フレームワークはnet10.0です。

## 画面の構成

| 画面 | シーン | 役割 |
|---|---|---|
| タイトル | `scenes/Title.tscn` | 「対戦」「トレーニング」の選択 |
| キャラ選択 | `scenes/CharacterSelect.tscn` | ステージの選択、両者の決定 |
| 対戦 | `scenes/Match.tscn` | 試合の進行、決着後にリザルトへ遷移 |
| リザルト | `scenes/Result.tscn` | 各ラウンドの決着とタイトルへの復帰 |
| トレーニング | `scenes/Training.tscn` | 相手の動作設定、位置のリセット |

各画面は`IE2eScreen`を実装し、`ScreenName`と`TryInvoke(action)`で主要導線の走破から操作できます。詳細は`docs/design/03-screens-and-e2e.md`にあります。

## 導入手順

1. Godot 4.7.2の.NET版を入れます。macOSでは`brew install --cask godot-mono`です。
2. .NET SDK 10を入れます。`dotnet --list-sdks`で10.0.400以上があることを確かめます。
3. リポジトリを取得し、`git config core.hooksPath .githooks`でpre-commitフックを有効にします。

## 動かし方

Godotエディタでプロジェクトを開くか、次のコマンドで直接起動します。

```sh
/Applications/Godot_mono.app/Contents/MacOS/Godot --path .
```

P1はWASDとJ（パンチ）、K（キック）、L（ガード）、P2は矢印キーとテンキーの1、2、3を使います。

## 品質ゲート

`git commit`のたびに`.githooks/pre-commit`が次を確かめます（`git config core.hooksPath .githooks`で有効化、ADR-0011）。

1. `tools/doclint.sh`で文書とソースの和文英数字境界スペース、ADRの必須節、先送り語を検出します（ADR-0013）
2. `dotnet format --verify-no-changes`で書式を確かめます
3. `dotnet build`で`AnalysisLevel=latest-All`の静的解析を含むビルドを確かめます（ADR-0009）
4. `dotnet test`でCore.Testsの296件のユニットテストを確かめます

主要導線の走破は、ヘッドレスのGodotから実行します。

```sh
godot --headless --fixed-fps 60 --path . -- --e2e tests/e2e/scenarios/f03-knockout.json
godot --headless --path . -- --check-flows
```

`--check-flows`は、`flows.json`に載っている7つの導線（F-01からF-07）それぞれに対応するシナリオがあることを確かめます（C-13）。カバレッジ、CRAP値、変異テストの基準はADR-0010にあります。

## 常に成り立つ条件

設計の決定に含まれる「常に成り立つ条件」（C-01からC-35）と、それを確かめるテストの対応表は`docs/design/README.md`にあります。全35条件に対応するテストが揃っています。

## 読む順番

1. 全ADRの一覧と、各ADRが満たす常に成り立つ条件は`docs/adr/README.md`
2. 常に成り立つ条件とテストの対応表、実装の順序は`docs/design/README.md`
3. 型、試合の処理、画面の詳細は`docs/design/01-data-structures.md`、`02-match-rules.md`、`03-screens-and-e2e.md`
