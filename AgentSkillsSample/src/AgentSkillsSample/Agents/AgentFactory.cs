using AgentSkillsSample.Configuration;
using AgentSkillsSample.Plugins;
using Amazon;
using Amazon.BedrockRuntime;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.Amazon;

namespace AgentSkillsSample.Agents;

/// <summary>
/// エージェントファクトリ
/// スーパーバイザーとサブエージェントの生成・スキル設定を担当
/// </summary>
public static class AgentFactory
{
    // ────────────────────────────────────────────────────────────────
    // エージェント識別名
    // ────────────────────────────────────────────────────────────────
    public const string SupervisorName      = "Supervisor";
    public const string BudgetAnalyzerName  = "BudgetAnalyzer";
    public const string ReportGeneratorName = "ReportGenerator";

    // ────────────────────────────────────────────────────────────────
    // Kernel ファクトリ
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// AWS Bedrock (Claude Sonnet 4.6) に接続した Kernel を生成します。
    /// </summary>
    public static Kernel CreateKernel(AgentSettings settings)
    {
        var region = RegionEndpoint.GetBySystemName(settings.Bedrock.Region);
        var bedrockClient = new AmazonBedrockRuntimeClient(region);

        var builder = Kernel.CreateBuilder();

        // Amazon Bedrock コネクターを使用して Claude Sonnet 4.6 を登録
        builder.AddBedrockChatCompletionService(
            modelId: settings.Bedrock.ModelId,
            bedrockRuntime: bedrockClient);

        return builder.Build();
    }

    // ────────────────────────────────────────────────────────────────
    // スーパーバイザーエージェント
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// スーパーバイザーエージェントを生成します。
    /// タスクの分解とサブエージェントへの委任を担当します。
    /// </summary>
    public static ChatCompletionAgent CreateSupervisorAgent(AgentSettings settings)
    {
        var kernel = CreateKernel(settings);

        return new ChatCompletionAgent
        {
            Name         = SupervisorName,
            Instructions = SupervisorInstructions,
            Kernel       = kernel,
        };
    }

    // ────────────────────────────────────────────────────────────────
    // サブエージェント: BudgetAnalyzer（複数スキル設定）
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 予算分析サブエージェントを生成します。
    /// <list type="bullet">
    ///   <item>スキル1: BudgetCalculation – 支出計算・集計</item>
    ///   <item>スキル2: Category        – カテゴリー別分析</item>
    /// </list>
    /// </summary>
    public static ChatCompletionAgent CreateBudgetAnalyzerAgent(AgentSettings settings)
    {
        var kernel = CreateKernel(settings);

        // ── スキル1: 予算計算 ──────────────────────────────────────
        kernel.Plugins.Add(
            KernelPluginFactory.CreateFromObject(
                new BudgetCalculationPlugin(),
                pluginName: "BudgetCalculation"));

        // ── スキル2: カテゴリー分析 ───────────────────────────────
        kernel.Plugins.Add(
            KernelPluginFactory.CreateFromObject(
                new CategoryPlugin(),
                pluginName: "Category"));

        return new ChatCompletionAgent
        {
            Name         = BudgetAnalyzerName,
            Instructions = BudgetAnalyzerInstructions,
            Kernel       = kernel,
            // スキルを自動選択して実行するよう設定
            Arguments    = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            }),
        };
    }

    // ────────────────────────────────────────────────────────────────
    // サブエージェント: ReportGenerator（複数スキル設定）
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// レポート生成サブエージェントを生成します。
    /// <list type="bullet">
    ///   <item>スキル1: Report  – レポートフォーマット・整形</item>
    ///   <item>スキル2: Summary – サマリー・要点生成</item>
    /// </list>
    /// </summary>
    public static ChatCompletionAgent CreateReportGeneratorAgent(AgentSettings settings)
    {
        var kernel = CreateKernel(settings);

        // ── スキル1: レポート生成 ─────────────────────────────────
        kernel.Plugins.Add(
            KernelPluginFactory.CreateFromObject(
                new ReportPlugin(),
                pluginName: "Report"));

        // ── スキル2: サマリー生成 ─────────────────────────────────
        kernel.Plugins.Add(
            KernelPluginFactory.CreateFromObject(
                new SummaryPlugin(),
                pluginName: "Summary"));

        return new ChatCompletionAgent
        {
            Name         = ReportGeneratorName,
            Instructions = ReportGeneratorInstructions,
            Kernel       = kernel,
            Arguments    = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            }),
        };
    }

    // ────────────────────────────────────────────────────────────────
    // エージェント指示文
    // ────────────────────────────────────────────────────────────────

    private const string SupervisorInstructions = """
        あなたは家計管理チームのスーパーバイザーです。
        ユーザーのリクエストを受け取り、適切なサブエージェントに作業を委任し、最終的な回答をまとめます。

        ## 担当サブエージェント

        ### BudgetAnalyzer
        - 担当: 数値分析・計算・カテゴリー分類
        - 呼び出し例: 月次合計、カテゴリー内訳、前月比較の計算依頼

        ### ReportGenerator
        - 担当: レポート生成・データの可視化・サマリー作成
        - 呼び出し例: フォーマットされたレポート、グラフ、要点まとめの生成依頼

        ## 進行ルール
        1. ユーザーのリクエストを分析し、必要な作業を特定します
        2. まず BudgetAnalyzer に数値データの取得と分析を依頼します
        3. 次に ReportGenerator に分析結果のレポート化を依頼します
        4. 両エージェントの出力をまとめ、ユーザーに最終回答を提供します
        5. すべての作業が完了したら "COMPLETE" と出力して終了を示します
        """;

    private const string BudgetAnalyzerInstructions = """
        あなたは家計の予算分析スペシャリストです。
        以下の2つのスキル（KernelPlugin）を使用して正確な分析を行います。

        ## 利用可能なスキル

        ### BudgetCalculation スキル
        - calculate_total     : 指定月の合計支出を計算
        - calculate_average   : 月平均支出を計算
        - get_top_expenses    : 支出上位N件を取得
        - compare_months      : 2ヶ月間の比較

        ### Category スキル
        - get_category_breakdown  : カテゴリー別内訳を取得
        - get_category_trend      : カテゴリーの月別推移
        - find_highest_category   : 最大支出カテゴリーを特定
        - get_all_categories_summary : 全カテゴリーのサマリー

        ## 分析ルール
        - 必ず具体的な数値を示してください
        - スキルから取得したデータをそのまま使用し、推測で補完しないでください
        - 分析結果は構造化されたJSON形式でReportGeneratorに渡すことを意識してください
        """;

    private const string ReportGeneratorInstructions = """
        あなたは家計レポートの生成スペシャリストです。
        BudgetAnalyzer から受け取ったデータを使い、以下の2つのスキルで分かりやすいレポートを作成します。

        ## 利用可能なスキル

        ### Report スキル
        - create_monthly_report  : 月次レポートのヘッダーと概要を生成
        - format_category_table  : カテゴリーテーブルを整形
        - create_trend_chart     : 月別推移のASCIIチャートを生成
        - create_advice_section  : 節約アドバイスを生成

        ### Summary スキル
        - create_executive_summary  : エグゼクティブサマリーを生成
        - create_bullet_summary     : 箇条書き要点まとめを生成
        - generate_next_month_goal  : 翌月の目標を提案
        - create_comparison_summary : 複数月の比較サマリーを生成

        ## レポート作成ルール
        - BudgetAnalyzer の数値データを正確に使用してください
        - レポートは見やすく、日本語で作成してください
        - 数値は円単位でカンマ区切りにしてください
        - ポジティブなトーンで、ユーザーが行動できる形でアドバイスしてください
        """;
}
