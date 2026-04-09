using AgentSkillsSample.Agents;
using AgentSkillsSample.Configuration;
using AgentSkillsSample.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;

// ──────────────────────────────────────────────────────────────────────────────
// 設定の読み込み
// ──────────────────────────────────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()          // 環境変数で上書き可能
    .Build();

var settings = configuration
    .GetSection("AgentSettings")
    .Get<AgentSettings>() ?? new AgentSettings();

Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("  家計管理マルチエージェント サンプル");
Console.WriteLine("  Microsoft Semantic Kernel Agent Framework + Skills");
Console.WriteLine($"  モデル: {settings.Bedrock.ModelId}");
Console.WriteLine($"  リージョン: {settings.Bedrock.Region}");
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine();

// ──────────────────────────────────────────────────────────────────────────────
// エージェントの生成
//
// スーパーバイザー (Supervisor)
//   └── タスク分解・委任・最終まとめ
//
// サブエージェント1: BudgetAnalyzer
//   ├── スキル1: BudgetCalculation (支出計算・集計)
//   └── スキル2: Category (カテゴリー別分析)
//
// サブエージェント2: ReportGenerator
//   ├── スキル1: Report (レポートフォーマット生成)
//   └── スキル2: Summary (サマリー・要点まとめ)
// ──────────────────────────────────────────────────────────────────────────────
Console.WriteLine("エージェントを初期化中...");

var supervisorAgent      = AgentFactory.CreateSupervisorAgent(settings);
var budgetAnalyzerAgent  = AgentFactory.CreateBudgetAnalyzerAgent(settings);
var reportGeneratorAgent = AgentFactory.CreateReportGeneratorAgent(settings);

// サブエージェントのスキル一覧を表示
Console.WriteLine();
Console.WriteLine($"✅ {AgentFactory.SupervisorName}: 初期化完了");
Console.WriteLine($"✅ {AgentFactory.BudgetAnalyzerName}: 初期化完了");
foreach (var plugin in budgetAnalyzerAgent.Kernel.Plugins)
    Console.WriteLine($"   📦 スキル: {plugin.Name} ({plugin.FunctionCount}個の関数)");

Console.WriteLine($"✅ {AgentFactory.ReportGeneratorName}: 初期化完了");
foreach (var plugin in reportGeneratorAgent.Kernel.Plugins)
    Console.WriteLine($"   📦 スキル: {plugin.Name} ({plugin.FunctionCount}個の関数)");

Console.WriteLine();

// ──────────────────────────────────────────────────────────────────────────────
// AgentGroupChat の構成
// スーパーバイザーが選択戦略を制御し、終了条件を管理します
// ──────────────────────────────────────────────────────────────────────────────
var chat = new AgentGroupChat(supervisorAgent, budgetAnalyzerAgent, reportGeneratorAgent)
{
    ExecutionSettings = new AgentGroupChatSettings
    {
        // スーパーバイザーが順番を制御するカスタム選択戦略
        SelectionStrategy = new SupervisorSelectionStrategy(),

        // "COMPLETE" 出力または最大イテレーション到達で終了
        TerminationStrategy = new SupervisorTerminationStrategy
        {
            MaximumIterations = settings.MaxIterations,
        },
    },
};

// ──────────────────────────────────────────────────────────────────────────────
// サンプルタスクの実行
// ──────────────────────────────────────────────────────────────────────────────
var userRequest = """
    2024年12月の家計を分析して、以下の情報を含む総合レポートを作成してください：
    1. 当月の合計支出と前月（11月）との比較
    2. カテゴリー別の支出内訳と割合
    3. 最も支出が多いカテゴリーとその推移
    4. 翌月（2025年1月）に向けた節約目標と具体的なアドバイス
    """;

Console.WriteLine("───────────────────────────────────────────────────────");
Console.WriteLine("ユーザーリクエスト:");
Console.WriteLine(userRequest);
Console.WriteLine("───────────────────────────────────────────────────────");
Console.WriteLine();

chat.AddChatMessage(new ChatMessageContent(
    Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User,
    userRequest));

// ストリーミングでエージェントの応答を表示
var currentAgent = string.Empty;

await foreach (var message in chat.InvokeAsync())
{
    if (message.AuthorName != currentAgent)
    {
        if (!string.IsNullOrEmpty(currentAgent))
            Console.WriteLine();

        currentAgent = message.AuthorName ?? "Unknown";
        var agentLabel = currentAgent switch
        {
            AgentFactory.SupervisorName      => "🎯 Supervisor",
            AgentFactory.BudgetAnalyzerName  => "📊 BudgetAnalyzer",
            AgentFactory.ReportGeneratorName => "📝 ReportGenerator",
            _                                => $"🤖 {currentAgent}",
        };
        Console.WriteLine($"\n{agentLabel}:");
        Console.WriteLine(new string('─', 50));
    }

    Console.Write(message.Content);
}

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("  完了: マルチエージェントによる家計分析が終了しました");
Console.WriteLine("══════════════════════════════════════════════════════");
