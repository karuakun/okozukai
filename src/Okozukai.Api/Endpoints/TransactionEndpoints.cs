using Okozukai.Api.Middleware;
using Okozukai.Api.Services;
using Okozukai.Core.DTOs;

namespace Okozukai.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions")
                       .RequireAuthorization()
                       .WithTags("Transactions");

        // GET /api/transactions?from=2025-01-01&to=2025-01-31&categoryId=1
        group.MapGet("/", async (
            HttpContext ctx,
            TransactionService svc,
            DateOnly? from,
            DateOnly? to,
            int? categoryId,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var result = await svc.GetAllAsync(userId, from, to, categoryId, ct);
            return Results.Ok(result);
        })
        .WithSummary("収支一覧を取得");

        // GET /api/transactions/summary?year=2025&month=1
        group.MapGet("/summary", async (
            HttpContext ctx,
            TransactionService svc,
            int year,
            int month,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var result = await svc.GetMonthlySummaryAsync(userId, year, month, ct);
            return Results.Ok(result);
        })
        .WithSummary("月次サマリを取得");

        // GET /api/transactions/{id}
        group.MapGet("/{id:int}", async (
            HttpContext ctx,
            TransactionService svc,
            int id,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var result = await svc.GetByIdAsync(userId, id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithSummary("収支詳細を取得");

        // POST /api/transactions
        group.MapPost("/", async (
            HttpContext ctx,
            TransactionService svc,
            CreateTransactionRequest req,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var result = await svc.CreateAsync(userId, req, ct);
            return Results.Created($"/api/transactions/{result.Id}", result);
        })
        .WithSummary("収支を登録");

        // PUT /api/transactions/{id}
        group.MapPut("/{id:int}", async (
            HttpContext ctx,
            TransactionService svc,
            int id,
            UpdateTransactionRequest req,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var result = await svc.UpdateAsync(userId, id, req, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithSummary("収支を更新");

        // DELETE /api/transactions/{id}
        group.MapDelete("/{id:int}", async (
            HttpContext ctx,
            TransactionService svc,
            int id,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var deleted = await svc.DeleteAsync(userId, id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("収支を削除");

        return app;
    }
}
