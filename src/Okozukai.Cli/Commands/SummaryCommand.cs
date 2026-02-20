using System.CommandLine;
using System.Net.Http.Json;
using Okozukai.Core.DTOs;
using Okozukai.Core.Interfaces;
using Spectre.Console;

namespace Okozukai.Cli.Commands;

/// <summary>
/// 月次サマリを表示するコマンド。
/// 使用例:
///   okozukai summary
///   okozukai summary --month 2025-01
/// </summary>
public static class SummaryCommand
{
    public static Command Create()
    {
        var monthOpt = new Option<string?>("--month", "対象年月 (例: 2025-01)");
        var cmd = new Command("summary", "月次サマリを表示") { monthOpt };

        cmd.SetHandler(async (month) =>
        {
            var (year, m) = ParseMonth(month);
            var client = ApiClientFactory.Create();

            var summary = await client.GetFromJsonAsync<MonthSummary>(
                $"/api/transactions/summary?year={year}&month={m}");
            var budgets = await client.GetFromJsonAsync<IReadOnlyList<BudgetProgressResponse>>(
                $"/api/budgets/progress?year={year}&month={m}");

            if (summary is null) return;

            // ヘッダー
            var rule = new Rule($"[bold]{year}年{m}月 サマリ[/]");
            AnsiConsole.Write(rule);

            // 収支サマリ
            AnsiConsole.MarkupLine($"  収入: [green]{summary.TotalIncome:C0}[/]");
            AnsiConsole.MarkupLine($"  支出: [red]{Math.Abs(summary.TotalExpense):C0}[/]");
            var balanceColor = summary.Balance >= 0 ? "blue" : "red";
            AnsiConsole.MarkupLine($"  残高: [{balanceColor}]{summary.Balance:C0}[/]");

            // 予算進捗
            if (budgets is { Count: > 0 })
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]予算進捗[/]");

                var table = new Table()
                    .AddColumn("カテゴリ")
                    .AddColumn(new TableColumn("予算").RightAligned())
                    .AddColumn(new TableColumn("使用額").RightAligned())
                    .AddColumn("進捗");

                foreach (var b in budgets)
                {
                    var pct = (int)Math.Min(b.UsageRate, 100);
                    var barColor = pct >= 100 ? "red" : pct >= 80 ? "yellow" : "green";
                    var bar = $"[{barColor}]{new string('█', pct / 10)}{new string('░', 10 - pct / 10)}[/] {b.UsageRate:F0}%";

                    table.AddRow(
                        $"{b.CategoryIcon} {b.CategoryName}",
                        $"{b.MonthlyLimit:C0}",
                        $"{b.ActualAmount:C0}",
                        bar
                    );
                }

                AnsiConsole.Write(table);
            }
        }, monthOpt);

        return cmd;
    }

    private static (int Year, int Month) ParseMonth(string? month)
    {
        if (month is null) return (DateTime.Now.Year, DateTime.Now.Month);
        var parts = month.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
