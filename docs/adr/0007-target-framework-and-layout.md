# ADR-0007：対象フレームワークはnet10.0にし、プロジェクトをGame、Core、テストに分ける

## 状態

採用

## 背景

一次情報として次を確認しました。

- Godot 4.7の公式ドキュメントは.NET 8以降を要求します。
- Godot.NET.Sdk 4.7.2はnet8.0とnet10.0の両方で`dotnet build`に成功しました。
- 開発機のランタイムは10.0.11だけで、SDKは10.0.400です。
- .NET公式のリリース情報では、8.0はLTSでサポート終了が2026-11-10、10.0はLTSで2028-11-14です。
- Godotの公式ドキュメントは、Godotプロジェクトの`.csproj`を`project.godot`と同じ階層に置く前提で書かれています。`Godot.NET.Sdk`は既定でその階層以下の全`.cs`をコンパイル対象にします。

| 軸 | net10.0 | net8.0 |
|---|---|---|
| サポート期限 | 2028-11-14 | 2026-11-10 |
| 開発機での実行 | そのまま動く | ランタイム8の追加が要る |
| C#の言語版 | C# 14 | C# 12 |

## 決定

- 全プロジェクトの対象フレームワークはnet10.0です。
- プロジェクトと置き場所は次のとおりです。

| プロジェクト | 場所 | SDK | 役割 |
|---|---|---|---|
| `Godot3dFighter` | リポジトリ直下の`Godot3dFighter.csproj` | `Godot.NET.Sdk/4.7.2` | 描画、入力の受け渡し、画面遷移、画面なし走破の実行器 |
| `Core` | `src/Core/Core.csproj` | `Microsoft.NET.Sdk` | 格闘ロジック。Godotの型を参照しない |
| `Core.Tests` | `tests/Core.Tests/Core.Tests.csproj` | `Microsoft.NET.Sdk` | Coreのユニットテスト |
| `GenTables` | `tools/GenTables/GenTables.csproj` | `Microsoft.NET.Sdk` | sin表とatan表のC#ソースを生成するコンソールアプリ |
| 走破シナリオ | `tests/e2e/` | 無し（JSONのみ） | 主要導線のシナリオと導線一覧 |

- `Godot3dFighter.csproj`は`<Compile Remove="src/**;tests/**;tools/**" />`で他プロジェクトのソースを除外し、`Core`を`ProjectReference`で参照します。
- Game側のソースは`game/`に置きます。Godotのシーンは`scenes/`、技データとステージデータは`data/`です。
- 共通のビルド設定は直下の`Directory.Build.props`に置きます（ADR-0009）。
- ソリューション`Godot3dFighter.sln`には`Godot3dFighter`、`Core`、`Core.Tests`、`GenTables`を含めます。走破シナリオはJSONだけなので含めません。

## 影響

- ランタイムやSDKの追加なしに、開発機でビルドとテストが動きます。
- 直下の`.csproj`の`Compile Remove`を外すと、CoreとテストがGodot側で二重にコンパイルされます。この設定は変更しません。
- 不変条件C-12として「`Core`はGodotSharpを参照しない」を置き、`Core.Tests`の`AssemblyReferenceTests`で`Core`アセンブリの参照一覧を確かめます。
