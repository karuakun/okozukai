using System.CommandLine;
using Okozukai.Cli.Commands;

// ルートコマンド
var root = new RootCommand("おこづかい帳 CLI - 収支管理ツール");

// サブコマンド登録
root.AddCommand(AuthCommand.Create());
root.AddCommand(TransactionCommand.Create());
root.AddCommand(BudgetCommand.Create());
root.AddCommand(SummaryCommand.Create());

return await root.InvokeAsync(args);
