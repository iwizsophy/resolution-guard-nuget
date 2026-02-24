# Trusted Publishing 設定

1. nuget.org 上で `ResolutionGuard.NuGet` パッケージを作成（またはパッケージ ID を予約）します。
2. nuget.org のパッケージ設定で、この GitHub リポジトリとワークフロー（`.github/workflows/publish.yml`）を Trusted Publisher として追加します。
3. `publish.yml` の `permissions.id-token: write` を維持します。
4. 例: `v1.0.0` のような git タグを作成してリリースします。

このワークフローは `NuGet/login@v1` で認証し、短命の API キー出力を使って push します。
