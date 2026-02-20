using Okozukai.Core.Models;

namespace Okozukai.Core.Interfaces;

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> GetByMonthAsync(int year, int month, CancellationToken ct = default);
    Task<Budget?> GetByCategoryAndMonthAsync(int categoryId, int year, int month, CancellationToken ct = default);
    Task<Budget> UpsertAsync(Budget budget, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
