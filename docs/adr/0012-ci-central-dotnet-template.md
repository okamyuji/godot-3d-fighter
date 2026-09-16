# ADR-0012：CIは中央リポジトリのdotnet用テンプレートを呼び、Godotは公式配布物を検証して使う

## 状態

採用（2026-09-17）

## 背景

CIの共通部品は`okamyuji/reusable-workflows`に集約し、各リポジトリは薄い呼び出しだけを持ちます。現時点の中央リポジトリにはgo-ci、node-ci、rails-ci、security-scanの4本があり、dotnet用はありません。本リポジトリの`ci.yml`はsecurity-scanをコミットSHAで版を固定して呼んでいます。

| 軸 | 中央に`dotnet-ci.yml`を足す | 本リポジトリに直接書く |
|---|---|---|
| 共通部品の方針 | 合致する | 反する。`check-drift.sh`が差分として検出する |
| 他のdotnetリポジトリでの再利用 | できる | できない |
| 作業範囲 | 中央リポジトリの変更とタグ付け替え | 本リポジトリだけ |

主要導線の走破にはCI上でGodot本体が要ります。GitHub Releasesの公式zip（`Godot_v4.7.2-stable_mono_linux_x86_64.zip`）を、同じリリースの`SHA512-SUMS.txt`に載るSHA-512で検証して使う方式と、第三者のコンテナイメージを比べました。公式配布物は供給元がgodotengineで、チェックサム検証で改ざんを検出できます。

## 決定

- 中央リポジトリに`.github/workflows/dotnet-ci.yml`を追加します。入力は`dotnet-version`（既定`10.0.x`）、`godot-version`（既定`4.7.2`）、`godot-sha512`（必須）、`run-e2e`（既定true）です。
- `dotnet-ci.yml`のジョブは、書式検査、ビルド、ユニットテストとカバレッジ、CRAP値、変異テスト、主要導線の走破です。条件はADR-0010のとおりです。
- 本リポジトリの`ci.yml`は、security-scanと`dotnet-ci.yml`をコミットSHAで版を固定して呼びます。中央リポジトリの更新はSHAを書き換えて取り込みます。
- Godot本体は公式zipを取得し、呼び出し側が渡したSHA-512と照合してから展開します。値はリリースの`SHA512-SUMS.txt`から写します。zipはactionsのキャッシュに保存します。
- `dotnet-ci.yml`が使う外部のアクションは、すべてコミットSHAで版を固定します。

## 影響

- 中央リポジトリの変更は`central-workflow-release`の手順（タグ付け替え）で公開します。
- `.github/workflows/`配下を含むpushには、`workflow`スコープを持つ認証が要ります。
- `dotnet-ci.yml`が中央に無い間、本リポジトリのCIはsecurity-scanだけが走ります。
