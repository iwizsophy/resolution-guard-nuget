# コントリビュートガイド

ResolutionGuard.NuGet への貢献を歓迎します。

## 開始前

- バグ報告や機能提案は、まず Issue を作成してください。
- 変更は可能な限り小さく、目的を明確にしてください。
- 議論やコントリビュートは歓迎しますが、最終判断はメンテナーが行います。

## 開発フロー

1. Fork して作業ブランチを作成します。
2. 実装とあわせて、テストまたは検証更新を行います。
3. ローカルで次を実行します:
   - `dotnet restore ResolutionGuard.NuGet.slnx`
   - `dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore`
   - `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release`
4. Pull Request には以下を記載してください:
   - 変更内容
   - 変更理由
   - 検証結果

## 追加ドキュメント

- 開発詳細: `docs/development.ja.md`
- NuGet.org Trusted Publishing: `docs/trusted-publishing.ja.md`
- 行動規範: `CODE_OF_CONDUCT.md`
- サポートポリシー: `.github/SUPPORT.md`
