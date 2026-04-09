# アーキテクチャドキュメント

## 概要

Microsoft Semantic Kernel Agent Framework の **スキル（KernelPlugin）** 機能を活用した、マルチエージェント家計管理システムのサンプルです。

**スーパーバイザーエージェント**が全体を統括し、複数のスキルを持つ**サブエージェント**に作業を委任するパターンを実装しています。LLM には AWS Bedrock 経由の **Claude Sonnet 4.6** を使用します。

---

## 全体アーキテクチャ

```
ユーザー
   │
   ▼
┌─────────────────────────────────────────────────────┐
│              AgentGroupChat                         │
│                                                     │
│  SupervisorSelectionStrategy (選択戦略)              │
│  SupervisorTerminationStrategy (終了戦略)            │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │  🎯 Supervisor Agent                         │   │
│  │  タスク分解 → 委任 → 最終まとめ              │   │
│  └────────────────┬────────────────────────────┘   │
│                   │ 委任                            │
│         ┌─────────┴──────────┐                     │
│         ▼                    ▼                     │
│  ┌─────────────────┐  ┌─────────────────────────┐  │
│  │ BudgetAnalyzer  │  │   ReportGenerator        │  │
│  │ サブエージェント │  │   サブエージェント        │  │
│  │                 │  │                          │  │
│  │ 📦 スキル1:     │  │ 📦 スキル1:              │  │
│  │ BudgetCalc      │  │ Report                   │  │
│  │ ・calculate_total│  │ ・create_monthly_report  │  │
│  │ ・calc_average  │  │ ・format_category_table  │  │
│  │ ・get_top_exp.  │  │ ・create_trend_chart     │  │
│  │ ・compare_months│  │ ・create_advice_section  │  │
│  │                 │  │                          │  │
│  │ 📦 スキル2:     │  │ 📦 スキル2:              │  │
│  │ Category        │  │ Summary                  │  │
│  │ ・get_breakdown │  │ ・exec_summary           │  │
│  │ ・get_trend     │  │ ・bullet_summary         │  │
│  │ ・find_highest  │  │ ・next_month_goal        │  │
│  │ ・get_summary   │  │ ・comparison_summary     │  │
│  └────────┬────────┘  └──────────┬───────────────┘  │
│           │                      │                   │
│           └──────────┬───────────┘                   │
│                      ▼                              │
│              AWS Bedrock (IAM認証)                  │
│              Claude Sonnet 4.6                      │
└─────────────────────────────────────────────────────┘
```

---

## コンポーネント詳細

### 1. Supervisor Agent（スーパーバイザーエージェント）

**役割**: マルチエージェントシステムの司令塔

| 項目 | 詳細 |
|------|------|
| クラス | `ChatCompletionAgent` |
| スキル | なし（LLM推論のみで判断） |
| 責務 | タスク分解、委任指示、最終回答の生成 |
| 終了条件 | レスポンスに `COMPLETE` を含めることで全会話を終了 |

**フロー**:
```
1. ユーザーリクエストを受信
2. 必要な分析タスクを特定
3. BudgetAnalyzer に数値分析を委任
4. ReportGenerator にレポート化を委任
5. 結果をまとめてユーザーに提示
6. "COMPLETE" を出力して終了
```

---

### 2. BudgetAnalyzer（予算分析サブエージェント）

**役割**: 数値データの取得・計算・カテゴリー分析

#### スキル1: BudgetCalculation

```
BudgetCalculationPlugin
├── calculate_total(year, month)        → 月次合計支出
├── calculate_average()                 → 月平均支出
├── get_top_expenses(count)             → 上位N件の支出
└── compare_months(base, target)        → 2ヶ月比較
```

#### スキル2: Category

```
CategoryPlugin
├── get_category_breakdown(year, month) → カテゴリー別内訳
├── get_category_trend(category)        → 月別推移
├── find_highest_category(year, month)  → 最大支出カテゴリー
└── get_all_categories_summary()        → 全カテゴリーサマリー
```

**スキルの登録方法**:
```csharp
// スキル1の登録
kernel.Plugins.Add(
    KernelPluginFactory.CreateFromObject(
        new BudgetCalculationPlugin(),
        pluginName: "BudgetCalculation"));

// スキル2の登録
kernel.Plugins.Add(
    KernelPluginFactory.CreateFromObject(
        new CategoryPlugin(),
        pluginName: "Category"));

// FunctionChoiceBehavior.Auto() で LLM がスキルを自動選択・実行
var agent = new ChatCompletionAgent
{
    Arguments = new KernelArguments(new PromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
```

---

### 3. ReportGenerator（レポート生成サブエージェント）

**役割**: 分析データの可視化・レポート化・サマリー生成

#### スキル1: Report

```
ReportPlugin
├── create_monthly_report(...)          → 月次レポートヘッダー
├── format_category_table(csvData)      → カテゴリーテーブル
├── create_trend_chart(csvData)         → ASCIIトレンドチャート
└── create_advice_section(...)          → 節約アドバイス
```

#### スキル2: Summary

```
SummaryPlugin
├── create_executive_summary(...)       → エグゼクティブサマリー
├── create_bullet_summary(points)       → 箇条書きまとめ
├── generate_next_month_goal(...)       → 翌月の節約目標
└── create_comparison_summary(...)      → 複数月比較サマリー
```

---

### 4. AgentGroupChat（エージェントグループチャット）

複数エージェントの会話を管理するコンテナです。

```csharp
var chat = new AgentGroupChat(supervisor, budgetAnalyzer, reportGenerator)
{
    ExecutionSettings = new AgentGroupChatSettings
    {
        SelectionStrategy  = new SupervisorSelectionStrategy(),
        TerminationStrategy = new SupervisorTerminationStrategy
        {
            MaximumIterations = 10
        }
    }
};
```

#### SupervisorSelectionStrategy（選択戦略）

スーパーバイザーが会話のターンを制御します。

```
Turn 0: Supervisor      → タスク分解・指示
Turn 1: BudgetAnalyzer  → 数値分析（スキル実行）
Turn 2: ReportGenerator → レポート生成（スキル実行）
Turn 3: Supervisor      → 結果まとめ・最終回答
```

#### SupervisorTerminationStrategy（終了戦略）

Supervisor の発言に `COMPLETE` が含まれる、または最大イテレーション数に達した場合に会話を終了します。

---

## AWS Bedrock 設定

### 認証

AWS の標準認証チェーンを使用します（環境変数 > IAM ロール > プロファイル）。

```bash
# 環境変数での設定例
export AWS_ACCESS_KEY_ID=your_access_key
export AWS_SECRET_ACCESS_KEY=your_secret_key
export AWS_DEFAULT_REGION=us-east-1
```

### モデル設定

`appsettings.json` でモデルIDを設定します。

```json
{
  "AgentSettings": {
    "Bedrock": {
      "Region": "us-east-1",
      "ModelId": "us.anthropic.claude-sonnet-4-6-20250929-v1:0"
    }
  }
}
```

> **注意**: モデルIDはリージョンとクロスリージョン推論の設定によって異なります。  
> AWS コンソールの Bedrock → モデルアクセスで利用可能なモデルを確認してください。

### Kernel への登録

```csharp
var bedrockClient = new AmazonBedrockRuntimeClient(region);

var builder = Kernel.CreateBuilder();
builder.AddBedrockChatCompletionService(
    modelId: settings.Bedrock.ModelId,
    bedrockRuntime: bedrockClient);

var kernel = builder.Build();
```

---

## ディレクトリ構成

```
AgentSkillsSample/
├── AgentSkillsSample.sln
├── docs/
│   └── architecture.md          ← このファイル
└── src/
    └── AgentSkillsSample/
        ├── AgentSkillsSample.csproj
        ├── Program.cs               # エントリーポイント・AgentGroupChat 構成
        ├── appsettings.json         # AWS Bedrock 設定
        ├── Agents/
        │   └── AgentFactory.cs      # エージェント生成・スキル登録
        ├── Configuration/
        │   └── AgentSettings.cs     # 設定モデル
        ├── Models/
        │   └── BudgetEntry.cs       # ドメインモデル
        ├── Plugins/
        │   ├── BudgetCalculationPlugin.cs  # スキル: 予算計算
        │   ├── CategoryPlugin.cs           # スキル: カテゴリー分析
        │   ├── ReportPlugin.cs             # スキル: レポート生成
        │   └── SummaryPlugin.cs            # スキル: サマリー生成
        └── Strategies/
            └── SupervisorSelectionStrategy.cs  # 選択・終了戦略
```

---

## 使用パッケージ

| パッケージ | バージョン | 用途 |
|-----------|-----------|------|
| `Microsoft.SemanticKernel` | 1.74.0 | SK コア・KernelPlugin |
| `Microsoft.SemanticKernel.Agents.Core` | 1.74.0 | Agent Framework (ChatCompletionAgent, AgentGroupChat) |
| `Microsoft.SemanticKernel.Connectors.Amazon` | 1.74.0-alpha | AWS Bedrock コネクター |

---

## スキルの実装パターン

Semantic Kernel でスキルを実装する方法:

```csharp
public sealed class MyPlugin
{
    [KernelFunction("my_function")]           // スキル関数名
    [Description("この関数の説明")]            // LLM が選択の判断に使用
    public string MyFunction(
        [Description("引数の説明")] string input  // 引数の説明
    )
    {
        // 実装
        return result;
    }
}

// エージェントへのスキル登録
kernel.Plugins.Add(
    KernelPluginFactory.CreateFromObject(new MyPlugin(), "PluginName"));
```

---

## 実行手順

### 前提条件

- .NET 8.0 SDK
- AWS CLI（認証済み）またはIAMロール
- AWS Bedrock の Claude Sonnet 4.6 モデルへのアクセス権限

### 実行

```bash
cd AgentSkillsSample/src/AgentSkillsSample
dotnet run
```

### 出力例

```
══════════════════════════════════════════════════════
  家計管理マルチエージェント サンプル
  Microsoft Semantic Kernel Agent Framework + Skills
══════════════════════════════════════════════════════

🎯 Supervisor:
──────────────────────────────────────────────────────
タスクを分析します。BudgetAnalyzerに数値分析を依頼します...

📊 BudgetAnalyzer:
──────────────────────────────────────────────────────
[BudgetCalculation スキル実行]
2024年12月の合計支出: 187,500円
前月比: +15,000円 (+8.7%)...

📝 ReportGenerator:
──────────────────────────────────────────────────────
═══════════════════════════════════════════════════════
  📊 家計レポート - 2024年12月
═══════════════════════════════════════════════════════
...
```
