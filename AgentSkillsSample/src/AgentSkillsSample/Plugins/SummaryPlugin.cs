using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;

namespace AgentSkillsSample.Plugins;

/// <summary>
/// サマリー生成スキル (Summary Generation Skill)
/// 家計データの要点をまとめるKernelPlugin
/// </summary>
public sealed class SummaryPlugin
{
    [KernelFunction("create_executive_summary")]
    [Description("経営者向け要約（エグゼクティブサマリー）スタイルで家計の概要を生成します。")]
    public string CreateExecutiveSummary(
        [Description("当月合計額 (円)")] decimal currentMonthTotal,
        [Description("前月合計額 (円)")] decimal previousMonthTotal,
        [Description("月平均額 (円)")] decimal monthlyAverage,
        [Description("最大支出カテゴリー")] string topCategory)
    {
        var diff = currentMonthTotal - previousMonthTotal;
        var diffSign = diff >= 0 ? "+" : "";
        var vsAvg = currentMonthTotal - monthlyAverage;
        var vsAvgSign = vsAvg >= 0 ? "+" : "";
        var health = currentMonthTotal <= monthlyAverage * 0.95m ? "良好 🟢"
                   : currentMonthTotal <= monthlyAverage * 1.05m ? "普通 🟡"
                   : "要注意 🔴";

        var sb = new StringBuilder();
        sb.AppendLine("【エグゼクティブサマリー】");
        sb.AppendLine($"  財務状況    : {health}");
        sb.AppendLine($"  当月支出    : {currentMonthTotal:N0}円");
        sb.AppendLine($"  前月比      : {diffSign}{diff:N0}円");
        sb.AppendLine($"  月平均比    : {vsAvgSign}{vsAvg:N0}円");
        sb.AppendLine($"  主要支出先  : {topCategory}");
        return sb.ToString();
    }

    [KernelFunction("create_bullet_summary")]
    [Description("箇条書き形式で重要ポイントをまとめます。複数の事実を受け取り、ポイントとして整理します。")]
    public string CreateBulletSummary(
        [Description("サマリーに含めるポイント (改行区切り)")] string points)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【主要ポイント】");

        var items = points.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in items)
        {
            var text = item.Trim();
            if (!string.IsNullOrEmpty(text))
                sb.AppendLine($"  • {text}");
        }

        return sb.ToString();
    }

    [KernelFunction("generate_next_month_goal")]
    [Description("翌月の節約目標を生成します。現在の支出パターンに基づいて現実的な目標を提案します。")]
    public string GenerateNextMonthGoal(
        [Description("当月の合計支出額 (円)")] decimal currentTotal,
        [Description("目標削減率 (0.0〜1.0, 例: 0.05で5%削減)")] double reductionRate = 0.05)
    {
        var target = currentTotal * (1 - (decimal)reductionRate);
        var savingsGoal = currentTotal - target;

        var sb = new StringBuilder();
        sb.AppendLine("【翌月の目標】");
        sb.AppendLine($"  🎯 目標支出額   : {target:N0}円");
        sb.AppendLine($"  💰 削減目標額   : {savingsGoal:N0}円 ({reductionRate * 100:0.0}%削減)");
        sb.AppendLine($"  📅 1日あたり目標: {target / 30:N0}円");
        return sb.ToString();
    }

    [KernelFunction("create_comparison_summary")]
    [Description("複数月のデータを比較した総括コメントを生成します。")]
    public string CreateComparisonSummary(
        [Description("比較する期間の説明 (例: 2024年10月〜12月)")] string period,
        [Description("最大支出月")] string highestMonth,
        [Description("最大支出額 (円)")] decimal highestAmount,
        [Description("最小支出月")] string lowestMonth,
        [Description("最小支出額 (円)")] decimal lowestAmount)
    {
        var variance = highestAmount - lowestAmount;
        var varianceRatio = lowestAmount > 0 ? variance / lowestAmount * 100 : 0;

        var sb = new StringBuilder();
        sb.AppendLine($"【{period} 比較サマリー】");
        sb.AppendLine($"  📈 最大支出月: {highestMonth} ({highestAmount:N0}円)");
        sb.AppendLine($"  📉 最小支出月: {lowestMonth} ({lowestAmount:N0}円)");
        sb.AppendLine($"  📊 振れ幅    : {variance:N0}円 ({varianceRatio:0.0}%)");

        if (varianceRatio > 20)
            sb.AppendLine("  ⚠️  月による支出変動が大きいです。一定のペースを目指しましょう。");
        else
            sb.AppendLine("  ✅ 支出が安定しています。良い傾向です。");

        return sb.ToString();
    }
}
