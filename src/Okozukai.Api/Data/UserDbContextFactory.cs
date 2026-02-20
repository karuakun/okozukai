using Microsoft.EntityFrameworkCore;
using Okozukai.Core.Interfaces;

namespace Okozukai.Api.Data;

/// <summary>
/// リクエストスコープでユーザーのSQLite DBコンテキストを生成・管理する。
/// 1リクエスト = 1ユーザー = 1SQLiteファイルの原則で動作する。
/// </summary>
public class UserDbContextFactory(
    IUserDbProvider dbProvider,
    ILogger<UserDbContextFactory> logger)
{
    private UserDbContext? _context;
    private string? _userId;

    public async Task<UserDbContext> GetContextAsync(string userId, CancellationToken ct = default)
    {
        if (_context is not null)
            return _context;

        _userId = userId;
        var connectionString = await dbProvider.AcquireConnectionStringAsync(userId, ct);
        logger.LogDebug("Acquired DB for user {UserId}", userId);

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(connectionString)
            .Options;

        _context = new UserDbContext(options);

        // 初回アクセス時にマイグレーションを適用（スキーマ作成）
        await _context.Database.MigrateAsync(ct);

        return _context;
    }

    /// <summary>処理完了後にDBをオブジェクトストレージへ書き戻す</summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_userId is null) return;
        await _context!.SaveChangesAsync(ct);
        await dbProvider.CommitAsync(_userId, ct);
        logger.LogDebug("Committed DB for user {UserId}", _userId);
    }
}
