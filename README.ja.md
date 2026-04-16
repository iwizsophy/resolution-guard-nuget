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

NuGet の依存関係解決はエントリープロジェクトごとに個別に行われます。
そのため、同一のソリューション内であっても、起動ポイントが異なると同じパッケージが異なるバージョンに解決されることがあります。

実際の開発において、多数のプロジェクトを含むソリューションで複数のアプリケーションをビルド可能な構成を扱っていました。

親プロジェクトの参照を更新することで、多くのアプリケーションでは遷移的なバージョン解決により問題のないパッケージが使用されるようになりましたが、一部のアプリケーションでは更新が取り残され、子プロジェクトが保持していた問題のあるパッケージバージョンが解決され続けていました。

その結果、すでに修正済みであるはずのエラーが再度検知されるという事態が発生しました。

このような問題はビルドが成功している限り顕在化しにくく、「一部だけ古い依存関係が残る」という形で潜在的に発生します。
また、既存の警告（NU1605 / NU1107 など）でも、こうしたプロジェクト間の解決結果の不一致は検知されないケースがあります。

さらに、Visual Studio の通常の開発フローではパッケージバージョンの不一致が明示的なエラーや警告として現れにくく、部分的な更新漏れは人手での確認に依存しがちです。

しかし、複数プロジェクト・複数エントリーポイントの構成では、この確認を人手で行うことには限界があります。

この経験から、「プロジェクト間で実際にどのバージョンに解決されているか」をビルド時に検知できる仕組みの必要性を感じ、本ツールを開発しました。

`ResolutionGuard.NuGet` は、Restore 後の `project.assets.json` を横断的に解析し、プロジェクト間でのパッケージ解決結果の不一致を検出します。

このツールは、NuGet の依存関係解決そのものを変更するものではなく、「実際に解決された結果」という事実に基づいて差異を検出することを目的としています。

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

`.csproj`、`.fsproj`、`.vbproj` などの一般的な SDK スタイル project は、現在のサポート方針に含まれます。

## インストール

```xml
<ItemGroup>
  <PackageReference Include="ResolutionGuard.NuGet" Version="1.4.0" PrivateAssets="all" />
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

推奨するエディター連携:

- 設定ファイルに `$schema` を追加し、公開スキーマ URL `https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/main/nuget-resolution-guard.schema.json` を指定します。
- NuGet パッケージには `schema/nuget-resolution-guard.schema.json` と `sbom/ResolutionGuard.NuGet.spdx.json` も同梱していますが、これらはパッケージ完全性とオフライン参照のためです。schema は `.nupkg` 内に保持され、利用側プロジェクトへ `contentFiles` のリンクとして露出しません。エディター連携の正規入口は公開スキーマ URL とします。

例:

```json
{
  "$schema": "https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/main/nuget-resolution-guard.schema.json",
  "mode": "warning",
  "scope": "repository",
  "directOnly": false,
  "runtimeOnly": false,
  "includeEntrypoints": [
    "src/AppA/AppA.csproj",
    "src/AppB/AppB.csproj"
  ],
  "excludeEntrypoints": [
    "src/ExperimentalApp/ExperimentalApp.csproj"
  ],
  "includePackageIds": [
    "Newtonsoft.Json",
    "Plugin.SDK"
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
- `includeEntrypoints` は任意のエントリープロジェクト許可リスト（allowlist）です。`scope=solution` の場合は solution に含まれるプロジェクト集合をさらに絞り込みます。
- `excludeEntrypoints` はエントリープロジェクトの除外リスト（ブラックリスト）で、同じパスが include/exclude の両方にある場合は exclude が優先されます。
- entrypoint のパスには SDK スタイル project のパスを指定します。`.csproj`、`.fsproj`、`.vbproj` などの一般的な拡張子をサポートします。
- 実効 entrypoint 集合が確定している場合（たとえば `scope=solution` や `includeEntrypoints` を使う場合）、analyzer は repository root 全体を再帰走査せず、それらの project に対応する `obj/project.assets.json` を直接導出します。
- `includePackageIds` は任意の PackageId 許可リスト（allowlist）です。
- `excludePackageIds` は検査対象パッケージの除外リスト（ブラックリスト）で、同じ PackageId が include/exclude の両方にある場合は exclude が優先されます。
- `rules[].mode` はその PackageId に対して全体 `mode` を上書きします。
- `rules[].versions` は任意です。列挙したバージョンは許容され、それ以外の解決バージョンに `rules[].mode` が適用されます。
- `scope=solution` のときは既定で `$(SolutionPath)` を使います。solution ファイルが取得できない場合は warning を出して `repository` にフォールバックします。
- `scope=solution` のとき、対応する `project.assets.json` がまだないプロジェクトは warning として報告され、Restore されるまで解析対象から外れます。
- `project.assets.json` に `project.restore.projectPath` がない場合、fallback 解決はそのディレクトリ内で一意に特定できる SDK スタイル project ファイルだけを受け入れます。曖昧な場合は推測せず、warning を出してその assets file をスキップします。

## MSBuild プロパティ

- `ResolutionGuardNuGetEnabled`（`true|false`）
- `ResolutionGuardNuGetEmitSuccessMessage`（`true|false`、既定 `false`。`true` のとき、不一致がなければ成功メッセージを出力）
- `ResolutionGuardNuGetConfigFile`
- `ResolutionGuardNuGetMode`（`off|info|warning|error`）
- `ResolutionGuardNuGetScope`（`repository|solution`）
- `ResolutionGuardNuGetDirectOnly`（`true|false`）
- `ResolutionGuardNuGetRuntimeOnly`（`true|false`）
- `ResolutionGuardNuGetSolutionFile`（`.sln` / `.slnx` パス上書き）
- `ResolutionGuardNuGetIncludedEntrypoints`（含める SDK スタイル project パスを `;` 区切りで指定）
- `ResolutionGuardNuGetExcludedEntrypoints`（除外する SDK スタイル project パスを `;` 区切りで指定）
- `ResolutionGuardNuGetIncludedPackageIds`（含める PackageId を `;` 区切りで指定）
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
