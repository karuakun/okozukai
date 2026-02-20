using Okozukai.Core.Models;

namespace Okozukai.Core.DTOs;

public record UpsertBudgetRequest(
    int CategoryId,
    decimal MonthlyLimit,
    int Year,
    int Month
);

public record BudgetResponse(
    int Id,
    int CategoryId,
    string CategoryName,
    decimal MonthlyLimit,
    int Year,
    int Month
)
{
    public static BudgetResponse FromModel(Budget b) => new(
        b.Id,
        b.CategoryId,
        b.Category?.Name ?? string.Empty,
        b.MonthlyLimit,
        b.Year,
        b.Month
    );
}

/// <summary>予算と実績を合わせたサマリ（月次レポート用）</summary>
public record BudgetProgressResponse(
    int CategoryId,
    string CategoryName,
    string CategoryIcon,
    decimal MonthlyLimit,
    decimal ActualAmount,
    decimal RemainingAmount,
    double UsageRate
);
