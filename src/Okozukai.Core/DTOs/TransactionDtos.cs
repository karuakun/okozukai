using Okozukai.Core.Models;

namespace Okozukai.Core.DTOs;

public record CreateTransactionRequest(
    decimal Amount,
    string Description,
    int CategoryId,
    DateOnly Date,
    string? Note = null
);

public record UpdateTransactionRequest(
    decimal Amount,
    string Description,
    int CategoryId,
    DateOnly Date,
    string? Note = null
);

public record TransactionResponse(
    int Id,
    decimal Amount,
    string Description,
    int CategoryId,
    string CategoryName,
    string CategoryIcon,
    DateOnly Date,
    string? Note,
    DateTime CreatedAt
)
{
    public static TransactionResponse FromModel(Transaction t) => new(
        t.Id,
        t.Amount,
        t.Description,
        t.CategoryId,
        t.Category?.Name ?? string.Empty,
        t.Category?.Icon ?? "💰",
        t.Date,
        t.Note,
        t.CreatedAt
    );
}

public record TransactionListResponse(
    IReadOnlyList<TransactionResponse> Items,
    int TotalCount
);
