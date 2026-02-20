using Okozukai.Core.DTOs;

namespace Okozukai.Web.Services;

/// <summary>
/// トランザクション一覧の状態管理（Blazor コンポーネント間で共有）。
/// 変更通知パターンで UI を自動更新する。
/// </summary>
public class TransactionStateService(OkozukaiApiClient api)
{
    public IReadOnlyList<TransactionResponse> Transactions { get; private set; } = [];
    public MonthSummary? Summary { get; private set; }
    public bool IsLoading { get; private set; }

    public event Action? OnChange;

    public async Task LoadAsync(int year, int month)
    {
        IsLoading = true;
        NotifyStateChanged();

        var from = new DateOnly(year, month, 1);
        var to   = from.AddMonths(1).AddDays(-1);

        var result  = await api.GetTransactionsAsync(from, to);
        var summary = await api.GetMonthlySummaryAsync(year, month);

        Transactions = result?.Items ?? [];
        Summary      = summary;
        IsLoading    = false;
        NotifyStateChanged();
    }

    public async Task DeleteAsync(int id)
    {
        await api.DeleteTransactionAsync(id);
        Transactions = Transactions.Where(t => t.Id != id).ToList();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
