using Microsoft.EntityFrameworkCore;
using Okozukai.Core.Models;

namespace Okozukai.Api.Data;

/// <summary>
/// ユーザーごとのSQLiteデータベースコンテキスト。
/// FaaS実行時は <see cref="UserDbContextFactory"/> 経由で
/// オブジェクトストレージからダウンロードした一時ファイルに接続する。
/// </summary>
public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("REAL");
            e.HasOne(t => t.Category)
             .WithMany(c => c.Transactions)
             .HasForeignKey(t => t.CategoryId);
            e.HasIndex(t => t.Date);
            e.HasIndex(t => t.CategoryId);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.CategoryId, b.Year, b.Month }).IsUnique();
            e.Property(b => b.MonthlyLimit).HasColumnType("REAL");
        });

        // デフォルトカテゴリのシードデータ
        modelBuilder.Entity<Category>().HasData(DefaultCategories());
    }

    private static Category[] DefaultCategories() =>
    [
        new() { Id = 1, Name = "食費",       Icon = "🍽️",  Type = CategoryType.Expense, SortOrder = 1 },
        new() { Id = 2, Name = "交通費",     Icon = "🚃",  Type = CategoryType.Expense, SortOrder = 2 },
        new() { Id = 3, Name = "日用品",     Icon = "🛒",  Type = CategoryType.Expense, SortOrder = 3 },
        new() { Id = 4, Name = "娯楽",       Icon = "🎮",  Type = CategoryType.Expense, SortOrder = 4 },
        new() { Id = 5, Name = "医療費",     Icon = "🏥",  Type = CategoryType.Expense, SortOrder = 5 },
        new() { Id = 6, Name = "給与",       Icon = "💴",  Type = CategoryType.Income,  SortOrder = 10 },
        new() { Id = 7, Name = "その他収入", Icon = "💰",  Type = CategoryType.Income,  SortOrder = 11 },
        new() { Id = 8, Name = "その他",     Icon = "📌",  Type = CategoryType.Both,    SortOrder = 99 },
    ];
}
