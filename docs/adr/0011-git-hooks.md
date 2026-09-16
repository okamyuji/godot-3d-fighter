# ADR-0011：gitフックは`core.hooksPath`で配布する

## 状態

採用

## 背景

pre-commitで走らせる内容はADR-0010で決まっています。ここではフックの配布と有効化の方式を比べました。

| 軸 | `.githooks/`と`core.hooksPath` | Husky.Net 0.9.1 | lefthook |
|---|---|---|---|
| 追加の依存 | 無し（git標準） | dotnetツール1つ | 外部バイナリ1つ |
| 有効化 | clone後に`git config`を1回 | `dotnet tool restore`と`dotnet husky install` | `lefthook install` |
| 記述 | シェルスクリプト | JSON | YAML |

フックの中身は4コマンドなので、シェルスクリプトで足ります。

## 決定

- フックは`.githooks/pre-commit`（POSIX sh）としてコミットします。
- 有効化は`git config core.hooksPath .githooks`で、READMEの導入手順に書きます。
- `pre-commit`は次を順に実行し、1つでも失敗すればコミットを止めます。
  1. `tools/doclint.sh`
  2. `dotnet format Godot3dFighter.sln --verify-no-changes`
  3. `dotnet build Godot3dFighter.sln -c Debug`
  4. `dotnet test tests/Core.Tests -c Debug --no-build`

## 影響

- 依存を足さずに、全環境で同じ検査が走ります。
- Windowsでは、Git for Windowsに同梱の`sh`で動きます。
- フックを飛ばす操作（`--no-verify`）は使いません。
