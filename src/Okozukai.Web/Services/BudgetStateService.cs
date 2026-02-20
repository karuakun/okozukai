using Okozukai.Core.DTOs;

namespace Okozukai.Web.Services;

public class BudgetStateService(OkozukaiApiClient api)
{
    public IReadOnlyList<BudgetProgressResponse> Progress { get; private set; } = [];
    public bool IsLoading { get; private set; }

    public event Action? OnChange;

    public async Task LoadAsync(int year, int month)
    {
        IsLoading = true;
        NotifyStateChanged();

        var result = await api.GetBudgetProgressAsync(year, month);
        Progress  = result ?? [];
        IsLoading = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
