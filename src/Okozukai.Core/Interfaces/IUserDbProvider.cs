namespace Okozukai.Core.Interfaces;

/// <summary>
/// ユーザーごとのSQLiteデータベース接続を提供する。
/// FaaS実行時はオブジェクトストレージからDBファイルを取得し、
/// 処理後に書き戻す責務を持つ。
/// </summary>
public interface IUserDbProvider : IAsyncDisposable
{
    /// <summary>ユーザーDBへの接続文字列を返す（ファイルをローカルに確保した後）</summary>
    Task<string> AcquireConnectionStringAsync(string userId, CancellationToken ct = default);

    /// <summary>処理済みのDBファイルをオブジェクトストレージへ書き戻す</summary>
    Task CommitAsync(string userId, CancellationToken ct = default);

    /// <summary>書き戻しをせずにローカルの一時ファイルを破棄する（エラー時）</summary>
    Task RollbackAsync(string userId, CancellationToken ct = default);
}
