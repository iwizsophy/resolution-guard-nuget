# コントリビュートガイド

ResolutionGuard.NuGet への貢献を歓迎します。

## 開始前

- バグ報告、機能提案、ユーザーに見える動作変更、設定/スキーマ変更、publish workflow の変更、dependency の追加や大きな更新は、まず Issue を作成してください。
- 変更は可能な限り小さく、目的を明確にしてください。
- default branch は `main` です。
- integration branch は `develop` です。
- 標準の branch flow は `feature/* -> develop`、`bugfix/* -> develop`、`chore/* -> develop`、release は `develop -> main` です。
- `develop` または `main` に入る変更は Pull Request 経由を基本としてください。
- 英語版と日本語版の文書は、可能な限り同じ変更で更新してください。片方だけになる場合は follow-up issue を作成してください。
- 設定サーフェスを変更する場合は、`README.md`、`README.ja.md`、`nuget-resolution-guard.schema.json` を合わせて更新してください。
- dependency を変更した場合は、同じ Pull Request で `THIRD-PARTY-NOTICES.md` を更新してください。
- リリースに見える変更は `CHANGELOG.md` を更新してください。
- 議論やコントリビュートは歓迎しますが、最終判断はメンテナーが行います。

## 開発フロー

1. Fork して作業ブランチを作成します。
2. 動作、リリースフロー、文書ポリシー、dependency ポリシーに影響する変更では Issue を作成または更新します。
3. 実装とあわせて、テスト、検証更新、または文書更新を行います。
4. ローカルで次を実行します:
   - `dotnet restore ResolutionGuard.NuGet.slnx`
   - `dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore`
   - `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net8.0 --no-build`
   - `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net9.0 --no-build`
   - `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net10.0 --no-build`
   - `dotnet pack src/ResolutionGuard.NuGet.Package/ResolutionGuard.NuGet.Package.csproj -c Release --no-build -o artifacts`
   - `dotnet pack` の検証では、pack 時に SPDX SBOM を生成して同梱するため、`syft` が `PATH` 上にある必要があります。
5. Pull Request には以下を記載してください:
   - 変更内容
   - 変更理由
   - 検証結果
   - マージ先 branch（通常は `develop`、release PR のみ `main`）

## リリースと publish に関する補足

- release tag は `v<major>.<minor>.<patch>` 形式を使ってください。
- 安定版 release は `develop -> main` の release PR をマージしたあと、その merge commit から tag を作成してください。
- `main` に到達可能な tag 対象 commit は `https://api.nuget.org/v3/index.json` に publish されます。
- `develop` に到達可能で `main` には到達不能な tag 対象 commit は `https://apiint.nugettest.org/v3/index.json` に publish されます。
- publish は GitHub Actions Trusted Publishing と `NuGet/login@v1`、repository secret `NUGET_USER` を使います。
- publish 時の pack step では、生成物による version の意図しない繰り上がりを防ぐため、`RelaxVersioner` の working-directory dirty check を無効化します。
- publish workflow は upload 前に、生成された `.nupkg` のファイル名が release tag の version と一致することを検証します。
- 安定版 release の文書更新では、`CHANGELOG.md` の `Unreleased` から対象 version section へ内容を移し、README の install 例も新 version に更新してください。

## 追加ドキュメント

- 開発詳細: `docs/development.ja.md`
- NuGet.org Trusted Publishing: `docs/trusted-publishing.ja.md`
- 行動規範: `CODE_OF_CONDUCT.md`
- サポートポリシー: `.github/SUPPORT.md`
