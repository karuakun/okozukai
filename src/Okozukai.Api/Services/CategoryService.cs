using Microsoft.EntityFrameworkCore;
using Okozukai.Api.Data;
using Okozukai.Core.DTOs;
using Okozukai.Core.Models;

namespace Okozukai.Api.Services;

public class CategoryService(UserDbContextFactory dbFactory)
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(string userId, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var categories = await db.Categories.OrderBy(c => c.SortOrder).ToListAsync(ct);
        return categories.Select(CategoryResponse.FromModel).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(
        string userId, CreateCategoryRequest req, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var category = new Category
        {
            Name      = req.Name,
            Icon      = req.Icon,
            Type      = req.Type,
            SortOrder = req.SortOrder,
        };
        db.Categories.Add(category);
        await dbFactory.CommitAsync(ct);
        return CategoryResponse.FromModel(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(
        string userId, int id, UpdateCategoryRequest req, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return null;

        category.Name      = req.Name;
        category.Icon      = req.Icon;
        category.Type      = req.Type;
        category.SortOrder = req.SortOrder;

        await dbFactory.CommitAsync(ct);
        return CategoryResponse.FromModel(category);
    }

    public async Task<bool> DeleteAsync(string userId, int id, CancellationToken ct)
    {
        var db = await dbFactory.GetContextAsync(userId, ct);
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return false;

        db.Categories.Remove(category);
        await dbFactory.CommitAsync(ct);
        return true;
    }
}
