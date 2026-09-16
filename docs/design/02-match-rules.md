# 試合の規則とフレーム処理

この文書は、`Core`が1フレームごとに行う処理の手順と、状態の移り方を定めます。型と技データは`01-data-structures.md`にあります。規則の理由はADR-0015からADR-0022にあり、ADRと矛盾する記述があれば、ADRが正です。

## 公開する関数

| 関数 | 内容 |
|---|---|
| `MatchSimulator.CreateMatch(MatchContext ctx)` | 1ラウンド目の`Intro`の状態を返す |
| `MatchSimulator.Step(in MatchState state, InputFrame p1, InputFrame p2, MatchContext ctx)` | 次のフレームの状態を返す。引数の状態は書き換えない |
| `MatchSimulator.ResetPositions(in MatchState state, MatchContext ctx)` | 勝ち数とラウンド番号を保ったまま、2人を開始位置と初期状態に戻した状態を返す |
| `StateHash.Compute(in MatchState state)` | 状態のバイト列のFNV-1a（64bit）。乗算は`unchecked` |

開始位置は、P1が`(-StartDistance / 2, 0, 0)`、P2が`(StartDistance / 2, 0, 0)`です。P1の`Facing`は0、P2は32768、`CameraYaw`は0、体力は`InitialHealth`、状態は`Idle`、`RoundTimerFrames`は`RoundTimeSeconds * 60`です。

## 状態と遷移

「行動の判断」は、ADR-0017の順で行動を選ぶ処理です。行動の判断に入る状態へ戻ったフレームと、取り消し可能な時点に入ったフレームでは、先行入力（`BufferFrames`）を使います。新しい状態に入ると`StateFrame`は0になり、そのフレームでは増やしません。

| 状態 | 行動の判断 | 入る条件 | 終わる条件と次の状態 |
|---|---|---|---|
| `Idle` | 毎フレーム | 行動の判断で立ちを選んだ | 行動の判断で決まる |
| `Walk`、`BackWalk` | 毎フレーム | 前（6）、後ろ（4） | 行動の判断で決まる |
| `Crouch` | 毎フレーム | 下方向（1、2、3） | 行動の判断で決まる |
| `Guard` | 毎フレーム | G | 行動の判断で決まる |
| `CrouchGuard` | 毎フレーム | Gと下方向 | 行動の判断で決まる |
| `SidestepIn`、`SidestepOut` | `StateFrame`が`SidestepCancelFrame`以上 | 横移動の入力（ADR-0015） | `StateFrame`が`SidestepFrames`で行動の判断 |
| `Dash`、`Backdash` | 最後の`DashCancelFrames`フレーム | 「66」「44」 | `StateFrame`が`DashFrames`、`BackdashFrames`で行動の判断 |
| `Attack` | しない | 技の成立 | `StateFrame`が`Startup + Active + Recovery`で行動の判断 |
| `Throwing`、`Thrown` | しない | 投げの成立 | `StateFrame`が`ThrowEscapeFrames`で投げの終わりの処理 |
| `ThrowEscape` | しない | 投げ抜け、投げどうしの同時成立 | `StateFrame`が`ThrowEscapeRecoveryFrames`で行動の判断 |
| `Blockstun`、`CrouchBlockstun` | しない | 立ちガード、しゃがみガードでのガード | `StunFrames`が0で行動の判断 |
| `Hitstun` | しない | ダウンしない命中 | `StunFrames`が0で行動の判断 |
| `Down` | しない | ダウンする命中、ダウンさせる投げ | 起き上がりの入力（下の節） |
| `Tech` | しない | ダウンから`TechFrames`以内のP+K+G | `StateFrame`が`TechRecoveryFrames`で行動の判断 |
| `RollIn`、`RollOut` | しない | ダウン中の上、下 | `StateFrame`が`RollFrames`で`Rise` |
| `Rise` | しない | ダウン中のG、`DownMaxFrames`の経過、転がりの終わり | `StateFrame`が`RiseFrames`で行動の判断 |
| `Dead` | しない | 体力0以下 | ラウンドの終わりまで続く |

技を始めると、`State`を`Attack`、`CurrentMove`を技番号にし、`PlayerFlags.HasHitThisMove`を消します。`Attack`と`Throwing`以外に移る時は`CurrentMove`を`NoMove`にします。

### 起き上がりの入力（ADR-0021）

`Down`に入った時に`PlayerFlags.DownHitTaken`を消します。`Down`の間は、毎フレーム次の順で判定します。

1. `StateFrame`が`TechFrames`未満で、P、K、Gがそろって押され、そのうち1つ以上がこのフレームに押された時は`Tech`です。レバーの上下で移動の向きを決め、中立ならその場です。
2. `StateFrame`が`DownMinFrames`未満なら、ほかの入力を受け付けません。
3. 上が新しく入ったら`RollIn`、下方向が新しく入ったら`RollOut`です。
4. PかKが押されたら、そのボタンをコマンドに持つ`RisingAttack`の技を始めます。技が無いボタンは無視します。
5. Gが押されたら`Rise`です。
6. `StateFrame`が`DownMaxFrames`に達したら`Rise`です。

## 画面上の左右と方向の数字

`CameraSide`は、毎フレームADR-0016の手順で`CameraYaw`を更新し、画面の左に立つプレイヤーを決めます。コマンド判定で使う方向の数字は、`InputFrame`から次の式で求めます。

```text
v = (Up ? 1 : 0) - (Down ? 1 : 0)
h = 画面の左に立つなら (Right ? 1 : 0) - (Left ? 1 : 0)
    画面の右に立つなら (Left ? 1 : 0) - (Right ? 1 : 0)
digit = 5 + 3 * v + h
```

入力履歴の過去のフレームも、判定するフレームの左右で読み替えます。

## 1フレームの処理の順序

`Step`は次の順で新しい状態を作ります。各手順は`Sim/`の関数で、状態を書き換えずに新しい値を返します。

1. 入力の保存 両方の入力の`IsNormalized`を確かめ、偽なら`ArgumentException`です。各プレイヤーの`Inputs`に`Push`し、`LastHitKind`を`None`に戻します。
2. 段階の進行 段階が`Intro`なら`PhaseFrames`を増やし、`IntroFrames`に達したら`Fight`にします。`RoundEnd`なら`PhaseFrames`を増やし、`RoundEndFrames`に達したら、試合が終わっていれば`MatchEnd`、終わっていなければ次のラウンドの開始位置と`Intro`にします。`MatchEnd`なら`PhaseFrames`を増やすだけで、上限の65535で止めます。`Fight`以外では手順13へ進みます。
3. ヒットストップ 残り（`HitstopFrames`）が1以上のプレイヤーは1減らし、このフレームは「止まっている」とします。止まっているプレイヤーは手順4から8を行わず、投げの判定もしません。やられ判定は残ります。
4. 状態の経過 止まっていないプレイヤーの`StateFrame`を増やし（上限65535）、`StunFrames`が1以上なら1減らします。状態の表の「終わる条件」、起き上がりの入力、投げ抜けの入力、投げの終わりの処理を行います。
5. 行動の判断 行動を受け付けるプレイヤーについて、ADR-0017の順で行動を選びます。
6. 移動 状態から`Velocity`を決め、`Position`に足して、各成分を`±PositionLimitMeters`に収めます。向きは`Trig.Direction`で求めます。前歩き、後ろ歩き、ダッシュは`Facing`の前後、横移動と転がりと受け身はADR-0016の向き、`Attack`は経過フレームを含む`Motion`の`ForwardPerFrame`だけ`Facing`の前へ動きます。ほかの状態では`Velocity`は0です。
7. 体の押し合いと壁 両者をADR-0015の手順で押し出します。どちらかが`Throwing`か`Thrown`なら押し合いは行いません。続けて、ADR-0002の手順で壁の縁から押し戻し、座標を`±PositionLimitMeters`に収めます。
8. 向きの更新 対象はADR-0015に挙げた状態のプレイヤーだけで、相手の足元への角度を`Trig.Atan2(-dz, dx)`で求めて`Facing`にします。2人の水平距離が0なら変えません。
9. 画面の左右 画面の右方向（`CameraYaw`）を`CameraSide`で更新します。
10. 投げの判定 状態が`Attack`で投げの技、`StateFrame`が`Startup`、止まっていないプレイヤーについて、ADR-0019の条件で投げの候補を作ります。
11. 打撃の判定と適用 下の節の手順で、2人分の結果を求めてからまとめて適用します。続けて、このフレームに命中かカウンターヒットを受けていない投げた側の候補を成立させます。2人の投げが同時に成立する時は、2人とも`ThrowEscape`にします。
12. 決着 ラウンドの決着をADR-0022の手順で判定します。トレーニングでは、リングの外に出たプレイヤーがいれば`ResetPositions`と同じ状態にします。
13. フレームの更新 フレーム番号（`FrameNumber`）を増やします。`Fight`でトレーニングでなく、`RoundTimerFrames`が1以上なら1減らします。

### 打撃の判定と適用（ADR-0006、ADR-0018、ADR-0020、ADR-0021）

攻撃する側は、`Attack`で技の`Kind`が`Strike`か`RisingAttack`、`HasHitThisMove`が消えていて、`StateFrame`を含む判定区間に攻撃判定があるプレイヤーです。攻撃判定とやられ判定のどちらも、カプセルの両端のワールド座標は、足元の座標に`Trig.Rotate(端点, Facing)`を足して求めます。

受ける側のやられ判定は、状態で次のように決まります。

| 受ける側の状態 | やられ判定 |
|---|---|
| `Down`、`RollIn`、`RollOut` | `DownHurtCapsules`。技の`HitsDown`が真で、`DownHitTaken`が消えている時だけ判定する |
| `Attack` | 判定区間の`Hurt`。空なら技の`Posture`に合う立ちかしゃがみの判定 |
| `Crouch`、`CrouchGuard`、`CrouchBlockstun` | `CrouchHurtCapsules` |
| `Throwing`、`Thrown`、`Dead` | 無し（当たらない） |
| それ以外 | `StandHurtCapsules` |

カプセルが1組でも重なれば、ADR-0018の表で結果を決めます。受ける側が`Attack`で`StateFrame`が`Startup + Active`未満なら、命中はカウンターヒットです。2人分の結果を、判定の前の状態から求めてから、次のとおり適用します。

| 結果 | 受けた側 | 攻撃した側 |
|---|---|---|
| ガード | 立ちガードなら`Blockstun`、しゃがみガードなら`CrouchBlockstun`。`StunFrames`は技の`Blockstun`、`Facing`の前へ`Pushback`離す、`LastHitKind`は`Guarded` | `HasHitThisMove`を付ける |
| 命中 | 体力から`Damage`を引く。`Knockdown`なら`Down`、違えば`Hitstun`で`StunFrames`は`Hitstun`。押し離す。`LastHitKind`は`Hit` | 同上 |
| カウンターヒット | `CounterDamage`、`CounterHitstun`、`CounterKnockdown`で命中と同じ処理。`LastHitKind`は`CounterHit` | 同上 |
| ダウン中への命中 | 体力から`Damage`を引き、`DownHitTaken`を付ける。状態と`StateFrame`は変えない。`LastHitKind`は`Hit` | 同上 |

ガード、命中、カウンターヒットのいずれかが起きたら、2人の`HitstopFrames`を、現在の値と、このフレームに当たった技の`Hitstop`の最大値にします。適用の後、体力が0以下のプレイヤーは`Dead`です。

### カプセルの重なり（ADR-0006）

`Collision.Overlaps`は、ワールド座標の2つのカプセルについて次の手順で重なりを判定します。座標は`Raw`の整数で、積と商は`Int128`で計算します。`/`はゼロ方向へ丸める整数除算、`>>`は負の無限大方向へ丸める算術シフトです。

```text
d1 = Q1 - P1, d2 = Q2 - P2, w = P1 - P2
a = d1・d1, e = d2・d2, f = d2・w
ONE = 65536
if a == 0 and e == 0: s = 0, t = 0
elif a == 0:          s = 0, t = clamp((f << 16) / e, 0, ONE)
else:
  c = d1・w
  if e == 0:          t = 0, s = clamp((-c << 16) / a, 0, ONE)
  else:
    b = d1・d2
    denom = a * e - b * b
    s = (denom != 0) ? clamp(((b * f - c * e) << 16) / denom, 0, ONE) : 0
    t = (b * s + (f << 16)) / e
    if t < 0:         t = 0,   s = clamp((-c << 16) / a, 0, ONE)
    elif t > ONE:     t = ONE, s = clamp(((b - c) << 16) / a, 0, ONE)
C1 = P1 + ((d1 * s) >> 16), C2 = P2 + ((d2 * t) >> 16)
重なり = |C1 - C2|^2 <= (r1 + r2)^2
```

P1とQ1、P2とQ2はそれぞれのカプセルの両端、r1とr2は半径です。

### 投げの入力と終わり（ADR-0019）

投げが成立したフレームに、投げられた側の入力履歴の直近`BufferFrames`フレームから、Gを含むボタンが押された最も古いフレームを探します。見つかればそのフレームの入力を、見つからなければ`Thrown`の間に最初にGを含むボタンが押されたフレームの入力を、投げ抜けの入力とします。判定は1回だけで、`PlayerFlags.ThrowEscapeTried`を付けた後の入力は対象外です。入力が投げのコマンドと一致すれば、2人を`ThrowEscape`にして、水平距離が`ThrowEscapeDistance`になるよう半分ずつ離します。

`Throwing`の`StateFrame`が`ThrowEscapeFrames`に達したら、投げられた側の体力から`Damage`を引き、位置を「投げた側の足元 + `Trig.Rotate(ThrowEndOffset, 投げた側のFacing)`」にして壁の縁から押し戻し、`Facing`を投げた側へ向けます。`Knockdown`なら`Down`、違えば`Hitstun`です。`LastHitKind`は`Thrown`で、体力が0以下なら`Dead`です。投げた側は`Attack`のまま`StateFrame`を`Startup + Active`にして、硬直に入ります。

## 決着（ADR-0022）

`RingBounds.IsOut(position, stage)`は、足元がリングアウトの縁の外側にあるかをADR-0002の式で判定します。`RingBounds.PushInsideWalls(position, bodyRadius, stage)`は、壁の縁からの押し戻しを行います。決着の判定は次の順です。

1. 体力が0以下か、リングの外のプレイヤーを負けとします。
2. 負けがいれば、負けでないプレイヤーの勝ち数を1増やします。2人とも負けなら2人の勝ち数を1ずつ増やします。
3. 負けがいなくて`RoundTimerFrames`が0なら、体力の多い方の勝ち数を1増やし、同じなら2人とも増やします。
4. 勝ち数が増えたら、`LastRoundReason`と`LastRoundWinners`を記録し、`Phase`を`RoundEnd`、`PhaseFrames`を0にします。
5. `RoundEnd`の終わりに、勝ち数が`RoundsToWin`以上で相手より多いプレイヤーがいるか、`RoundNumber`が`MaxRounds`なら`MatchEnd`です。そうでなければ`RoundNumber`を増やして次のラウンドを始めます。

乱数（`RngState`）は現在の規則では使わず、状態の一部として保持します。
