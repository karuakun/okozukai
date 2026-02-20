using System.Text.Json;

namespace Okozukai.Cli.Commands;

/// <summary>
/// CLIコマンドから使うHTTPクライアントを生成する。
/// ~/.okozukai/token.json に保存済みのJWTトークンをAuthorizationヘッダーに付与する。
/// </summary>
public static class ApiClientFactory
{
    private static readonly string ConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                     ".okozukai", "config.json");

    private static readonly string TokenPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                     ".okozukai", "token.json");

    public static HttpClient Create()
    {
        var baseUrl = GetBaseUrl();
        var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

        if (File.Exists(TokenPath))
        {
            var json    = File.ReadAllText(TokenPath);
            var doc     = JsonDocument.Parse(json);
            var token   = doc.RootElement.GetProperty("access_token").GetString();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private static string GetBaseUrl()
    {
        if (File.Exists(ConfigPath))
        {
            var json = File.ReadAllText(ConfigPath);
            var doc  = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("apiUrl", out var urlEl))
                return urlEl.GetString() ?? DefaultUrl;
        }
        return DefaultUrl;
    }

    private const string DefaultUrl = "https://localhost:7100";
}
