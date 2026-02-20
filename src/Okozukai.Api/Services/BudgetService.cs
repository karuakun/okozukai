using Microsoft.EntityFrameworkCore;
using Okozukai.Api.Data;
using Okozukai.Core.DTOs;
using Okozukai.Core.Models;

namespace Okozukai.Api.Services;

public class BudgetService(UserDbContextFactory dbFactory)
{
    public async Task<IReadOnlyList<BudgetProgressResponse>> GetProgressAsync(
        string userId, int year, int month, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var from = new DateOnly(year, month, 1);
        var to   = from.AddMonths(1).AddDays(-1);

        var budgets = await db.Budgets
            .Include(b => b.Category)
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync(ct);

        // 対象月の支出集計
        var actuals = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to && t.Amount < 0)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var actualMap = actuals.ToDictionary(a => a.CategoryId, a => Math.Abs(a.Total));

        return budgets.Select(b =>
        {
            var actual = actualMap.GetValueOrDefault(b.CategoryId, 0);
            return new BudgetProgressResponse(
                b.CategoryId,
                b.Category?.Name ?? string.Empty,
                b.Category?.Icon ?? "💰",
                b.MonthlyLimit,
                actual,
                b.MonthlyLimit - actual,
                b.MonthlyLimit > 0 ? (double)(actual / b.MonthlyLimit) * 100 : 0
            );
        }).ToList();
    }

    public async Task<BudgetResponse> UpsertAsync(
        string userId, UpsertBudgetRequest req, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var existing = await db.Budgets
            .FirstOrDefaultAsync(b => b.CategoryId == req.CategoryId
                                   && b.Year == req.Year
                                   && b.Month == req.Month, ct);

        if (existing is null)
        {
            existing = new Budget
            {
                CategoryId   = req.CategoryId,
                MonthlyLimit = req.MonthlyLimit,
                Year         = req.Year,
                Month        = req.Month,
            };
            db.Budgets.Add(existing);
        }
        else
        {
            existing.MonthlyLimit = req.MonthlyLimit;
        }

        await dbFactory.CommitAsync(ct);
        await db.Entry(existing).Reference(b => b.Category).LoadAsync(ct);
        return BudgetResponse.FromModel(existing);
    }

    public async Task<bool> DeleteAsync(string userId, int id, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var budget = await db.Budgets.FindAsync([id], ct);
        if (budget is null) return false;

        db.Budgets.Remove(budget);
        await dbFactory.CommitAsync(ct);
        return true;
    }
}
