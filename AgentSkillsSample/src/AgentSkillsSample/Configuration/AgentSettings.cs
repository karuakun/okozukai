namespace AgentSkillsSample.Configuration;

/// <summary>AWS Bedrock 接続設定</summary>
public sealed class BedrockSettings
{
    /// <summary>
    /// AWS リージョン (例: us-east-1)
    /// </summary>
    public string Region { get; init; } = "us-east-1";

    /// <summary>
    /// AWS Bedrock モデルID
    /// Claude Sonnet 4.6 の例: us.anthropic.claude-sonnet-4-6-20250929-v1:0
    /// ※ クロスリージョン推論プレフィックス "us." を使用する場合は対応リージョンを確認してください
    /// </summary>
    public string ModelId { get; init; } = "us.anthropic.claude-sonnet-4-6-20250929-v1:0";
}

/// <summary>エージェント共通設定</summary>
public sealed class AgentSettings
{
    public BedrockSettings Bedrock { get; init; } = new();

    /// <summary>スーパーバイザーが1ターンで実行する最大ラウンド数</summary>
    public int MaxIterations { get; init; } = 10;

    /// <summary>ストリーミング出力を有効化するか</summary>
    public bool EnableStreaming { get; init; } = true;
}
