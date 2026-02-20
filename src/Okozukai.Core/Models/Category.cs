namespace Okozukai.Core.Models;

/// <summary>収支カテゴリ</summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>アイコン識別子（絵文字 or アイコン名）</summary>
    public string Icon { get; set; } = "💰";

    public CategoryType Type { get; set; }
    public int SortOrder { get; set; }

    public List<Transaction> Transactions { get; set; } = [];
}

public enum CategoryType
{
    Income,   // 収入
    Expense,  // 支出
    Both      // 両方
}
