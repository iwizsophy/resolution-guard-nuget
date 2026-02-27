# 開発ガイド

この文書はメンテナーおよびコントリビューター向けです。

## 前提条件

- .NET SDK がインストールされていること
- 本リポジトリでの動作確認 SDK バージョン:
  - 8.0.x
  - 10.0.x

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
