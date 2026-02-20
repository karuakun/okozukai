using System.CommandLine;
using System.Net.Http.Json;
using Okozukai.Core.DTOs;
using Spectre.Console;

namespace Okozukai.Cli.Commands;

/// <summary>
/// 予算設定コマンド。
/// 使用例:
///   okozukai budget set --category 1 --limit 30000 --month 2025-01
/// </summary>
public static class BudgetCommand
{
    public static Command Create()
    {
        var cmd = new Command("budget", "予算を管理");
        cmd.AddCommand(CreateSetCommand());
        return cmd;
    }

    private static Command CreateSetCommand()
    {
        var categoryOpt = new Option<int>("--category", "カテゴリID") { IsRequired = true };
        var limitOpt    = new Option<decimal>("--limit", "月次予算額") { IsRequired = true };
        var monthOpt    = new Option<string?>("--month", "対象年月 (例: 2025-01)");

        var cmd = new Command("set", "予算を設定または更新")
        {
            categoryOpt, limitOpt, monthOpt
        };

        cmd.SetHandler(async (categoryId, limit, month) =>
        {
            var (year, m) = ParseMonth(month);
            var req = new UpsertBudgetRequest(categoryId, limit, year, m);

            var client = ApiClientFactory.Create();
            var res = await client.PutAsJsonAsync("/api/budgets", req);
            res.EnsureSuccessStatusCode();

            AnsiConsole.MarkupLine(
                $"[green]予算を設定しました[/]: カテゴリID={categoryId} {year}年{m}月 {limit:C0}/月");
        }, categoryOpt, limitOpt, monthOpt);

        return cmd;
    }

    private static (int Year, int Month) ParseMonth(string? month)
    {
        if (month is null) return (DateTime.Now.Year, DateTime.Now.Month);
        var parts = month.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
