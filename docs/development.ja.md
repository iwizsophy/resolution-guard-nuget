# 開発ガイド

この文書はメンテナーおよびコントリビューター向けです。

## 想定環境

`ResolutionGuard.NuGet` は `netstandard2.0` を対象とする MSBuild Task パッケージとして提供されます。

このリポジトリでの開発と検証は、次の条件を満たす SDK スタイルの .NET ビルド環境を想定しています。
- `dotnet restore`、`dotnet build`、`dotnet pack` を実行できること
- `netstandard2.0` を対象とする Task アセンブリを含むパッケージを利用できること

現在の CI では、.NET 8 / 9 / 10 の SDK / MSBuild host で build-time task の動作を検証しています。

この検証範囲は IDE の完全な対応表より狭く、網羅的な互換性保証ではなく、現在のスモークテスト対象として扱います。

## ビルドとテスト

```powershell
dotnet restore ResolutionGuard.NuGet.slnx
dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net8.0 --no-build
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net9.0 --no-build
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net10.0 --no-build
```

CI の host-validation job を単一の SDK / MSBuild host で再現するには、clean tree 上で次を実行します。

```powershell
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net8.0 -p:ResolutionGuardTestTargetFrameworks=net8.0
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net9.0 -p:ResolutionGuardTestTargetFrameworks=net9.0
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net10.0 -p:ResolutionGuardTestTargetFrameworks=net10.0
```

`ResolutionGuardTestTargetFrameworks` は test 専用プロパティで、smoke-test project を単一の target framework に絞り、host ごとの検証で restore assets を分離します。

## ローカルでの pack

```powershell
dotnet pack src/ResolutionGuard.NuGet.Package/ResolutionGuard.NuGet.Package.csproj -c Release --no-restore -o artifacts
```

ローカルで pack する場合は、`syft` が `PATH` 上にあることを前提とします。

package project は pack 中に SPDX JSON SBOM を生成し、生成された `.nupkg` へ `sbom/ResolutionGuard.NuGet.spdx.json` として同梱します。

## プロジェクト構成

- `src/ResolutionGuard.NuGet.Core`: 解析・設定解決ロジック
- `src/ResolutionGuard.NuGet.Tasks`: MSBuild Task 実装
- `src/ResolutionGuard.NuGet.Package`: NuGet パッケージング（`build`、`buildTransitive`、Task バイナリ）
- `tests/ResolutionGuard.NuGet.Tests`: スモークテスト
- `docs/`: コントリビューター向け・公開手順ドキュメント

## リリース

NuGet.org への公開は `docs/trusted-publishing.ja.md` を参照してください。
