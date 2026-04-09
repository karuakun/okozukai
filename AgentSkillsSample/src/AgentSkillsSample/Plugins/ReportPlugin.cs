using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;

namespace AgentSkillsSample.Plugins;

/// <summary>
/// レポート生成スキル (Report Generation Skill)
/// 分析結果を構造化されたレポートとして整形するKernelPlugin
/// </summary>
public sealed class ReportPlugin
{
    [KernelFunction("create_monthly_report")]
    [Description("月次家計レポートのヘッダーとフォーマットを生成します。分析データを受け取り、整形します。")]
    public string CreateMonthlyReport(
        [Description("レポート対象年月 (例: 2024年12月)")] string yearMonth,
        [Description("合計支出額 (数値)")] decimal totalAmount,
        [Description("最大支出カテゴリー名")] string topCategory,
        [Description("前月比 (例: +5.2% または -3.1%)")] string monthOverMonthChange)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine($"  📊 家計レポート - {yearMonth}");
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("【概要】");
        sb.AppendLine($"  ✅ 当月合計支出 : {totalAmount:N0}円");
        sb.AppendLine($"  📈 前月比       : {monthOverMonthChange}");
        sb.AppendLine($"  🏆 最大カテゴリー: {topCategory}");
        sb.AppendLine();

        return sb.ToString();
    }

    [KernelFunction("format_category_table")]
    [Description("カテゴリー別支出をテーブル形式に整形します。JSON形式の集計データを受け取ります。")]
    public string FormatCategoryTable(
        [Description("カテゴリー名と金額のCSVデータ (カテゴリー,金額,割合 の形式)")] string categoryCsvData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【カテゴリー別内訳】");
        sb.AppendLine("┌─────────────┬──────────────┬────────┐");
        sb.AppendLine("│ カテゴリー   │    金額      │  割合  │");
        sb.AppendLine("├─────────────┼──────────────┼────────┤");

        var lines = categoryCsvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length >= 3)
            {
                var category = parts[0].Trim().PadRight(10);
                var amount   = parts[1].Trim().PadLeft(10);
                var ratio    = parts[2].Trim().PadLeft(5);
                sb.AppendLine($"│ {category}│ {amount}円 │ {ratio} │");
            }
        }

        sb.AppendLine("└─────────────┴──────────────┴────────┘");
        return sb.ToString();
    }

    [KernelFunction("create_trend_chart")]
    [Description("月別推移をASCIIチャート形式で生成します。月と金額のペアを受け取ります。")]
    public string CreateTrendChart(
        [Description("月別データCSV (月,金額 の形式, 例: '10月,45000\\n11月,52000\\n12月,48000')")] string monthlyDataCsv)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【月別推移】");

        var lines = monthlyDataCsv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var maxAmount = 0m;

        var data = new List<(string month, decimal amount)>();
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length >= 2 && decimal.TryParse(parts[1].Trim(), out var amt))
            {
                data.Add((parts[0].Trim(), amt));
                if (amt > maxAmount) maxAmount = amt;
            }
        }

        const int barWidth = 30;
        foreach (var (month, amount) in data)
        {
            var barLen = maxAmount > 0 ? (int)(amount / maxAmount * barWidth) : 0;
            var bar = new string('█', barLen);
            sb.AppendLine($"  {month,5}: {bar,-30} {amount:N0}円");
        }

        return sb.ToString();
    }

    [KernelFunction("create_advice_section")]
    [Description("支出パターンに基づく節約アドバイスセクションを生成します。")]
    public string CreateAdviceSection(
        [Description("最も支出が多いカテゴリー")] string highestCategory,
        [Description("前月からの増加額 (円)")] decimal increaseAmount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【節約アドバイス】");

        var adviceMap = new Dictionary<string, string>
        {
            ["食費"]   = "  💡 食費削減のヒント: まとめ買いや自炊を増やすと効果的です",
            ["交通費"] = "  💡 交通費削減のヒント: 定期券の範囲内での移動を意識しましょう",
            ["光熱費"] = "  💡 光熱費削減のヒント: 省エネ機器の活用や、節電を心がけましょう",
            ["娯楽費"] = "  💡 娯楽費削減のヒント: 月のエンタメ予算を決めて管理しましょう",
            ["医療費"] = "  💡 医療費: 予防医療への投資は長期的に節約になります",
            ["その他"] = "  💡 その他の支出: 明細を細かく記録して把握しましょう",
        };

        if (adviceMap.TryGetValue(highestCategory, out var advice))
            sb.AppendLine(advice);

        if (increaseAmount > 5000)
            sb.AppendLine($"  ⚠️  先月比 {increaseAmount:N0}円の増加。支出を見直しましょう。");
        else if (increaseAmount < -5000)
            sb.AppendLine($"  🎉 先月比 {Math.Abs(increaseAmount):N0}円の削減。節約効果が出ています！");
        else
            sb.AppendLine("  ✅ 支出は安定しています。この調子を維持しましょう。");

        return sb.ToString();
    }
}
