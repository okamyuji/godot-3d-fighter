# ADR一覧

各ADRは「状態」「背景」「決定」「影響」の節を持ちます。決定に含まれる「常に成り立つ条件」はC番号で参照し、`docs/design/01-data-structures.md`の対応表でテストと結び付けます。

| 番号 | 題名 | 常に成り立つ条件 |
|---|---|---|
| [ADR-0001](0001-fixed-point-q16-16.md) | 数値表現は`int`のQ16.16固定小数点にする | C-01 |
| [ADR-0002](0002-coordinates-and-ring.md) | 座標系はGodotに合わせ、リングは円にする | C-02、C-03 |
| [ADR-0003](0003-input-encoding-and-buffer.md) | 入力は1バイトに符号化し、64フレーム分を状態の中に持つ | C-04、C-05 |
| [ADR-0004](0004-move-data-json.md) | 技データはJSONで持ち、数値は実数で書く | C-06、C-07 |
| [ADR-0005](0005-match-state-struct.md) | 試合状態は参照を含まない構造体にし、履歴は呼び出し側が持つ | C-08、C-09 |
| [ADR-0006](0006-hit-spheres-from-data.md) | 判定は球で表し、位置は技データに持つ | C-10、C-11 |
| [ADR-0007](0007-target-framework-and-layout.md) | 対象フレームワークはnet10.0にし、プロジェクトをGame、Core、テストに分ける | C-12 |
| [ADR-0008](0008-test-frameworks.md) | ユニットテストはxUnit v3、主要導線の走破は自前の画面なし実行で行う | C-13 |
| [ADR-0009](0009-static-analysis.md) | 静的解析はSDK同梱アナライザの全規則と禁止シンボルで行う | C-14 |
| [ADR-0010](0010-quality-gates.md) | 合格条件はカバレッジ、CRAP値、変異テスト、主要導線の走破で決める | 無し |
| [ADR-0011](0011-git-hooks.md) | gitフックは`core.hooksPath`で配布する | 無し |
| [ADR-0012](0012-ci-central-dotnet-template.md) | CIは中央リポジトリのdotnet用テンプレートを呼び、Godotは公式配布物を検証して使う | 無し |
| [ADR-0013](0013-doclint.md) | 文書とソースの機械検査は`tools/doclint.sh`で行う | 無し |
| [ADR-0014](0014-facing-angle-and-trig-tables.md) | 向きは16bitの角度で持ち、sin/cosの表で回転する | C-15、C-16 |
