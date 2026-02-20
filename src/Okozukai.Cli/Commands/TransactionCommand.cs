using System.CommandLine;
using System.Net.Http.Json;
using Okozukai.Core.DTOs;
using Spectre.Console;

namespace Okozukai.Cli.Commands;

/// <summary>
/// 収支の追加・一覧・削除コマンド。
/// 使用例:
///   okozukai tx list --month 2025-01
///   okozukai tx add --amount -500 --desc "コーヒー" --category 1 --date 2025-01-15
///   okozukai tx delete 42
/// </summary>
public static class TransactionCommand
{
    public static Command Create()
    {
        var cmd = new Command("tx", "収支を管理 (transactionのエイリアス)");
        cmd.AddAlias("transaction");
        cmd.AddCommand(CreateListCommand());
        cmd.AddCommand(CreateAddCommand());
        cmd.AddCommand(CreateDeleteCommand());
        return cmd;
    }

    private static Command CreateListCommand()
    {
        var monthOpt = new Option<string?>("--month", "対象年月 (例: 2025-01)");
        var cmd = new Command("list", "収支一覧を表示") { monthOpt };

        cmd.SetHandler(async (month) =>
        {
            var (year, m) = ParseMonth(month);
            var client = ApiClientFactory.Create();
            var result = await client.GetFromJsonAsync<TransactionListResponse>(
                $"/api/transactions?from={year}-{m:D2}-01&to={year}-{m:D2}-{DateTime.DaysInMonth(year, m):D2}");

            if (result is null || result.Items.Count == 0)
            {
                AnsiConsole.MarkupLine("[dim]データがありません。[/]");
                return;
            }

            var table = new Table()
                .AddColumn("ID")
                .AddColumn("日付")
                .AddColumn("カテゴリ")
                .AddColumn("説明")
                .AddColumn(new TableColumn("金額").RightAligned());

            foreach (var t in result.Items)
            {
                var amountColor = t.Amount >= 0 ? "green" : "red";
                table.AddRow(
                    t.Id.ToString(),
                    t.Date.ToString("MM/dd"),
                    $"{t.CategoryIcon} {t.CategoryName}",
                    t.Description,
                    $"[{amountColor}]{t.Amount:C0}[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"合計 [bold]{result.TotalCount}[/] 件");
        }, monthOpt);

        return cmd;
    }

    private static Command CreateAddCommand()
    {
        var amountOpt   = new Option<decimal>("--amount",   "金額 (正=収入, 負=支出)") { IsRequired = true };
        var descOpt     = new Option<string>("--desc",      "説明") { IsRequired = true };
        var categoryOpt = new Option<int>("--category",     "カテゴリID") { IsRequired = true };
        var dateOpt     = new Option<DateOnly?>("--date",   "日付 (省略時: 今日)");
        var noteOpt     = new Option<string?>("--note",     "メモ");

        var cmd = new Command("add", "収支を追加")
        {
            amountOpt, descOpt, categoryOpt, dateOpt, noteOpt
        };

        cmd.SetHandler(async (amount, desc, categoryId, date, note) =>
        {
            var req = new CreateTransactionRequest(
                amount,
                desc,
                categoryId,
                date ?? DateOnly.FromDateTime(DateTime.Today),
                note
            );

            var client = ApiClientFactory.Create();
            var res = await client.PostAsJsonAsync("/api/transactions", req);
            res.EnsureSuccessStatusCode();

            var created = await res.Content.ReadFromJsonAsync<TransactionResponse>();
            AnsiConsole.MarkupLine(
                $"[green]登録しました[/]: ID={created!.Id} {created.Date:MM/dd} {created.Description} {created.Amount:C0}");
        }, amountOpt, descOpt, categoryOpt, dateOpt, noteOpt);

        return cmd;
    }

    private static Command CreateDeleteCommand()
    {
        var idArg = new Argument<int>("id", "削除する収支のID");
        var cmd = new Command("delete", "収支を削除") { idArg };

        cmd.SetHandler(async (id) =>
        {
            var client = ApiClientFactory.Create();
            var res = await client.DeleteAsync($"/api/transactions/{id}");
            if (res.IsSuccessStatusCode)
                AnsiConsole.MarkupLine($"[green]ID={id} を削除しました。[/]");
            else
                AnsiConsole.MarkupLine($"[red]削除失敗: ID={id} が見つかりません。[/]");
        }, idArg);

        return cmd;
    }

    private static (int Year, int Month) ParseMonth(string? month)
    {
        if (month is null) return (DateTime.Now.Year, DateTime.Now.Month);
        var parts = month.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
