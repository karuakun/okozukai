using System.ComponentModel;
using System.Text.Json;
using AgentSkillsSample.Models;
using Microsoft.SemanticKernel;

namespace AgentSkillsSample.Plugins;

/// <summary>
/// カテゴリー分析スキル (Category Analysis Skill)
/// 支出のカテゴリー別集計・分析を行うKernelPlugin
/// </summary>
public sealed class CategoryPlugin
{
    private readonly List<BudgetEntry> _entries;

    public CategoryPlugin(IEnumerable<BudgetEntry>? entries = null)
    {
        _entries = entries?.ToList() ?? CreateSharedSampleData();
    }

    [KernelFunction("get_category_breakdown")]
    [Description("指定した月のカテゴリー別支出内訳を取得します。")]
    public string GetCategoryBreakdown(
        [Description("対象年 (例: 2024)")] int year,
        [Description("対象月 (1-12)")] int month)
    {
        var monthEntries = _entries
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToList();

        if (monthEntries.Count == 0)
            return $"{year}年{month}月のデータがありません。";

        var total = monthEntries.Sum(e => e.Amount);
        var breakdown = monthEntries
            .GroupBy(e => e.Category)
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count(),
                Ratio = $"{g.Sum(e => e.Amount) / total * 100:0.0}%"
            })
            .OrderByDescending(x => x.Amount);

        return JsonSerializer.Serialize(breakdown, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("get_category_trend")]
    [Description("指定したカテゴリーの月別推移を取得します。")]
    public string GetCategoryTrend(
        [Description("カテゴリー名 (食費, 交通費, 光熱費, 娯楽費, 医療費, その他)")] string category)
    {
        var trend = _entries
            .Where(e => e.Category == category)
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new
            {
                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Period);

        return JsonSerializer.Serialize(trend, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("find_highest_category")]
    [Description("最も支出が多いカテゴリーを特定します。")]
    public string FindHighestCategory(
        [Description("対象年 (例: 2024)")] int year,
        [Description("対象月 (1-12)")] int month)
    {
        var monthEntries = _entries
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .ToList();

        if (monthEntries.Count == 0)
            return $"{year}年{month}月のデータがありません。";

        var highest = monthEntries
            .GroupBy(e => e.Category)
            .OrderByDescending(g => g.Sum(e => e.Amount))
            .First();

        return $"最大支出カテゴリー: {highest.Key} " +
               $"(合計: {highest.Sum(e => e.Amount):C0}, 件数: {highest.Count()}件)";
    }

    [KernelFunction("get_all_categories_summary")]
    [Description("全期間のカテゴリー別サマリーを取得します。")]
    public string GetAllCategoriesSummary()
    {
        var summary = _entries
            .GroupBy(e => e.Category)
            .Select(g => new
            {
                Category = g.Key,
                TotalAmount = g.Sum(e => e.Amount),
                MonthlyAvg = g.GroupBy(e => new { e.Date.Year, e.Date.Month })
                              .Average(m => m.Sum(e => e.Amount)),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount);

        return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
    }

    private static List<BudgetEntry> CreateSharedSampleData()
    {
        // BudgetCalculationPluginと同じサンプルデータを生成
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

        foreach (var month in Enumerable.Range(10, 3))
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
