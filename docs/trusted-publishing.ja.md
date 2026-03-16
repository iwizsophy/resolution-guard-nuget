# Trusted Publishing ガイド

この文書は `ResolutionGuard.NuGet` の現在の公開モデルをまとめたものです。

## 推奨方針

長期有効な API キーではなく、GitHub Actions と OpenID Connect を使った NuGet Trusted Publishing を推奨します。

## 設定チェックリスト

1. nuget.org 上で `ResolutionGuard.NuGet` パッケージを作成または予約します。
2. `develop` からの tag publish を有効にする場合は、`int.nugettest.org` 上でも `ResolutionGuard.NuGet` パッケージを作成または予約します。
3. この GitHub リポジトリを nuget.org 側パッケージの Trusted Publisher として登録します。
4. `develop` publish を有効にする場合は、この GitHub リポジトリを `int.nugettest.org` 側パッケージの Trusted Publisher としても登録します。
5. Trusted Publishing policy には workflow file 名として `publish.yml` を登録します。`.github/workflows/` のパスは付けません。
6. publish に使うアカウント名を repository secret `NUGET_USER` に設定します。
7. `.github/workflows/publish.yml` で `permissions.id-token: write` を維持します。
8. `v1.2.3` のように `v<major>.<minor>.<patch>` 形式の release tag を作成して push します。
9. パッケージ/アセンブリ バージョンは `RelaxVersioner` により git タグから解決されます。

## ワークフローの動作

- `.github/workflows/publish.yml` は tag ベースの publish を担当します。
- publish 先は tag 対象 commit の到達可能 branch で決まります。
  - `main` に到達可能な commit: `https://api.nuget.org/v3/index.json`
  - `develop` に到達可能で `main` には到達不能な commit: `https://apiint.nugettest.org/v3/index.json`
- 認証には `NuGet/login@v1` を使います。
- nuget.org 側は `audience=https://www.nuget.org` と `token-service-url=https://www.nuget.org/api/v2/token` を使います。
- `int.nugettest.org` 側は `audience=https://int.nugettest.org` と `token-service-url=https://int.nugettest.org/api/v2/token` を使います。
- テスト用 feed は、feed host が `apiint.nugettest.org`、OIDC token host が `int.nugettest.org` で異なります。
- publish 前に build を実行します。
- publish 時の pack step では、生成された build output による version の意図しない繰り上がりを防ぐため、`RelaxVersioner` の working-directory dirty check を無効化します。
- upload 前に、生成された `.nupkg` のファイル名が release tag の version と一致することを検証します。

## Release チェックリスト

- `CHANGELOG.md` を更新済み
- 英語版と日本語版の docs の同期を確認済み
- dependency 変更を含む場合は `THIRD-PARTY-NOTICES.md` を更新済み
- intended publish branch から release tag を作成済み
- publish 先（`main` なら nuget.org、`develop` なら `apiint.nugettest.org`）を確認済み
