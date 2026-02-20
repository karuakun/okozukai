using System.Net.Http.Json;
using Okozukai.Core.DTOs;
using Okozukai.Core.Interfaces;

namespace Okozukai.Web.Services;

/// <summary>
/// WebAPI への型付きHTTPクライアント。
/// CLI プロジェクトも同様のクライアントを持ち、APIは共通。
/// </summary>
public class OkozukaiApiClient(HttpClient http)
{
    // ── Transactions ─────────────────────────────────────────────

    public Task<TransactionListResponse?> GetTransactionsAsync(
        DateOnly? from = null, DateOnly? to = null, int? categoryId = null)
    {
        var query = BuildQuery(
            ("from",       from?.ToString("yyyy-MM-dd")),
            ("to",         to?.ToString("yyyy-MM-dd")),
            ("categoryId", categoryId?.ToString())
        );
        return http.GetFromJsonAsync<TransactionListResponse>($"/api/transactions{query}");
    }

    public Task<MonthSummary?> GetMonthlySummaryAsync(int year, int month) =>
        http.GetFromJsonAsync<MonthSummary>($"/api/transactions/summary?year={year}&month={month}");

    public Task<TransactionResponse?> GetTransactionAsync(int id) =>
        http.GetFromJsonAsync<TransactionResponse>($"/api/transactions/{id}");

    public async Task<TransactionResponse?> CreateTransactionAsync(CreateTransactionRequest req)
    {
        var res = await http.PostAsJsonAsync("/api/transactions", req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TransactionResponse>();
    }

    public async Task<TransactionResponse?> UpdateTransactionAsync(int id, UpdateTransactionRequest req)
    {
        var res = await http.PutAsJsonAsync($"/api/transactions/{id}", req);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TransactionResponse>();
    }

    public async Task<bool> DeleteTransactionAsync(int id)
    {
        var res = await http.DeleteAsync($"/api/transactions/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Budgets ──────────────────────────────────────────────────

    public Task<IReadOnlyList<BudgetProgressResponse>?> GetBudgetProgressAsync(int year, int month) =>
        http.GetFromJsonAsync<IReadOnlyList<BudgetProgressResponse>>(
            $"/api/budgets/progress?year={year}&month={month}");

    public async Task<BudgetResponse?> UpsertBudgetAsync(UpsertBudgetRequest req)
    {
        var res = await http.PutAsJsonAsync("/api/budgets", req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<BudgetResponse>();
    }

    // ── Categories ───────────────────────────────────────────────

    public Task<IReadOnlyList<CategoryResponse>?> GetCategoriesAsync() =>
        http.GetFromJsonAsync<IReadOnlyList<CategoryResponse>>("/api/categories");

    // ── Helpers ──────────────────────────────────────────────────

    private static string BuildQuery(params (string Key, string? Value)[] pairs)
    {
        var parts = pairs
            .Where(p => p.Value is not null)
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? $"?{qs}" : string.Empty;
    }
}
