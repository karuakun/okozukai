using Okozukai.Api.Data;
using Okozukai.Api.Endpoints;
using Okozukai.Api.Services;
using Okozukai.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ─── 認証・認可 ────────────────────────────────────────────────
// OIDC JWTベアラー認証。Auth0 / Azure AD B2C / Firebase Auth のいずれかを想定。
// appsettings.json の Auth:Authority と Auth:Audience を設定する。
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = builder.Configuration["Auth:Audience"];
        // 開発環境ではHTTPS検証を緩和可能
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();

// ─── CORS（Blazor WASM / CLIからのアクセス許可） ──────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

// ─── アプリケーションサービス ───────────────────────────────────
// ユーザーDBプロバイダー（オブジェクトストレージ ↔ ローカル一時ファイル）
builder.Services.AddScoped<IUserDbProvider, BlobStorageUserDbProvider>();
// EF Core DbContextはリクエストスコープでユーザーごとに生成
builder.Services.AddScoped<UserDbContextFactory>();
// リポジトリ・サービス
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<CategoryService>();

// ─── インフラ ───────────────────────────────────────────────────
builder.Services.AddSingleton(
    new Azure.Storage.Blobs.BlobServiceClient(
        builder.Configuration["Azure:StorageConnectionString"]));

// ─── OpenAPI ────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // /scalar/v1 で対話型APIドキュメント
}

// ─── エンドポイント登録 ─────────────────────────────────────────
app.MapTransactionEndpoints();
app.MapBudgetEndpoints();
app.MapCategoryEndpoints();

// ヘルスチェック（FaaSのウォームアップ用）
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

app.Run();
