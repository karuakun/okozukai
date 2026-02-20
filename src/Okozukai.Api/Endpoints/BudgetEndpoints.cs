using Okozukai.Api.Middleware;
using Okozukai.Api.Services;
using Okozukai.Core.DTOs;

namespace Okozukai.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budgets")
                       .RequireAuthorization()
                       .WithTags("Budgets");

        // GET /api/budgets/progress?year=2025&month=1
        group.MapGet("/progress", async (
            HttpContext ctx,
            BudgetService svc,
            int year,
            int month,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var result = await svc.GetProgressAsync(userId, year, month, ct);
            return Results.Ok(result);
        })
        .WithSummary("予算進捗を取得（実績付き）");

        // PUT /api/budgets  (upsert)
        group.MapPut("/", async (
            HttpContext ctx,
            BudgetService svc,
            UpsertBudgetRequest req,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var result = await svc.UpsertAsync(userId, req, ct);
            return Results.Ok(result);
        })
        .WithSummary("予算を設定（作成または更新）");

        // DELETE /api/budgets/{id}
        group.MapDelete("/{id:int}", async (
            HttpContext ctx,
            BudgetService svc,
            int id,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var deleted = await svc.DeleteAsync(userId, id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("予算を削除");

        return app;
    }
}
