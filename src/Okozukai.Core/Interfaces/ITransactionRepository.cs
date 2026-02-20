using Okozukai.Core.Models;

namespace Okozukai.Core.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllAsync(DateOnly? from = null, DateOnly? to = null, int? categoryId = null, CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken ct = default);
    Task<Transaction> UpdateAsync(Transaction transaction, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>月次残高サマリを取得</summary>
    Task<MonthSummary> GetMonthlySummaryAsync(int year, int month, CancellationToken ct = default);
}

public record MonthSummary(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance
);
