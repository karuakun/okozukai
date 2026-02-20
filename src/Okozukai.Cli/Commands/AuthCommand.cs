using System.CommandLine;
using System.Text.Json;
using Spectre.Console;

namespace Okozukai.Cli.Commands;

/// <summary>
/// CLIからのOIDC認証コマンド。
/// Device Authorization Grant（デバイスフロー）を使用してブラウザなしで認証可能。
/// トークンは ~/.okozukai/token.json にキャッシュする。
/// </summary>
public static class AuthCommand
{
    private static readonly string TokenPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                     ".okozukai", "token.json");

    public static Command Create()
    {
        var cmd = new Command("auth", "認証管理");
        cmd.AddCommand(CreateLoginCommand());
        cmd.AddCommand(CreateLogoutCommand());
        cmd.AddCommand(CreateStatusCommand());
        return cmd;
    }

    private static Command CreateLoginCommand()
    {
        var cmd = new Command("login", "Auth0 でログイン（デバイスフロー）");
        cmd.SetHandler(async () =>
        {
            AnsiConsole.MarkupLine("[bold]おこづかい帳[/] にログインします...");
            // TODO: MSAL Device Flow でトークン取得
            // 1. POST /oauth/device/code
            // 2. ユーザーにURL + codeを表示
            // 3. ポーリングでトークン取得
            // 4. TokenPath に保存
            AnsiConsole.MarkupLine("[yellow]実装予定: Device Flow 認証[/]");
            AnsiConsole.MarkupLine($"トークン保存先: [dim]{TokenPath}[/]");
        });
        return cmd;
    }

    private static Command CreateLogoutCommand()
    {
        var cmd = new Command("logout", "ログアウト（キャッシュトークン削除）");
        cmd.SetHandler(() =>
        {
            if (File.Exists(TokenPath))
            {
                File.Delete(TokenPath);
                AnsiConsole.MarkupLine("[green]ログアウトしました。[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]ログイン済みのセッションはありません。[/]");
            }
        });
        return cmd;
    }

    private static Command CreateStatusCommand()
    {
        var cmd = new Command("status", "現在の認証状態を表示");
        cmd.SetHandler(() =>
        {
            if (File.Exists(TokenPath))
            {
                var json = File.ReadAllText(TokenPath);
                // TODO: トークンの有効期限チェック
                AnsiConsole.MarkupLine("[green]ログイン済み[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]未ログイン[/] - `okozukai auth login` でログインしてください");
            }
        });
        return cmd;
    }
}
