using Okozukai.Api.Middleware;
using Okozukai.Api.Services;
using Okozukai.Core.DTOs;

namespace Okozukai.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
                       .RequireAuthorization()
                       .WithTags("Categories");

        group.MapGet("/", async (HttpContext ctx, CategoryService svc, CancellationToken ct) =>
        {
            var result = await svc.GetAllAsync(ctx.GetUserId(), ct);
            return Results.Ok(result);
        }).WithSummary("カテゴリ一覧を取得");

        group.MapPost("/", async (HttpContext ctx, CategoryService svc, CreateCategoryRequest req, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(ctx.GetUserId(), req, ct);
            return Results.Created($"/api/categories/{result.Id}", result);
        }).WithSummary("カテゴリを作成");

        group.MapPut("/{id:int}", async (HttpContext ctx, CategoryService svc, int id, UpdateCategoryRequest req, CancellationToken ct) =>
        {
            var result = await svc.UpdateAsync(ctx.GetUserId(), id, req, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithSummary("カテゴリを更新");

        group.MapDelete("/{id:int}", async (HttpContext ctx, CategoryService svc, int id, CancellationToken ct) =>
        {
            var deleted = await svc.DeleteAsync(ctx.GetUserId(), id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithSummary("カテゴリを削除");

        return app;
    }
}
