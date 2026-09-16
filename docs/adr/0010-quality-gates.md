# ADR-0010：合格条件はカバレッジ、CRAP値、変異テスト、主要導線の走破で決める

## 状態

採用（2026-09-17）

## 背景

一次情報として次を確認しました。

- coverlet.msbuild 10.0.1は`/p:Threshold`でカバレッジの下限を設定でき、下回るとビルドが失敗します。collector版はビルドを失敗させられません。
- Crap4DotNet 0.1.1はRoslynで循環的複雑度を算出し、coberturaのカバレッジと組み合わせてCRAP値を出します。`--threshold N`を超える関数があれば終了コード1です。開発機では`--allow-roll-forward`付きで導入し、`analyze --help`が動くことを確認しました。ソースでは、閾値は整数で、判定は「CRAP値 > 閾値」です（`CrapAnalyzer.cs`）。`--min-crap`は実数を受け、「CRAP値 ≥ 指定値」の関数だけを出力のJSONに残します（`AnalyzeCommand.cs`）。
- Stryker.NET 5.0.0は`since`でgitの差分にある変異だけを対象にでき、`thresholds.break`を下回ると終了コード1です。`break`は`low`以下、`low`は`high`以下が条件です。
- Microsoft.CodeAnalysis.Metrics 5.6.0の`Metrics.exe`は.NET Framework 4.7.2向けで、macOSでは動きません。

## 決定

| 検査 | 対象 | 条件 | 実行 |
|---|---|---|---|
| 書式 | 全プロジェクト | `dotnet format --verify-no-changes`が差分ゼロ | pre-commit、CI |
| ビルド | 全プロジェクト | アナライザ込みで警告ゼロ（ADR-0009） | pre-commit、CI |
| ユニットテスト | `Core.Tests` | 全件Pass | pre-commit、CI |
| 行カバレッジ | `Core` | 80%以上。`coverlet.msbuild`の`Threshold=80`、`ThresholdType=line`、`ThresholdStat=total` | CI |
| CRAP値 | `Core` | 全関数が15未満。`dotnet-crap analyze src/Core --coverage <cobertura> --threshold 15 --min-crap 15 --output crap.json`を実行し、`crap.json`の`methods`が空であること | CI |
| 変異テスト | `Core` | 変更行の変異がすべて検出される。`dotnet stryker --project Core.csproj --since:<基準> --break-at 100 --threshold-low 100 --threshold-high 100` | CI |
| 主要導線の走破 | Game | `flows.json`の全導線のシナリオが終了コード0 | CI |
| 文書の機械検査 | UTF-8で読める全ファイル | `tools/doclint.sh`が検出ゼロ（ADR-0013） | pre-commit、CI |

- Crap4DotNetは`dotnet-tools.json`に版0.1.1と`rollForward: true`で登録し、`dotnet tool restore`で導入します。
- 変異テストの基準は、Pull Requestでは`main`、`main`への直接pushでは1つ前のコミットです。対象は`Core`だけで、Gameは変異しません。
- ユニットテストは、変更した関数について正常系、異常系、境界値を含めます。網羅の厳密さより、常に成り立つ条件（各ADRのC番号）ごとに1つ以上のテストがあることを優先します。

## 影響

- pre-commitは書式、ビルド、ユニットテスト、文書検査の4つで、数十秒で終わります。CRAP値、変異テスト、主要導線の走破はCIだけで回します。
- Crap4DotNetの終了コードは「閾値を超えるか」で決まるため、14.5のような値を「15未満」として扱えません。合否は`--min-crap 15`で出力した`methods`が空かどうかで判定します。
- 変異テストの`break-at 100`は、変更行に1つでも生き残った変異があれば失敗します。
