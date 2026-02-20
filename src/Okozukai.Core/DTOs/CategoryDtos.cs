using Okozukai.Core.Models;

namespace Okozukai.Core.DTOs;

public record CreateCategoryRequest(
    string Name,
    string Icon,
    CategoryType Type,
    int SortOrder = 0
);

public record UpdateCategoryRequest(
    string Name,
    string Icon,
    CategoryType Type,
    int SortOrder
);

public record CategoryResponse(
    int Id,
    string Name,
    string Icon,
    CategoryType Type,
    int SortOrder
)
{
    public static CategoryResponse FromModel(Category c) => new(
        c.Id, c.Name, c.Icon, c.Type, c.SortOrder
    );
}
