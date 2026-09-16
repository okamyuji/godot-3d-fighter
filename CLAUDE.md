# godot-3d-fighter

3D格闘ゲームの格闘コア（Core）とGame側（Godot 4.7.2 .NET版）。

## 読む順番

1. 全ADRの一覧と、各ADRが満たす常に成り立つ条件（C番号）は`docs/adr/README.md`
2. 実装の順序と、常に成り立つ条件とテストの対応表は`docs/design/README.md`
3. 型、試合の処理、画面の詳細は`docs/design/01-data-structures.md`、`02-match-rules.md`、`03-screens-and-e2e.md`

## 作業の進め方

- `docs/design/README.md`の「実装の順序」に沿って1項目ずつ進める。各項目はテストを先に書き、失敗を確かめてから実装する（TDD）。
- 1項目ごとに`dotnet build`と`dotnet test`が通ることを確かめてからコミットする。
- コミット前に`tools/doclint.sh`と`dotnet format --verify-no-changes`が通ることを確かめる。`.githooks/pre-commit`が同じ内容を自動で行う（`git config core.hooksPath .githooks`で有効）。
- `Core`（`src/Core/`）はGodotの型を参照せず、`float`、`double`、`System.Math`、`System.MathF`、`System.Random`を使わない（ADR-0001、ADR-0009）。座標と数値は`Fix16`のQ16.16固定小数点で表す。
- `dotnet test`はMTP（Microsoft.Testing.Platform）モードで動く。リポジトリ直下の`global.json`がその設定を持つ（ADR-0008）。

## 品質ゲート

pre-commit（書式、ビルド、ユニットテスト、文書検査）はADR-0011、CIも含めた全条件（行カバレッジ、CRAP値、変異テスト、主要導線の走破）はADR-0010に定める。
