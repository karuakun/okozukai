namespace AgentSkillsSample.Models;

/// <summary>家計の支出エントリー</summary>
public record BudgetEntry(
    DateTime Date,
    string Description,
    decimal Amount,
    string Category
);

/// <summary>家計カテゴリー</summary>
public static class BudgetCategory
{
    public const string Food = "食費";
    public const string Transport = "交通費";
    public const string Utilities = "光熱費";
    public const string Entertainment = "娯楽費";
    public const string Healthcare = "医療費";
    public const string Other = "その他";

    public static readonly IReadOnlyList<string> All =
    [
        Food, Transport, Utilities, Entertainment, Healthcare, Other
    ];
}
