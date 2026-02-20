using Microsoft.EntityFrameworkCore;
using Okozukai.Api.Data;
using Okozukai.Core.DTOs;
using Okozukai.Core.Interfaces;
using Okozukai.Core.Models;

namespace Okozukai.Api.Services;

public class TransactionService(UserDbContextFactory dbFactory)
{
    public async Task<TransactionListResponse> GetAllAsync(
        string userId, DateOnly? from, DateOnly? to, int? categoryId, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var query = db.Transactions.Include(t => t.Category).AsQueryable();

        if (from.HasValue)       query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue)         query = query.Where(t => t.Date <= to.Value);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync(ct);

        return new TransactionListResponse(
            items.Select(TransactionResponse.FromModel).ToList(),
            items.Count
        );
    }

    public async Task<TransactionResponse?> GetByIdAsync(string userId, int id, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var t = await db.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id, ct);
        return t is null ? null : TransactionResponse.FromModel(t);
    }

    public async Task<TransactionResponse> CreateAsync(
        string userId, CreateTransactionRequest req, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var transaction = new Transaction
        {
            Amount      = req.Amount,
            Description = req.Description,
            CategoryId  = req.CategoryId,
            Date        = req.Date,
            Note        = req.Note,
        };
        db.Transactions.Add(transaction);
        await dbFactory.CommitAsync(ct);

        await db.Entry(transaction).Reference(t => t.Category).LoadAsync(ct);
        return TransactionResponse.FromModel(transaction);
    }

    public async Task<TransactionResponse?> UpdateAsync(
        string userId, int id, UpdateTransactionRequest req, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var transaction = await db.Transactions.FindAsync([id], ct);
        if (transaction is null) return null;

        transaction.Amount      = req.Amount;
        transaction.Description = req.Description;
        transaction.CategoryId  = req.CategoryId;
        transaction.Date        = req.Date;
        transaction.Note        = req.Note;
        transaction.UpdatedAt   = DateTime.UtcNow;

        await dbFactory.CommitAsync(ct);

        await db.Entry(transaction).Reference(t => t.Category).LoadAsync(ct);
        return TransactionResponse.FromModel(transaction);
    }

    public async Task<bool> DeleteAsync(string userId, int id, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var transaction = await db.Transactions.FindAsync([id], ct);
        if (transaction is null) return false;

        db.Transactions.Remove(transaction);
        await dbFactory.CommitAsync(ct);
        return true;
    }

    public async Task<MonthSummary> GetMonthlySummaryAsync(
        string userId, int year, int month, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var from = new DateOnly(year, month, 1);
        var to   = from.AddMonths(1).AddDays(-1);

        var transactions = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to)
            .ToListAsync(ct);

        var income  = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var expense = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);

        return new MonthSummary(year, month, income, expense, income + expense);
    }
}
