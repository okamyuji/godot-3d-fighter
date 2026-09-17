# 画面と主要導線の走破

この文書は、Game側の画面の構成と遷移、入力の受け取り、トレーニングの機能、主要導線の走破の実行方法を定めます。画面の構成を決めた理由はADR-0023、走破の方式はADR-0008にあります。

## 画面

各画面はGodotのシーンで、根のノードのスクリプトが`IE2eScreen`を実装します。

```csharp
public interface IE2eScreen
{
    string ScreenName { get; }
    bool TryInvoke(string action);   // 画面にない操作ならfalse
}
```

| 画面 | シーン | 表示する情報 | 操作と遷移 |
|---|---|---|---|
| `Title` | `scenes/Title.tscn` | タイトル、「対戦」「トレーニング」の選択肢 | `Versus`で対戦のキャラ選択へ、`Training`でトレーニングのキャラ選択へ |
| `CharacterSelect` | `scenes/CharacterSelect.tscn` | 2人の選択中のキャラ名、決定済みかどうか、P2がCPUになるまでの秒数、選択中のステージ名、CPUの手強さ | `NextStage`でステージを`data/stages/`のファイル名の順に切り替え、`NextCpuLevel`でCPUの手強さをやさしい、ふつう、つよいの順に切り替え、`ConfirmP1`と`ConfirmP2`が両方そろうと試合へ、対戦で`ConfirmP1`だけなら180フレームのカウントダウンの後にP2をCPUにして試合へ（ADR-0026）、トレーニングは`ConfirmP1`だけでトレーニングへ、`Back`でタイトルへ |
| `Match` | `scenes/Match.tscn` | 体力ゲージ、残り時間（秒、切り上げ）、勝ち数、ラウンド番号、決着の理由（KO、RING OUT、TIME UP）、カウンターヒットと壁やられの表示 | 試合が`MatchEnd`になり`RoundEndFrames`が経つとリザルトへ |
| `Result` | `scenes/Result.tscn` | 勝者か引き分け、各ラウンドの決着の理由と勝者 | `Confirm`でタイトルへ |
| `Training` | `scenes/Training.tscn` | `Match`の表示に加え、フレーム差、2人の状態名と`StateFrame`、相手の動作の設定 | `ResetPositions`、`DummyStand`、`DummyCrouch`、`DummyGuardAll`、`Exit`（タイトルへ） |

試合とトレーニングの画面は、`_PhysicsProcess`で`MatchSimulator.Step`を1回呼び、状態を描画に反映します。試合は`data/rules.json`、トレーニングは`data/rules-training.json`の規則を使います。`project.godot`の`physics_ticks_per_second`は60です。キャラは頭、胴、両腕（上腕と前腕）、両脚（大腿と下腿）の図形（ADR-0025）で表し、位置と向きは`FixConvert`で`Vector3`と回転に変換します。攻撃の判定区間では、技のコマンドがPなら右腕、Kなら右脚の先端を、区間の最初の攻撃カプセルの`B`へ伸ばし、その手足を明るい色にします。発生中は構えから最初の区間の`B`へ、硬直中は最後の区間の`B`から構えへ、フレーム数で補間します。投げは判定区間を持たないので、技の間と`Throwing`と`Thrown`の間は両腕を前へ伸ばした固定の姿勢にし、技の間は両腕を明るい色にします。打撃が命中した側と投げられた側は6フレーム白くし、ガードした側は白くしません。カメラはADR-0016のとおり、2人の足元の中点から`CameraYaw - 16384`の方向へ4.5m離れた高さ1.6mに置き、中点の1m上を見ます。

リザルトに出す各ラウンドの結果は、試合画面が`Phase`の`RoundEnd`への変化を見て、`LastRoundReason`と`LastRoundWinners`を記録したものです。

試合とトレーニングの画面は、HUDに出している文字列（体力、残り時間、ラウンド番号と勝ち数、決着の理由、`COUNTER`、`WALL`、`COUNTER WALL`）を空白で連結した`HudText`を`IE2eMatchState`で公開します。

## 入力

`InputMapper`は、Godotの`InputMap`の操作名からプレイヤーごとの`InputFrame`を作り、ADR-0003の正規化を行います。

| 操作名 | P1の初期割り当て | P2の初期割り当て |
|---|---|---|
| `pN_up`、`pN_down`、`pN_left`、`pN_right` | W、S、A、D | 矢印キー |
| `pN_punch`、`pN_kick`、`pN_guard` | J、K、L | テンキーの1、2、3 |

ゲームパッドは、接続した順にP1、P2へ割り当て、方向パッドと左スティック、下側の3つのボタンを使います。

メニューの画面は、同じ操作名を`_UnhandledInput`で受けます。ボタンをマウスで押しても同じ`TryInvoke`を呼びます。

| 画面 | キー |
|---|---|
| `Title`、`Result` | `p1_up`と`p1_down`でフォーカスを移し、`p1_punch`か`p2_punch`でフォーカス中のボタンを押す（`MenuKeys`）。矢印キーとEnterはGodotの既定のフォーカス移動で動く |
| `CharacterSelect` | `p1_punch`で`ConfirmP1`、`p2_punch`で`ConfirmP2`、`p1_kick`か`p2_kick`で`NextStage`、`p1_guard`で`NextCpuLevel`、`ui_cancel`（Esc）で`Back` |
| `Training` | `ui_cancel`（Esc）で`Exit` |

## CPUの相手

対戦でP1だけが決定すると、キャラ選択は180フレームのカウントダウンを表示します。その間に`ConfirmP2`があれば人間のP2として始め、無ければ`GameState.P2IsCpu`を立てて試合へ進みます。試合画面は`P2IsCpu`なら、`InputMapper`を読まずに`CpuPolicy.Decide`（ADR-0026）の入力をP2に渡します。手強さは`GameState.CpuLevel`で、既定は「ふつう」です。主要導線の`frames`手順の`p2`は、P2がCPUの間は無視されます。

## トレーニング

- フレーム差 一方の`LastHitKind`が`Hit`、`CounterHit`、`Guarded`のいずれかになった後、2人が行動の判断を受け付ける状態へ戻ったフレームを記録します。「相手が戻ったフレーム − 自分が戻ったフレーム」を攻撃した側のフレーム差として表示します。正なら攻撃した側が有利です。
- 相手の動作 相手（P2）の入力を、設定に合わせてGame側で作ります。「立つ」は中立、「しゃがむ」は下です。「すべてガード」は、P1が`Attack`で打撃の技の段が`Low`ならGと下、それ以外はGです。
- 位置のリセット 現在の状態を、`MatchSimulator.ResetPositions`が返す状態にします。

## 主要導線の走破

### 実行

```text
godot --headless --fixed-fps 60 --path . -- --e2e tests/e2e/scenarios/<name>.json
godot --headless --path . -- --check-flows
```

`--`より後ろの引数は`OS.GetCmdlineUserArgs()`で読みます。`--e2e`のシナリオは、`res://`からの相対パスか絶対パスで指定します。絶対パスは、エクスポートした実行ファイルで走らせる時に使います。`--e2e`が無い時は、シナリオ用の規則やステージの差し替えを受け付けません。

`E2eRunner`は、シナリオの手順を1つずつ実行し、すべて成功すれば終了コード0、失敗した時は失敗した手順の番号と内容を出力して終了コード1で終わります。1つのシナリオの上限は36000フレームで、超えた時点で失敗です。

`--check-flows`は、`tests/e2e/flows.json`の各導線について、`flow`が一致するシナリオが`tests/e2e/scenarios/`に1つ以上あるかを確かめます。無い導線があれば、その導線を出力して終了コード1です。

### シナリオの形式

```json
{
  "flow": "F-03",
  "rules": "tests/e2e/data/rules-e2e.json",
  "stage": "tests/e2e/data/stage-e2e.json",
  "steps": [
    { "expectScreen": "Title", "withinFrames": 120 },
    { "action": "Versus" },
    { "expectScreen": "CharacterSelect", "withinFrames": 60 },
    { "action": "ConfirmP1" },
    { "action": "ConfirmP2" },
    { "expectPhase": "Fight", "withinFrames": 120 },
    { "frames": 20, "p1": "6", "p2": "5" },
    { "frames": 1, "p1": "5P", "p2": "5" },
    { "frames": 40, "p1": "5", "p2": "5" },
    { "frames": 10, "p1": "6", "p2": "5" },
    { "frames": 1, "p1": "5P", "p2": "5" },
    { "frames": 30, "p1": "5", "p2": "5" },
    { "expectRoundEnd": { "reason": "KnockOut", "winners": [1] }, "withinFrames": 600 }
  ]
}
```

| 手順 | 内容 |
|---|---|
| `expectScreen` | 表示中の画面の`ScreenName`が一致するまで最大`withinFrames`フレーム待つ |
| `action` | 表示中の画面の`TryInvoke`を呼ぶ。falseなら失敗 |
| `expectPhase` | 試合かトレーニングの`Phase`が一致するまで待つ |
| `expectHud` | 試合かトレーニングの`HudText`が文字列を含むまで待つ |
| `frames` | `p1`と`p2`の入力を`frames`フレーム続けて与える。入力は画面上の絶対的な方向の数字（6が右）と、押し続けるボタン（P、K、G）の並び |
| `expectRoundEnd` | `LastRoundReason`と`LastRoundWinners`（1がP1、2がP2）が一致する`RoundEnd`まで待つ |
| `expectMatchEnd` | `MatchEnd`になり、勝者の一覧が一致するまで待つ。引き分けは空の一覧 |

`rules`、`stage`、`characters`（2要素の配列）は省略でき、省略した時は`data/`の既定のファイルを使います。

### 導線とシナリオ

`tests/e2e/flows.json`は、導線の`id`と`name`を持つ配列です。

| 導線 | シナリオ | 差し替えるデータ | 手順の要点 |
|---|---|---|---|
| F-01 | `f01-title.json` | 無し | タイトルの表示を待つ |
| F-02 | `f02-start-match.json` | 無し | 対戦を選び、2人が決定し、`Fight`を待つ |
| F-03 | `f03-knockout.json` | `rules-e2e.json` | P1が近づいて打撃を繰り返し、KOでP1の勝ちを待つ、`KO`の表示を待つ |
| F-04 | `f04-ringout.json` | `rules-e2e.json`、`stage-e2e.json` | P2が後ろ（画面の右）へ歩き続け、リングアウトでP1の勝ちを待つ、`RING OUT`の表示を待つ |
| F-05 | `f05-timeup.json` | `rules-timeup.json` | 入力しないまま時間切れを待ち、2人の勝ちと引き分けの試合終了を待つ、`TIME UP`の表示を待つ |
| F-06 | `f06-result-to-title.json` | `rules-e2e.json` | F-03と同じ手順で試合を終え、リザルトで`Confirm`し、タイトルを待つ |
| F-07 | `f07-training.json` | 無し | トレーニングを選び、P1だけが決定し、`DummyGuardAll`で打撃を出し、`ResetPositions`、`Exit`でタイトルを待つ |
| F-08 | `f08-cpu-match.json` | `rules-e2e.json` | 対戦を選び、P1だけが決定し、カウントダウンの後の`Fight`を待ち、P1が動かないままKOでP2の勝ちを待つ、`KO`の表示を待つ |

走破用のデータは`tests/e2e/data/`に置きます。

| ファイル | 内容 |
|---|---|
| `rules-e2e.json` | `data/rules.json`から、`InitialHealth`を24、`RoundsToWin`を1、`MaxRounds`を1、`IntroFrames`と`RoundEndFrames`を30にした規則 |
| `rules-timeup.json` | `rules-e2e.json`から、`RoundTimeSeconds`を3にした規則 |
| `stage-e2e.json` | 形を円、`Size`を2.0、縁をリングアウト、`StartDistance`を2.0にしたステージ |
