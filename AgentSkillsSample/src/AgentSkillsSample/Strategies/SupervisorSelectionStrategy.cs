using AgentSkillsSample.Agents;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace AgentSkillsSample.Strategies;

/// <summary>
/// スーパーバイザー選択戦略
/// スーパーバイザーエージェントが次に発言するエージェントを決定します。
/// </summary>
public sealed class SupervisorSelectionStrategy : SelectionStrategy
{
    private int _turnCount = 0;

    /// <summary>
    /// 会話の状態に基づいて次に発言するエージェントを選択します。
    ///
    /// フロー:
    ///   Turn 0: Supervisor   – タスクの分解と指示
    ///   Turn 1: BudgetAnalyzer  – 数値分析（スキル実行）
    ///   Turn 2: ReportGenerator – レポート生成（スキル実行）
    ///   Turn 3: Supervisor   – 結果のまとめと最終回答
    /// </summary>
    public override Task<Agent> NextAsync(
        IReadOnlyList<Agent> agents,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken = default)
    {
        var agentMap = agents.ToDictionary(a => a.Name ?? string.Empty);

        var selected = (_turnCount % 4) switch
        {
            0 => agentMap.GetValueOrDefault(AgentFactory.SupervisorName),
            1 => agentMap.GetValueOrDefault(AgentFactory.BudgetAnalyzerName),
            2 => agentMap.GetValueOrDefault(AgentFactory.ReportGeneratorName),
            _ => agentMap.GetValueOrDefault(AgentFactory.SupervisorName),
        };

        _turnCount++;

        // フォールバック: Supervisorを返す
        selected ??= agents.FirstOrDefault(
            a => a.Name == AgentFactory.SupervisorName) ?? agents[0];

        return Task.FromResult(selected);
    }
}

/// <summary>
/// スーパーバイザー終了戦略
/// スーパーバイザーが "COMPLETE" を出力したか、最大イテレーション数に達した場合に終了します。
/// </summary>
public sealed class SupervisorTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(
        Agent agent,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken = default)
    {
        // スーパーバイザーの最後のメッセージに "COMPLETE" が含まれる場合に終了
        if (agent.Name != AgentFactory.SupervisorName)
            return Task.FromResult(false);

        var lastMessage = history
            .LastOrDefault(m => m.AuthorName == AgentFactory.SupervisorName)
            ?.Content ?? string.Empty;

        return Task.FromResult(
            lastMessage.Contains("COMPLETE", StringComparison.OrdinalIgnoreCase));
    }
}
