# 設計書の一覧

設計の決定とその理由は`docs/adr/`に、実装に必要な型、手順、画面の詳細はこのディレクトリにあります。

| 文書 | 内容 |
|---|---|
| [01-data-structures.md](01-data-structures.md) | 定数と上限、プロジェクト配置、型、技データのJSON、読み込み時の検査 |
| [02-match-rules.md](02-match-rules.md) | 公開する関数、状態と遷移、1フレームの処理の順序、打撃と投げの適用、決着 |
| [03-screens-and-e2e.md](03-screens-and-e2e.md) | 画面の構成と遷移、入力の割り当て、トレーニング、主要導線の走破 |

## 常に成り立つ条件とテストの対応

| 番号 | 条件 | ADR | テスト |
|---|---|---|---|
| C-01 | `Fix16`の全演算は整数演算だけで閉じ、同じ入力に同じ結果を返す | 0001 | `Fix16Tests`。乗算の負数の丸め、除算のゼロ方向の丸め、加算とキャストのあふれ、0除算 |
| C-02 | 座標成分の絶対値は1024m以下 | 0002 | `PositionClampTests` |
| C-03 | リングアウトは距離の二乗の比較で決まり、半径ちょうどはリング内 | 0002 | `RingOutTests`。半径ちょうどと`Raw`で1外側 |
| C-04 | Coreに入る`InputFrame`は正規化済みで、ビット7は0 | 0003 | `InputFrameTests` |
| C-05 | `InputBuffer`は直近64フレームだけを保持する | 0003 | `InputBufferTests`。65回`Push`して`At(63)`と`At(64)` |
| C-06 | 同じJSONから同じ内容のデータ。未知と欠損の項目は例外 | 0004 | `GameDataLoaderTests`。読み込んだ結果を項目ごとに比べる |
| C-07 | `Fix16.FromDecimal`は0.5をゼロから遠い方へ丸める | 0004 | `Fix16Tests` |
| C-08 | `MatchState`に詰め物のバイトが無い（230バイト） | 0005 | `MatchStateLayoutTests` |
| C-09 | 同じ状態と入力から`Step`は同じ状態と同じハッシュを返す | 0005 | `MatchSimulatorTests`。同じ入力列を2回流して比べる |
| C-10 | 球の重なりは距離の二乗の比較で決まり、接触は重なり | 0006 | `HitSphereTests` |
| C-11 | 1区間の攻撃判定4個、やられ判定8個を超える区間と、範囲の重なる区間は読み込み時に例外 | 0006 | `GameDataLoaderTests` |
| C-12 | `Core`はGodotSharpを参照しない | 0007 | `AssemblyReferenceTests` |
| C-13 | `flows.json`の全導線に対応するシナリオがある | 0008 | `E2eRunner --check-flows` |
| C-14 | `Core`の公開APIに`float`と`double`が現れない | 0009 | `PublicApiTests` |
| C-15 | sin表の代表値と`sin^2 + cos^2`の誤差 | 0014 | `TrigTableTests` |
| C-16 | atan2の代表値 | 0014 | `Atan2Tests` |
| C-17 | `Attack`の経過フレームが`Tracking`以上なら`Facing`は変わらない | 0015 | `TrackingTests` |
| C-18 | 投げの最中を除き、2人の水平距離は半径の和から`Raw`で1を引いた値以上 | 0015 | `BodyPushTests` |
| C-19 | 下方向を`TapFrames`以内に中立へ戻すと横移動、それ以外はならない | 0015 | `SidestepInputTests` |
| C-20 | `CameraYaw`は1フレームで16384を超えて変わらず、位置の入れ替わりで左右が入れ替わる | 0016 | `CameraSideTests` |
| C-21 | 押し続けたボタンで技は1回だけ成立し、同時押しは猶予の内側だけで成立する | 0017 | `CommandParserTests` |
| C-22 | 複数の技の成立は、方向の要素、ボタンの数、技番号の順で1つに決まる | 0017 | `CommandPriorityTests` |
| C-23 | 硬直が切れる`BufferFrames`フレーム前までの入力で、切れたフレームに技が出る | 0017 | `InputBufferingTests` |
| C-24 | 段と守りの12通りの組み合わせが表のとおりに決まる | 0018 | `GuardMatrixTests` |
| C-25 | 投げられない状態、範囲外、同じフレームに打撃を受けた投げは成立しない | 0019 | `ThrowTests` |
| C-26 | 投げ抜けは最初のGを含む入力だけで判定し、方向違いと背中からの投げは抜けられない | 0019 | `ThrowEscapeTests` |
| C-27 | 出始めと判定が出ている間の命中だけがカウンターヒットで、ガードはならない | 0020 | `CounterHitTests` |
| C-28 | 同じフレームの命中は、処理の順に関係なく2人とも受ける | 0020 | `TradeTests`。P1とP2を入れ替えた2通りを比べる |
| C-29 | ダウン中は`HitsDown`の技だけが当たり、1回のダウンで2回目は当たらない | 0021 | `DownAttackTests` |
| C-30 | 受け身は`TechFrames`以内のP+K+Gだけで成立し、`DownMinFrames`の間は他の入力を受け付けない | 0021 | `WakeUpTests` |
| C-31 | 負けの人数と残り時間の組み合わせごとに、勝ち数の増え方が決まる | 0022 | `RoundResolutionTests` |
| C-32 | 勝ち数が同数なら試合は続き、`MaxRounds`で引き分けになる | 0022 | `MatchEndTests` |
| C-33 | トレーニングでは残り時間が減らず、ラウンドが終わらない | 0022 | `TrainingRulesTests` |

## 実装の順序

各項目は、テストを先に書いて失敗を確かめてから実装する単位です。1項目ごとにコミットします。

1. 土台 共通設定（`Directory.Build.props`、`.editorconfig`）、`Core.csproj`、`Core.Tests.csproj`、`BannedSymbols.txt`、直下の`csproj`の`Compile Remove`、`.githooks/pre-commit`、`.config/dotnet-tools.json`、`CLAUDE.md`を作り、空のテストが通る状態にします。
2. 固定小数点 型`Fix16`を実装し、C-01とC-07を確かめます。
3. ベクトルと整数平方根 型`Vec3Fix`と`IntMath`を実装します。
4. 三角関数 生成ツール`tools/GenTables`で表を生成し、`Trig`を実装してC-15とC-16を確かめます。
5. 入力 型`InputFrame`と`InputBuffer`を実装し、C-04とC-05を確かめます。
6. 判定の球とリング 型`HitSphere`、`Collision`、`RingOut`を実装し、C-03とC-10を確かめます。
7. 技データ 読み込み`GameDataLoader`と`data/`の既定のファイルを作り、C-06とC-11を確かめます。
8. 試合状態 型`PlayerState`、`MatchState`、`MatchContext`、`StateHash`を実装し、C-08を確かめます。
9. コマンド判定 判定器`CommandParser`と行動の判断`ActionSelector`を実装し、C-21、C-22、C-23を確かめます。
10. 移動と向き 移動、押し合い、向き、画面の左右を実装し、C-02、C-17、C-18、C-19、C-20を確かめます。
11. 打撃 段、カウンターヒット、相打ちを実装し、C-24、C-27、C-28を確かめます。
12. 投げ 投げと投げ抜けを実装し、C-25とC-26を確かめます。
13. ダウン ダウン、受け身、転がり、起き上がりを実装し、C-29とC-30を確かめます。
14. 決着 ラウンドと試合の決着、トレーニングの規則を実装し、C-09、C-31、C-32、C-33を確かめます。
15. 参照と公開APIの検査 テスト`AssemblyReferenceTests`（C-12）と`PublicApiTests`（C-14）を書きます。
16. 画面 ゲーム側の画面、入力の割り当て、トレーニングの表示を作ります。
17. 主要導線の走破 実行器`E2eRunner`、`flows.json`、各導線のシナリオを作り、C-13と全シナリオの成功を確かめます。
