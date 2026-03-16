# ResolutionGuard.NuGet

![ResolutionGuard.NuGet アイコン](https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/main/assets/icon.png)

`ResolutionGuard.NuGet` は、Restore 後の `obj/project.assets.json` を解析し、プロジェクト間で発生する NuGet パッケージバージョン不一致を検出する MSBuild Task パッケージです。

## 免責事項

- NuGet の依存関係解決を補助する非公式ツールです。
- Microsoft および NuGet とは提携・関係がありません。

## プロジェクト方針

このプロジェクトは、実践的なコラボレーションと敬意ある技術的な議論を重視します。
コントリビュートは歓迎しますが、最終判断はメンテナーが行います。

## 背景

NuGet の依存関係解決はエントリープロジェクトごとに行われます。
そのため、同一ソリューション内でも起動ポイントが異なると同じパッケージが異なるバージョンに解決されることがあり、NU1605/NU1107 で検知できないケースがあります。
本パッケージは、その差分を Restore 後に検出します。

## セキュリティと動作

- 本パッケージは `.props/.targets` によりビルド動作へ介入します。
- Task は有効時に `AfterTargets="Build"` で実行されます。
- 読み取るのはローカルファイル（`**/obj/project.assets.json`、任意の `nuget-resolution-guard.json`、任意の `.sln` / `.slnx`）のみです。
- 外部コマンド実行やネットワーク通信は行いません。
- 既定は opt-in（`ResolutionGuardNuGetEnabled=false`）です。

## 対応環境

`ResolutionGuard.NuGet` は `netstandard2.0` を対象とする MSBuild Task パッケージとして提供されます。

次の条件を満たす SDK スタイルの .NET ビルド環境を想定しています。
- MSBuild Task を実行できること
- `netstandard2.0` を対象とする Task アセンブリを含むパッケージを利用できること

現在の CI では、.NET 8 / 9 / 10 の SDK / MSBuild host で build-time task の動作を検証しています。

この検証範囲は、このリポジトリの現在のスモークテスト対象を示すものです。IDE や SDK の網羅的な対応表を意味するものではありません。

## インストール

```xml
<ItemGroup>
  <PackageReference Include="ResolutionGuard.NuGet" Version="1.3.0" PrivateAssets="all" />
</ItemGroup>
```

## 有効化

`Directory.Build.props`（または CI のプロパティ）で有効化します。

```xml
<Project>
  <PropertyGroup>
    <ResolutionGuardNuGetEnabled>true</ResolutionGuardNuGetEnabled>
  </PropertyGroup>
</Project>
```

## 設定ファイル

既定の検索ファイル名は `nuget-resolution-guard.json` です（プロジェクトディレクトリから親方向へ探索）。

MSBuild プロパティで上書きできます。

```xml
<ResolutionGuardNuGetConfigFile>path/to/nuget-resolution-guard.json</ResolutionGuardNuGetConfigFile>
```

例:

```json
{
  "mode": "warning",
  "scope": "repository",
  "directOnly": false,
  "runtimeOnly": false,
  "excludeEntrypoints": [
    "src/ExperimentalApp/ExperimentalApp.csproj"
  ],
  "excludePackageIds": [
    "Microsoft.NETCore.App",
    "Microsoft.AspNetCore.App"
  ],
  "rules": [
    { "packageId": "Newtonsoft.Json", "mode": "error" },
    { "packageId": "Plugin.SDK", "mode": "error", "versions": ["1.2.0", "1.3.0"] }
  ]
}
```

補足:

- 既定では、検出されたすべてのプロジェクトと PackageId が検査対象です。
- `scope` は任意です（既定 `repository`）。`solution` を指定すると、現在の solution ファイルに含まれるプロジェクトだけを検査します。
- `directOnly` は任意です（既定 `false`）。`true` の場合、直接参照されるパッケージのみを検査します。
- `runtimeOnly` は任意です（既定 `false`）。`true` の場合、実行時アセットを持つパッケージのみを検査します。
- `excludeEntrypoints` はエントリープロジェクトの除外リスト（ブラックリスト）です。
- `excludePackageIds` は検査対象パッケージの除外リスト（ブラックリスト）です。
- `rules[].mode` はその PackageId に対して全体 `mode` を上書きします。
- `rules[].versions` は任意です。列挙したバージョンは許容され、それ以外の解決バージョンに `rules[].mode` が適用されます。
- `scope=solution` のときは既定で `$(SolutionPath)` を使います。solution ファイルが取得できない場合は warning を出して `repository` にフォールバックします。

## MSBuild プロパティ

- `ResolutionGuardNuGetEnabled`（`true|false`）
- `ResolutionGuardNuGetEmitSuccessMessage`（`true|false`、既定 `false`。`true` のとき、不一致がなければ成功メッセージを出力）
- `ResolutionGuardNuGetConfigFile`
- `ResolutionGuardNuGetMode`（`off|info|warning|error`）
- `ResolutionGuardNuGetScope`（`repository|solution`）
- `ResolutionGuardNuGetDirectOnly`（`true|false`）
- `ResolutionGuardNuGetRuntimeOnly`（`true|false`）
- `ResolutionGuardNuGetSolutionFile`（`.sln` / `.slnx` パス上書き）
- `ResolutionGuardNuGetExcludedEntrypoints`（除外する csproj パスを `;` 区切りで指定）
- `ResolutionGuardNuGetExcludedPackageIds`（除外する PackageId を `;` 区切りで指定）
- `ResolutionGuardNuGetRepositoryRoot`（探索ルート上書き）

## 出力動作

- `error`: ビルド失敗（`Log.LogError`）
- `warning`: 警告のみ（`Log.LogWarning`）
- `info`: 情報メッセージのみ（`Log.LogMessage`）
- `off`: そのパッケージでは出力なし
- `ResolutionGuardNuGetEmitSuccessMessage=true` の場合、不一致なしで完了したときに成功メッセージを出力します。

## ドキュメント

- 英語版ユーザーガイド: `README.md`
- コントリビュートガイド: `CONTRIBUTING.ja.md`
- 行動規範: `CODE_OF_CONDUCT.md`
- 開発者向けガイド: `docs/development.ja.md`
- Trusted Publishing ガイド: `docs/trusted-publishing.ja.md`
- セキュリティポリシー: `SECURITY.md`
- サポートポリシー: `.github/SUPPORT.md`
- 変更履歴: `CHANGELOG.md`

## ライセンス

- 本プロジェクトは MIT License の下で提供されています。
- ライセンス全文は `LICENSE` を参照してください。
- 法的な解釈・効力は `LICENSE` の英語原文に従います。
- サードパーティ ライセンス情報は `THIRD-PARTY-NOTICES.md` を参照してください。
