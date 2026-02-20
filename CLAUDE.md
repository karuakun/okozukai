# CLAUDE.md

AIアシスタント向けのコードベースガイドです。このリポジトリで作業する際に参照してください。

---

## プロジェクト概要

**okozukai（おこづかい帳）** — 個人向け収支管理アプリケーション。

### 設計方針
- **Blazor WebAssembly** でブラウザ完結のフロントエンド
- **ASP.NET Core Minimal API** でバックエンドを構築し、CLIからも同じAPIを利用
- **FaaS（Azure Functions 消費プラン）** でサーバレス実行 → ほぼ無料で稼働
- **ユーザーごとにSQLiteファイルを作成** → Azure Blob Storage に保存
- **Auth0経由でソーシャルログイン**（Google / Microsoft / Facebook）

---

## ディレクトリ構成

```
okozukai/
├── okozukai.sln                    # ソリューション
├── .env.example                    # 環境変数テンプレート
├── .gitignore
│
├── src/
│   ├── Okozukai.Core/              # 共有ドメイン層（フレームワーク非依存）
│   │   ├── Models/                 # エンティティ (Transaction, Category, Budget)
│   │   ├── Interfaces/             # リポジトリ・サービスのインターフェース
│   │   └── DTOs/                   # Request/Response の記録型
│   │
│   ├── Okozukai.Api/               # ASP.NET Core Minimal API（FaaSデプロイ対象）
│   │   ├── Program.cs              # DI・ミドルウェア・エンドポイント登録
│   │   ├── Endpoints/              # エンドポイント定義（拡張メソッド）
│   │   ├── Services/               # ビジネスロジック + BlobStorageUserDbProvider
│   │   ├── Data/                   # EF Core DbContext + Factory
│   │   └── Middleware/             # UserContextExtensions（JWTからuserId取得）
│   │
│   ├── Okozukai.Web/               # Blazor WebAssembly（静的ファイル配信）
│   │   ├── Program.cs              # OIDC認証 + HttpClient + MudBlazor登録
│   │   ├── Pages/                  # .razor ページ
│   │   ├── Components/             # 再利用可能なコンポーネント
│   │   ├── Services/               # OkozukaiApiClient + StateService
│   │   └── wwwroot/
│   │       └── appsettings.json    # Auth0/API URL 設定（公開情報のみ）
│   │
│   └── Okozukai.Cli/               # dotnet tool（CLIクライアント）
│       ├── Program.cs              # System.CommandLine ルート登録
│       └── Commands/               # auth / tx / budget / summary コマンド
│
├── tests/
│   ├── Okozukai.Api.Tests/         # APIのユニット・統合テスト
│   └── Okozukai.Core.Tests/        # ドメインロジックのユニットテスト
│
├── infra/
│   └── azure/
│       ├── bicep/main.bicep        # Azure Bicep（IaC）
│       └── staticwebapp.config.json # Static Web Apps ルーティング設定
│
└── .github/
    └── workflows/deploy.yml        # CI/CD（GitHub Actions）
```

---

## アーキテクチャ詳細

### データフロー（FaaS + SQLite on Blob Storage）

```
[ユーザー] → [Blazor WASM / CLI]
                   ↓ HTTP + JWT
          [Azure Functions API]
                   ↓
     [BlobStorageUserDbProvider]
       1. Blob Lease 取得（排他制御）
       2. users/{userId}/okozukai.db をダウンロード → /tmp/
       3. EF Core で SQLite 操作
       4. /tmp/okozukai.db を Blob へアップロード
       5. Lease 解放
```

### 認証フロー

```
[ブラウザ / CLI]
    │
    ├─ (OIDC PKCE フロー)──→ [Auth0]
    │                              ↓ JWT (access_token)
    ├──────── Authorization: Bearer <JWT> ──────→ [API]
    │                                               ↓ JWKS 検証
    │                                          [エンドポイント処理]
```

- **Blazor WASM**: OIDC PKCE フロー（`Microsoft.AspNetCore.Components.WebAssembly.Authentication`）
- **CLI**: OAuth2 Device Authorization Grant（ブラウザ不要）
- **API**: JWT Bearer 検証（`sub` クレームをユーザーIDとして使用）

### ユーザーDB の命名規則

```
Blob Storage コンテナ: okozukai-userdata
Blob パス:            users/{auth0_sub}/okozukai.db
例:                   users/google-oauth2|123456789/okozukai.db
```

---

## 技術スタック

| 領域 | 選択 | 理由 |
|------|------|------|
| ランタイム | .NET 9 | 最新LTS、AOT対応でFaaS起動高速化が将来可能 |
| API | ASP.NET Core Minimal API | 軽量、FaaSと相性良い |
| ORM | EF Core 9 + SQLite | ユーザーごとDB戦略に最適 |
| フロントエンド | Blazor WebAssembly | C#のみで完結、静的配信可能 |
| UI | MudBlazor | Material Design、Blazor向け成熟ライブラリ |
| CLI | System.CommandLine + Spectre.Console | 公式CLI FW + リッチ出力 |
| 認証 | Auth0 (OIDC) | Google/MS/Facebook を一元管理、無料7500 MAU |
| ストレージ | Azure Blob Storage | 安価、Lease機能で排他制御可能 |
| ホスティング | Azure Static Web Apps (Free) + Azure Functions (従量) | ほぼ無料 |
| IaC | Azure Bicep | ARM テンプレートより可読性高い |
| CI/CD | GitHub Actions | 無料枠で十分 |

---

## 開発セットアップ

### 必要ツール
- .NET 9 SDK
- Azure CLI (`az`)
- Azurite（Azure Storage ローカルエミュレーター）: `npm install -g azurite`

### ローカル起動

```bash
# 1. Azurite 起動（別ターミナル）
azurite --silent --location /tmp/azurite

# 2. API 起動
cd src/Okozukai.Api
dotnet run

# 3. Blazor WASM 起動（別ターミナル）
cd src/Okozukai.Web
dotnet run

# 4. API仕様書確認
open https://localhost:7100/scalar/v1
```

### CLI のローカル実行

```bash
cd src/Okozukai.Cli
dotnet run -- auth login
dotnet run -- tx list
dotnet run -- summary --month 2025-01
```

### CLI のグローバルインストール

```bash
dotnet pack src/Okozukai.Cli
dotnet tool install --global --add-source ./src/Okozukai.Cli/nupkg Okozukai.Cli
okozukai --help
```

---

## コーディング規約

### 全般
- nullable reference types 有効（`Nullable=enable`）
- `ImplicitUsings` 有効
- 非同期メソッドは常に `CancellationToken ct` を末尾引数として受け取る
- 日本語コメント可（ドメイン知識の記述に使う）

### API エンドポイント
- `src/Okozukai.Api/Endpoints/` に拡張メソッドとして定義
- グループ化: `MapGroup("/api/{resource}")` + `RequireAuthorization()`
- `ctx.GetUserId()` でユーザーIDを取得（`Middleware/UserContextExtensions.cs`）
- レスポンス: `Results.Ok()`, `Results.Created()`, `Results.NotFound()`, `Results.NoContent()`

```csharp
// Good: エンドポイントの典型的な形
group.MapPost("/", async (HttpContext ctx, MyService svc, MyRequest req, CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    var result = await svc.DoSomethingAsync(userId, req, ct);
    return Results.Created($"/api/resource/{result.Id}", result);
}).WithSummary("リソースを作成");
```

### DTOs
- `record` 型を使う（イミュータブル）
- 命名: `Create{Entity}Request`, `Update{Entity}Request`, `{Entity}Response`
- エンティティ → DTOの変換は `{Dto}.FromModel(entity)` スタティックメソッドで行う

### サービス層
- コンストラクタインジェクション（プライマリコンストラクター構文）
- `UserDbContextFactory` を通じて DbContext を取得（直接 `new` しない）
- 書き込み操作後は必ず `await dbFactory.CommitAsync(ct)` を呼ぶ（Blob書き戻し）

### Blazor
- 状態管理: `*StateService` パターン（`event Action? OnChange` で変更通知）
- `@implements IDisposable` で `OnChange` のイベント解除を忘れずに
- HttpClient は `OkozukaiApiClient`（型付きクライアント）経由で呼ぶ

---

## APIエンドポイント一覧

### Transactions
| Method | Path | 説明 |
|--------|------|------|
| GET | `/api/transactions` | 一覧（`from`, `to`, `categoryId` フィルター） |
| GET | `/api/transactions/summary` | 月次サマリ（`year`, `month`） |
| GET | `/api/transactions/{id}` | 詳細 |
| POST | `/api/transactions` | 作成 |
| PUT | `/api/transactions/{id}` | 更新 |
| DELETE | `/api/transactions/{id}` | 削除 |

### Budgets
| Method | Path | 説明 |
|--------|------|------|
| GET | `/api/budgets/progress` | 予算進捗（`year`, `month`） |
| PUT | `/api/budgets` | 予算のupsert |
| DELETE | `/api/budgets/{id}` | 削除 |

### Categories
| Method | Path | 説明 |
|--------|------|------|
| GET | `/api/categories` | 一覧 |
| POST | `/api/categories` | 作成 |
| PUT | `/api/categories/{id}` | 更新 |
| DELETE | `/api/categories/{id}` | 削除 |

### Other
| Method | Path | 説明 |
|--------|------|------|
| GET | `/health` | ヘルスチェック（認証不要） |
| GET | `/scalar/v1` | API仕様書（開発環境のみ） |

---

## テスト

```bash
# 全テスト実行
dotnet test okozukai.sln

# 特定プロジェクトのみ
dotnet test tests/Okozukai.Api.Tests

# カバレッジレポート
dotnet test --collect:"XPlat Code Coverage"
```

テストの方針:
- ドメインロジックは `Okozukai.Core.Tests` でユニットテスト
- APIエンドポイントは `Okozukai.Api.Tests` で `WebApplicationFactory` を使った統合テスト
- `BlobStorageUserDbProvider` のテストはAzurite に接続して実施

---

## デプロイ

### Azure リソース作成（初回のみ）

```bash
# リソースグループ作成
az group create -n okozukai-rg -l japaneast

# Bicep デプロイ
az deployment group create \
  -g okozukai-rg \
  -f infra/azure/bicep/main.bicep \
  -p environmentName=prd
```

### 手動デプロイ

```bash
# API
dotnet publish src/Okozukai.Api -c Release -o ./publish/api
func azure functionapp publish <FUNCTION_APP_NAME> --dotnet-isolated

# Blazor WASM
dotnet publish src/Okozukai.Web -c Release -o ./publish/web
az staticwebapp deploy \
  --name <STATIC_WEB_APP_NAME> \
  --app-location ./publish/web/wwwroot
```

---

## ブランチ戦略

| ブランチ | 用途 |
|---------|------|
| `main` | 本番リリース。直接プッシュ禁止 |
| `feature/*` | 機能開発 |
| `fix/*` | バグ修正 |
| `claude/*-<sessionId>` | AIアシスタントによる変更 |

---

## AIアシスタントへの注意事項

1. **読んでから編集** — ファイルを必ず Read してから Edit する
2. **`UserDbContextFactory.CommitAsync()` の呼び出し** — 書き込み操作後は必ず呼ぶ（Blob書き戻しが走る）
3. **ユーザーID** — APIでは常に `ctx.GetUserId()` で取得する（ハードコード禁止）
4. **マイグレーション** — `UserDbContext.OnModelCreating` を変更したら EF Coreマイグレーションが必要
5. **秘密情報** — `.env.example` にキーを追加したら実際の値は環境変数・Key Vaultに置く
6. **CLIとWebの対称性** — API追加時は `OkozukaiApiClient.cs`（Web）と対応するCLIコマンドも合わせて更新する
7. **このファイルを更新** — アーキテクチャ変更時は CLAUDE.md を最新状態に保つ
