using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Okozukai.Web;
using Okozukai.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ─── 認証（OIDC PKCE フロー） ────────────────────────────────────
// Auth0 / Azure AD B2C / Firebase いずれかの設定を wwwroot/appsettings.json に記載
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Auth:Oidc", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code"; // PKCE
});

// ─── API クライアント（認証済みHTTPクライアント） ──────────────────
builder.Services.AddHttpClient<OkozukaiApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]
                              ?? throw new InvalidOperationException("Api:BaseUrl is required"));
})
.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// ─── UI ────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ─── アプリケーションサービス ────────────────────────────────────
builder.Services.AddScoped<TransactionStateService>();
builder.Services.AddScoped<BudgetStateService>();

await builder.Build().RunAsync();
