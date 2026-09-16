# ADR-0005：試合状態は参照を含まない構造体にし、履歴は呼び出し側が持つ

## 状態

採用

## 背景

ロールバック、リプレイ、トレーニングモードの巻き戻しは、あるフレームの試合状態を丸ごと保存して復元します。その保存単位の作り方と、履歴の持ち主を比べました。

| 軸 | 参照を含まない`struct` | `class`に`Clone()` | バイト列へ変換 |
|---|---|---|---|
| 保存と復元 | 値の代入1回 | 複製を手で書き、追加時に漏れる | 変換コスト |
| 同期ずれ検出用のハッシュ | メモリをそのまま読める | 項目ごとに計算 | バイト列から計算 |
| 制約 | 可変長は上限付きの固定長にする | 無し | 無し |

履歴をCoreが持つと、巻き戻しAPIとその境界テストがCoreに入ります。Coreを「状態と入力から次の状態を作る」だけに保てば、ロールバックを後から載せてもCoreは変わりません。

## 決定

- 試合状態は`MatchState`（`struct`）です。参照型の項目を持たず、配列は`[InlineArray]`の固定長です。
- `MatchState`と内包する`PlayerState`は`[StructLayout(LayoutKind.Sequential, Pack = 1)]`とし、項目をサイズの大きい順に並べます。詰め物のバイトを作らないためです。列挙型の項目（`StateKind`、`RoundPhase`）は基底型を`byte`にします。基底型を省くと`int`（4バイト）になり、設計書のサイズと合わなくなります。
- 同期ずれの検出は`MemoryMarshal.AsBytes`で得たバイト列のFNV-1a（64bit）で行います。ハッシュの乗算は桁あふれを前提とするため`unchecked`で書きます。
- Coreの入口は`MatchSimulator.Step(in MatchState state, InputFrame p1, InputFrame p2, MatchContext ctx)`で、新しい`MatchState`を返します。引数の状態は書き換えません。
- 履歴（過去のフレームの状態）はCoreの外が持ちます。Coreは現在の状態しか持ちません。
- `System.Random`はCoreで使いません（ADR-0009）。乱数が要る時のため、`MatchState`に`uint RngState`を置き、xorshift32で更新します。初期値は`Rules.Seed`（既定1）で、0にはしません。乱数の状態も試合状態の一部です。

## 影響

- ロールバックは、保存した`MatchState`を代入し、入力列を`Step`で再適用するだけになります。
- 可変長のデータは上限を決めて固定長にします。上限は設計書の「定数と上限」に一覧します。
- 常に成り立つ条件C-08として「`Unsafe.SizeOf<MatchState>()`が全項目のサイズの和と一致する（詰め物が無い）」を置き、`MatchStateLayoutTests`で確かめます。
- 常に成り立つ条件C-09として「同じ状態と同じ入力から`Step`は同じ状態と同じハッシュを返す」を置き、`MatchSimulatorTests`で確かめます。
