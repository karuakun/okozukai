using System.ComponentModel;
using System.Text.Json;
using AgentSkillsSample.Models;
using Microsoft.SemanticKernel;

namespace AgentSkillsSample.Plugins;

/// <summary>
/// 予算計算スキル (Budget Calculation Skill)
/// 家計の支出計算・集計を行うKernelPlugin
/// </summary>
public sealed class BudgetCalculationPlugin
{
    private readonly List<BudgetEntry> _entries;

    public BudgetCalculationPlugin(IEnumerable<BudgetEntry>? entries = null)
    {
        _entries = entries?.ToList() ?? GenerateSampleData();
    }

    [KernelFunction("calculate_total")]
    [Description("指定した期間の支出合計を計算します。月(1-12)を指定してください。")]
    public decimal CalculateTotal(
        [Description("対象年 (例: 2024)")] int year,
        [Description("対象月 (1-12)")] int month)
    {
        return _entries
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .Sum(e => e.Amount);
    }

    [KernelFunction("calculate_average")]
    [Description("月別の平均支出額を計算します。")]
    public decimal CalculateMonthlyAverage()
    {
        if (_entries.Count == 0) return 0;

        return _entries
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => g.Sum(e => e.Amount))
            .Average();
    }

    [KernelFunction("get_top_expenses")]
    [Description("支出額の大きい上位N件を取得します。")]
    public string GetTopExpenses(
        [Description("取得する件数 (例: 5)")] int count = 5)
    {
        var top = _entries
            .OrderByDescending(e => e.Amount)
            .Take(count)
            .Select(e => new
            {
                e.Date,
                e.Description,
                Amount = e.Amount.ToString("C0"),
                e.Category
            });

        return JsonSerializer.Serialize(top, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("compare_months")]
    [Description("2つの月の支出を比較します。前月比の増減をパーセントで返します。")]
    public string CompareMonths(
        [Description("比較元の年月 (例: 2024-11)")] string baseYearMonth,
        [Description("比較先の年月 (例: 2024-12)")] string targetYearMonth)
    {
        var baseParts = baseYearMonth.Split('-');
        var targetParts = targetYearMonth.Split('-');

        if (baseParts.Length != 2 || targetParts.Length != 2)
            return "年月の形式が正しくありません。YYYY-MM形式で指定してください。";

        int.TryParse(baseParts[0], out var baseYear);
        int.TryParse(baseParts[1], out var baseMonth);
        int.TryParse(targetParts[0], out var targetYear);
        int.TryParse(targetParts[1], out var targetMonth);

        var baseTotal = CalculateTotal(baseYear, baseMonth);
        var targetTotal = CalculateTotal(targetYear, targetMonth);

        var diff = targetTotal - baseTotal;
        var pct = baseTotal == 0 ? 0 : (diff / baseTotal) * 100;

        return $"{baseYearMonth}: {baseTotal:C0}, {targetYearMonth}: {targetTotal:C0}, " +
               $"差額: {diff:+#,0;-#,0;0}円 ({pct:+0.0;-0.0;0.0}%)";
    }

    private static List<BudgetEntry> GenerateSampleData()
    {
        var rnd = new Random(42);
        var entries = new List<BudgetEntry>();
        var categories = BudgetCategory.All;
        var descriptions = new Dictionary<string, string[]>
        {
            [BudgetCategory.Food]          = ["スーパー", "コンビニ", "外食", "飲み会"],
            [BudgetCategory.Transport]     = ["電車定期", "バス", "タクシー", "ガソリン"],
            [BudgetCategory.Utilities]     = ["電気代", "水道代", "ガス代", "インターネット"],
            [BudgetCategory.Entertainment] = ["映画", "書籍", "ゲーム", "旅行"],
            [BudgetCategory.Healthcare]    = ["病院", "薬局", "健康診断"],
            [BudgetCategory.Other]         = ["日用品", "衣類", "雑費"],
        };

        foreach (var month in Enumerable.Range(10, 3)) // 10, 11, 12月
        {
            var daysInMonth = DateTime.DaysInMonth(2024, month);
            var entryCount = rnd.Next(15, 30);

            for (var i = 0; i < entryCount; i++)
            {
                var category = categories[rnd.Next(categories.Count)];
                var descList = descriptions[category];
                entries.Add(new BudgetEntry(
                    Date: new DateTime(2024, month, rnd.Next(1, daysInMonth + 1)),
                    Description: descList[rnd.Next(descList.Length)],
                    Amount: rnd.Next(500, 30000),
                    Category: category
                ));
            }
        }

        return entries;
    }
}
