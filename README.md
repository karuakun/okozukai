# おこづかい帳 (okozukai)

個人向け収支管理アプリケーション。Blazor WebAssembly + サーバレスAPIで構成され、ブラウザとCLIの両方から利用できます。

## 特徴

- **マルチクライアント** — ブラウザ（Blazor WASM）とターミナル（CLIツール）から同じAPIを利用
- **完全サーバレス** — Azure Functions 消費プランで稼働費用ほぼゼロ
- **データ分離** — ユーザーごとに独立したSQLiteデータベース（Azure Blob Storage保存）
- **ソーシャルログイン** — Google / Microsoft / Facebook（Auth0経由）

## クイックスタート

### 必要環境

- .NET 9 SDK
- Azurite（ローカルストレージエミュレーター）: `npm install -g azurite`
- Auth0 アカウント（無料）

### セットアップ

```bash
git clone <repo-url>
cd okozukai

# 環境変数設定
cp .env.example .env
# .env を編集して Auth0 の設定を記入

# Azurite 起動
azurite --silent --location /tmp/azurite &

# API 起動
dotnet run --project src/Okozukai.Api

# Blazor WASM 起動（別ターミナル）
dotnet run --project src/Okozukai.Web
```

### CLIの使い方

```bash
# インストール
dotnet tool install --global Okozukai.Cli

# ログイン
okozukai auth login

# 収支登録
okozukai tx add --amount -800 --desc "ランチ" --category 1

# 月次サマリ表示
okozukai summary

# 一覧表示
okozukai tx list --month 2025-01

# 予算設定
okozukai budget set --category 1 --limit 30000
```

## アーキテクチャ

```
                    ┌─────────────────────────────────────┐
                    │           Client Layer               │
                    │                                      │
        ┌───────────┴──────────┐  ┌───────────────────────┤
        │    Blazor WASM       │  │    CLI (okozukai)      │
        │  (Static Web Apps)   │  │  (dotnet global tool)  │
        └───────────┬──────────┘  └──────────┬────────────┘
                    │  JWT + REST             │ JWT + REST
                    └──────────┬──────────────┘
                               │
                    ┌──────────▼──────────────┐
                    │   Azure Functions API   │
                    │  (ASP.NET Minimal API)  │
                    │   消費プラン（無料枠）  │
                    └──────────┬──────────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
    ┌─────────▼──────┐ ┌──────▼───────┐ ┌──────▼───────┐
    │  Auth0 (OIDC)  │ │ Blob Storage │ │   /health    │
    │  Google/MS/FB  │ │ users/{id}/  │ │  (監視用)    │
    └────────────────┘ │ okozukai.db  │ └──────────────┘
                       └──────────────┘
```

## プロジェクト構成

| プロジェクト | 説明 |
|------------|------|
| `Okozukai.Core` | ドメインモデル・インターフェース・DTO |
| `Okozukai.Api` | REST API（FaaSデプロイ対象） |
| `Okozukai.Web` | Blazor WebAssembly フロントエンド |
| `Okozukai.Cli` | dotnet グローバルツール |

## コスト試算（Azure、月100ユーザー想定）

| リソース | プラン | 月額概算 |
|---------|-------|---------|
| Azure Static Web Apps | Free | ¥0 |
| Azure Functions | 消費プラン（100万req/月無料） | ¥0〜数十円 |
| Azure Blob Storage | LRS | ¥1〜10円 |
| Auth0 | Free（7500 MAU） | ¥0 |
| **合計** | | **¥0〜数十円** |

## ライセンス

MIT
