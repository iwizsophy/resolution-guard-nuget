# Trusted Publishing 設定

1. nuget.org 上で `ResolutionGuard.NuGet` パッケージを作成（またはパッケージ ID を予約）します。
2. nuget.org のパッケージ設定で、この GitHub リポジトリとワークフロー（`.github/workflows/publish.yml`）を Trusted Publisher として追加します。
3. nugettest.org のパッケージ設定でも、同じワークフローを Trusted Publisher として追加します。
4. `publish.yml` の `permissions.id-token: write` を維持します。
5. `v1.2.3` のようなバージョンタグを作成して push します。
6. 公開先はタグ付与コミットの所属ブランチで決まります。
   - `main` に含まれるコミット: `https://api.nuget.org/v3/index.json`
   - `develop` のみに含まれるコミット: `https://apiint.nugettest.org/v3/index.json`
7. パッケージ/アセンブリ バージョンは `RelaxVersioner` により git タグから解決されます。

このワークフローは `NuGet/login@v1` で認証し、短命の API キー出力を使って push します。
- `main` 系は `audience=https://www.nuget.org` と `token-service-url=https://www.nuget.org/api/v2/token` を使用します。
- `develop` 系は `audience=https://apiint.nugettest.org` と `token-service-url=https://apiint.nugettest.org/api/v2/token` を使用します。
