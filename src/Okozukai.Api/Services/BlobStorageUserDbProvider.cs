using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Okozukai.Core.Interfaces;

namespace Okozukai.Api.Services;

/// <summary>
/// Azure Blob Storage を使ったユーザーDBプロバイダー実装。
///
/// ストレージレイアウト:
///   コンテナ: okozukai-userdata
///   Blob パス: users/{userId}/okozukai.db
///
/// 排他制御: Blob Lease（最大60秒）でユーザーごとに書き込み競合を防止。
/// ローカル作業: /tmp/{userId}/okozukai.db に一時展開して操作。
/// </summary>
public class BlobStorageUserDbProvider(
    BlobServiceClient blobServiceClient,
    ILogger<BlobStorageUserDbProvider> logger) : IUserDbProvider
{
    private const string ContainerName = "okozukai-userdata";
    private readonly Dictionary<string, (string TempPath, BlobLeaseClient Lease)> _sessions = [];

    public async Task<string> AcquireConnectionStringAsync(string userId, CancellationToken ct = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobPath = $"users/{userId}/okozukai.db";
        var blob = container.GetBlobClient(blobPath);
        var tempPath = Path.Combine(Path.GetTempPath(), userId, "okozukai.db");

        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        // Blob Leaseを取得（60秒、排他制御）
        var leaseClient = blob.GetBlobLeaseClient();
        try
        {
            await leaseClient.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: ct);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 409)
        {
            throw new InvalidOperationException($"User {userId} DB is currently locked. Please retry.", ex);
        }

        // DBファイルが存在する場合はダウンロード
        if (await blob.ExistsAsync(ct))
        {
            await blob.DownloadToAsync(tempPath, ct);
            logger.LogDebug("Downloaded DB for user {UserId} ({Path})", userId, blobPath);
        }
        else
        {
            logger.LogDebug("New DB will be created for user {UserId}", userId);
        }

        _sessions[userId] = (tempPath, leaseClient);
        return $"Data Source={tempPath}";
    }

    public async Task CommitAsync(string userId, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(userId, out var session)) return;

        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blob = container.GetBlobClient($"users/{userId}/okozukai.db");

        await blob.UploadAsync(session.TempPath, overwrite: true, cancellationToken: ct);
        logger.LogDebug("Uploaded DB for user {UserId}", userId);

        await ReleaseLeaseAsync(session.Lease, ct);
        CleanupTemp(session.TempPath);
        _sessions.Remove(userId);
    }

    public async Task RollbackAsync(string userId, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(userId, out var session)) return;

        logger.LogWarning("Rolling back DB session for user {UserId}", userId);
        await ReleaseLeaseAsync(session.Lease, ct);
        CleanupTemp(session.TempPath);
        _sessions.Remove(userId);
    }

    private static async Task ReleaseLeaseAsync(BlobLeaseClient lease, CancellationToken ct)
    {
        try { await lease.ReleaseAsync(cancellationToken: ct); }
        catch { /* リリース失敗はリース期限切れで自動解放される */ }
    }

    private static void CleanupTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
        catch { /* ベストエフォート */ }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (userId, session) in _sessions)
        {
            logger.LogWarning("Disposing uncommitted session for user {UserId}", userId);
            await ReleaseLeaseAsync(session.Lease, CancellationToken.None);
            CleanupTemp(session.TempPath);
        }
        _sessions.Clear();
    }
}
