# ADR-0009：静的解析はSDK同梱アナライザの全規則と禁止シンボルで行う

## 状態

採用

## 背景

一次情報として次を確認しました。

- .NET SDKにはNetAnalyzersが同梱され、`AnalysisLevel`を`latest-All`にすると全規則が有効になります。`EnforceCodeStyleInBuild`で`.editorconfig`の書式規則もビルド時に検査できます。
- BannedApiAnalyzersの最新は5.6.0です。禁止シンボルの一覧を1ファイルに書くと、参照した箇所がビルドエラーになります。
- 追加候補の最新版は、SonarAnalyzer.CSharp 10.34.0.3385、Roslynator.Analyzers 5.0.0です。
- Godot.NET.Sdk 4.7.2のプロジェクトで`latest-All`、`EnforceCodeStyleInBuild`、`TreatWarningsAsErrors`を有効にし、名前空間を持つ`Node`派生クラスを置いた状態でビルドが成功しました。ソースジェネレータの出力はこの設定を通ります。

| 軸 | SDK同梱＋BannedApiAnalyzers | Sonarを追加 | Roslynatorを追加 |
|---|---|---|---|
| 追加の依存 | 1つ | 2つ | 2つ |
| 複雑度の規則 | 無し（CRAP値で縛る） | 重複する | 重複する |
| 修正パターンの安定性 | 高い | 固有の指摘に揺れが出る | 同左 |

## 決定

- 直下の`Directory.Build.props`で全プロジェクトに次を設定します。`AnalysisLevel`は`latest-All`、`EnforceCodeStyleInBuild`はtrue、`TreatWarningsAsErrors`はtrue、`Nullable`はenable、`ImplicitUsings`はdisableです。
- 書式は`dotnet format`（SDK同梱）で整え、`.editorconfig`をリポジトリ直下に置きます。
- `Core`と`Core.Tests`は`Microsoft.CodeAnalysis.BannedApiAnalyzers` 5.6.0を参照し、`src/Core/BannedSymbols.txt`で`T:System.Math`、`T:System.MathF`、`T:System.Random`を禁止します。
- `Core`は`CheckForOverflowUnderflow`をtrueにします（ADR-0001）。
- 規則の抑制は`.editorconfig`に理由付きで書き、`#pragma`や`[SuppressMessage]`は使いません。

## 影響

- 警告が1つでもあればビルドが失敗し、pre-commitとCIで止まります。
- Coreでは`Math`の整数版（`Math.Abs(int)`など）も使えないため、必要な補助関数は`Fix16`と`IntMath`に持ちます。
- `float`と`double`は型として禁止できないため、制約条件C-14として「`Core`の公開APIの引数と戻り値に`float`と`double`が現れない」を置き、`PublicApiTests`でリフレクションにより確かめます。
