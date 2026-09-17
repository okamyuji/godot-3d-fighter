# ADR-0008：ユニットテストはxUnit v3、主要導線の走破は自前の画面なし実行で行う

## 状態

採用

## 背景

一次情報として次を確認しました。

- xUnit v3の最新は4.0.1です。NUnitの最新は4.6.1、MSTestの最新は4.4.1です。
- .NET 10 SDKでは、`Microsoft.Testing.Platform`（MTP）に対応したテストプロジェクトで`dotnet test`をVSTest経由で動かすと、ビルド時に`Testing with VSTest target is no longer supported`というエラーで止まります。xUnit v3はMTPに標準で対応しているため、この制限を受けます（実機で確認）。
- `dotnet test`をMTPで動かすには、リポジトリ直下の`global.json`に`{ "test": { "runner": "Microsoft.Testing.Platform" } }`を置きます。これで`dotnet test`はxUnit v3のMTP実行体を直接呼び、`xunit.runner.visualstudio`（VSTestアダプタ）と`Microsoft.NET.Test.Sdk`（VSTest連携パッケージ）は無くても動きます（実機で確認）。
- MTPでのカバレッジ収集には`Microsoft.Testing.Extensions.CodeCoverage`（最新18.11.2）を使います。`dotnet test -- --coverage --coverage-output-format cobertura --coverage-output <path>`で、参照プロジェクトのクラス・メソッド・行ごとのcobertura形式を得られます（実機で確認。テストプロジェクトと参照プロジェクトを別ディレクトリに分けないと、参照側のコードが正しく計測されないことも確認）。
- Crap4DotNet 0.1.1はCoverletの出力もMTPの`Microsoft.Testing.Extensions.CodeCoverage`の出力も同じcobertura形式として読み、正しくCRAP値を算出します（実機で確認）。
- gdunit4.api 5.0.0はGodotSharp 4.4.0に依存し、対応表はGodot 4.4.1までです。Godot 4.7での動作は確認できていません。

| 軸 | xUnit v3 4.0.1 | NUnit 4.6.1 | MSTest 4.4.1 |
|---|---|---|---|
| MTPでの`dotnet test` | 標準対応 | 別途アダプタが要る | 別途アダプタが要る |
| データ駆動テスト | `[Theory]`と`[InlineData]` | `[TestCase]` | `[DataRow]` |
| 並列実行 | 既定でクラス単位 | 属性で指定 | 属性で指定 |

主要導線の走破は、Godot本体を画面なしで起動し、シナリオに沿って入力を注入して終了コードで判定する方式と、gdUnit4を比べました。自前の方式はGodot 4.7での動作が未確認の依存を持ちません。

## 決定

- ユニットテストはxUnit v3 4.0.1で、MTPモードで動かします。参照は`xunit.v3` 4.0.1と`Microsoft.Testing.Extensions.CodeCoverage` 18.11.2だけです。`xunit.runner.visualstudio`と`Microsoft.NET.Test.Sdk`は参照しません。
- リポジトリ直下に`global.json`を置き、`test.runner`を`Microsoft.Testing.Platform`にします。
- `dotnet test`の実行は`dotnet test <プロジェクトかディレクトリのパス> [-c <構成>] [--no-build]`です。カバレッジが要る時は`-- --coverage --coverage-output-format cobertura --coverage-output <path>`を続けます。
- 主要導線の走破はGame側の`E2eRunner`が行います。起動は`godot --headless --fixed-fps 60 --path . -- --e2e tests/e2e/scenarios/<name>.json`で、シナリオの各フレームの入力をCoreの入力境界に注入し、期待する画面と結果に達したら終了コード0、達しなければ1で終了します。
- 導線の一覧は`tests/e2e/flows.json`に持ちます。各導線には1つ以上のシナリオが対応し、`E2eRunner --check-flows`は対応するシナリオが無い導線があれば終了コード1を返します。
- 主要導線は次のとおりです。

| 導線ID | 内容 |
|---|---|
| F-01 | 起動してタイトル画面に達する |
| F-02 | キャラ選択から試合開始に達する |
| F-03 | 体力ゼロでラウンドが決着する |
| F-04 | リングアウトでラウンドが決着する |
| F-05 | 時間切れでラウンドが決着する |
| F-06 | 試合終了からリザルトを経てタイトルへ戻る |
| F-07 | トレーニングモードを開始し、終了してタイトルへ戻る |

各導線が通る画面と操作はADR-0023で定めます。

## 影響

- 判定やフレーム計算のテストはGodotを起動せずに`dotnet test`で回ります。
- 主要導線の走破はGodot本体を必要とするため、CIと手動で回します。pre-commitには入れません（ADR-0011）。
- 制約条件C-13として「`flows.json`の全導線に対応するシナリオが存在する」を置き、`E2eRunner --check-flows`で確かめます。
