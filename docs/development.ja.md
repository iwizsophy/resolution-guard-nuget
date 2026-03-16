# 開発ガイド

この文書はメンテナーおよびコントリビューター向けです。

## 想定環境

`ResolutionGuard.NuGet` は `netstandard2.0` を対象とする MSBuild Task パッケージとして提供されます。

このリポジトリでの開発と検証は、次の条件を満たす SDK スタイルの .NET ビルド環境を想定しています。
- `dotnet restore`、`dotnet build`、`dotnet pack` を実行できること
- `netstandard2.0` を対象とする Task アセンブリを含むパッケージを利用できること

このリポジトリでは、IDE や SDK のバージョンごとの対応表は維持していません。

## ビルドとテスト

```powershell
dotnet restore ResolutionGuard.NuGet.slnx
dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release
```

## ローカルでの pack

```powershell
dotnet pack src/ResolutionGuard.NuGet.Package/ResolutionGuard.NuGet.Package.csproj -c Release --no-restore -o artifacts
```

## プロジェクト構成

- `src/ResolutionGuard.NuGet.Core`: 解析・設定解決ロジック
- `src/ResolutionGuard.NuGet.Tasks`: MSBuild Task 実装
- `src/ResolutionGuard.NuGet.Package`: NuGet パッケージング（`build`、`buildTransitive`、Task バイナリ）
- `tests/ResolutionGuard.NuGet.Tests`: スモークテスト
- `docs/`: コントリビューター向け・公開手順ドキュメント

## リリース

NuGet.org への公開は `docs/trusted-publishing.ja.md` を参照してください。
