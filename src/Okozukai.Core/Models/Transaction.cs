namespace Okozukai.Core.Models;

/// <summary>収支トランザクション（収入・支出の記録）</summary>
public class Transaction
{
    public int Id { get; set; }

    /// <summary>金額（正 = 収入、負 = 支出）</summary>
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>メモ・備考</summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsIncome => Amount > 0;
    public bool IsExpense => Amount < 0;
}
