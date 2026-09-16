# ADR-0004：技データはJSONで持ち、数値は実数で書く

## 状態

採用

## 背景

技ごとのコマンド、発生・持続・硬直、ダメージ、判定の形と位置は表として持ち、Coreはその表を読むだけにします。CoreはGodotの型を参照しないため、Godotの`.tres`リソースは変換層なしには使えません。

| 軸 | JSON（`System.Text.Json`） | Godotの`.tres` | C#の定数 |
|---|---|---|---|
| 追加の依存 | 無し（標準ライブラリ） | Game側の変換層 | 無し |
| Core単体のテスト | 文字列を渡すだけ | Godot起動が要る | ビルドに含まれる |
| 再ビルドなしの調整 | できる | できる | できない |
| 入れ子構造 | 自然 | 自然 | 自然 |

数値の書き方は、実数（0.35のようにmとフレームの単位）とQ16.16の生の整数を比べました。実数は人が読めますが、2進で割り切れない値に丸めが入ります。丸め方を読み込み時の1か所に固定すれば、同じ文字列は常に同じ整数になります。

## 決定

- 技データは`System.Text.Json`で読むJSONファイルです。ファイルは`data/characters/<name>.json`、`data/stages/<name>.json`、`data/rules.json`に置きます。
- 読み込みは`Godot3dFighter.Core.Data.GameDataLoader`の`LoadCharacter(Stream)`、`LoadStage(Stream)`、`LoadRules(Stream)`が行います。Game側は`FileAccess.GetFileAsBytes`で読んだバイト列を`MemoryStream`に包んで渡し、空の配列が返った時は読み込み失敗として扱います。
- JSONの読み込みには`JsonSerializerContext`のソース生成を使い、リフレクションに頼りません。
- DTOの実数項目は`decimal`型で受けます。`Fix16`への変換は`decimal.Round(v * 65536m, 0, MidpointRounding.AwayFromZero)`で行い、結果を`int`に収めます。この変換は`Fix16.FromDecimal`の1か所だけです。
- フレーム数、ダメージ、体力は整数項目です。
- 未知の項目は`JsonUnmappedMemberHandling.Disallow`で拒否し、必須項目は`required`で宣言します。欠けた項目や未知の項目があれば読み込み時に例外にします。

## 影響

- 技データの追加や調整は再ビルドなしに行えます。
- 常に成り立つ条件C-06として「同じJSON文字列からは同じ内容のデータが得られ、未知または欠損の項目は読み込み時に例外になる」を置き、`GameDataLoaderTests`で確かめます。
- 常に成り立つ条件C-07として「`Fix16.FromDecimal`は0.5を常にゼロから遠い方へ丸める」を置き、`Fix16Tests`で確かめます。
