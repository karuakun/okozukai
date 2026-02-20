namespace Okozukai.Core.Models;

/// <summary>予算設定</summary>
public class Budget
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>予算額（月単位）</summary>
    public decimal MonthlyLimit { get; set; }

    /// <summary>対象年月（例: 2025-01）</summary>
    public int Year { get; set; }
    public int Month { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
