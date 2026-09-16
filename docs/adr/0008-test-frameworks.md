# ADR-0008：ユニットテストはxUnit v3、主要導線の走破は自前の画面なし実行で行う

## 状態

採用

## 背景

一次情報として次を確認しました。

- xUnit v3の最新は4.0.1です。公式ドキュメントでは、`dotnet test`は既定でVSTest経由で動き、Microsoft.Testing.Platformモードでは標準のCoverletが使えません。
- NUnitの最新は4.6.1、MSTestの最新は4.4.1です。
- Stryker.NET 5.0.0の`test-runner`の既定は`vstest`で、`mtp`はプレビューです。
- Crap4DotNet 0.1.1はCoverletのcobertura形式を入力にします。
- gdunit4.api 5.0.0はGodotSharp 4.4.0に依存し、対応表はGodot 4.4.1までです。Godot 4.7での動作は確認できていません。

| 軸 | xUnit v3 4.0.1 | NUnit 4.6.1 | MSTest 4.4.1 |
|---|---|---|---|
| 実行モード | VSTest（既定） | VSTest | VSTest |
| データ駆動テスト | `[Theory]`と`[InlineData]` | `[TestCase]` | `[DataRow]` |
| 並列実行 | 既定でクラス単位 | 属性で指定 | 属性で指定 |

主要導線の走破は、Godot本体を画面なしで起動し、シナリオに沿って入力を注入して終了コードで判定する方式と、gdUnit4を比べました。自前の方式はGodot 4.7での動作が未確認の依存を持ちません。

## 決定

- ユニットテストはxUnit v3 4.0.1で、VSTestモードで動かします。参照は`xunit.v3` 4.0.1、`xunit.runner.visualstudio` 4.0.0、`Microsoft.NET.Test.Sdk` 18.10.1、`coverlet.msbuild` 10.0.1です。
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
- 常に成り立つ条件C-13として「`flows.json`の全導線に対応するシナリオが存在する」を置き、`E2eRunner --check-flows`で確かめます。
