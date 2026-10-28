using GestaoFinanceiraPessoal.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<FinancialProfile> FinancialProfiles => Set<FinancialProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<InvestmentMovement> InvestmentMovements => Set<InvestmentMovement>();
    public DbSet<MonthlySpendingPlan> MonthlySpendingPlans => Set<MonthlySpendingPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.Name).HasMaxLength(120);
            entity.Property(user => user.Email).HasMaxLength(256);
            entity.Property(user => user.NormalizedEmail).HasMaxLength(256);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();

            entity.HasOne(user => user.FinancialProfile)
                .WithOne(profile => profile.User)
                .HasForeignKey<FinancialProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialProfile>(entity =>
        {
            entity.Property(profile => profile.InitialCapital).HasPrecision(18, 2);
            entity.Property(profile => profile.OpeningInvestedAmount).HasPrecision(18, 2);
            entity.Property(profile => profile.EmploymentType).HasConversion<string>().HasMaxLength(20);
            entity.Property(profile => profile.IncomeSetupMode).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(profile => profile.UserId).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(80);
            entity.Property(category => category.Type).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(category => new { category.UserId, category.Type, category.Name }).IsUnique();

            entity.HasOne(category => category.User)
                .WithMany(user => user.Categories)
                .HasForeignKey(category => category.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialTransaction>(entity =>
        {
            entity.Property(transaction => transaction.Description).HasMaxLength(180);
            entity.Property(transaction => transaction.Note).HasMaxLength(1000);
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(transaction => transaction.User)
                .WithMany(user => user.Transactions)
                .HasForeignKey(transaction => transaction.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(transaction => transaction.Category)
                .WithMany(category => category.Transactions)
                .HasForeignKey(transaction => transaction.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(transaction => transaction.RecurringTransaction)
                .WithMany(recurring => recurring.Transactions)
                .HasForeignKey(transaction => transaction.RecurringTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(transaction => new { transaction.RecurringTransactionId, transaction.Date })
                .IsUnique()
                .HasFilter("[RecurringTransactionId] IS NOT NULL");
        });

        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.Property(recurring => recurring.Description).HasMaxLength(180);
            entity.Property(recurring => recurring.Note).HasMaxLength(1000);
            entity.Property(recurring => recurring.Amount).HasPrecision(18, 2);
            entity.Property(recurring => recurring.TotalPurchaseAmount).HasPrecision(18, 2);
            entity.Property(recurring => recurring.Type).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(recurring => recurring.User)
                .WithMany(user => user.RecurringTransactions)
                .HasForeignKey(recurring => recurring.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(recurring => recurring.Category)
                .WithMany(category => category.RecurringTransactions)
                .HasForeignKey(recurring => recurring.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvestmentMovement>(entity =>
        {
            entity.Property(movement => movement.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(movement => movement.Amount).HasPrecision(18, 2);
            entity.Property(movement => movement.Note).HasMaxLength(500);
            entity.HasIndex(movement => new { movement.UserId, movement.Date });

            entity.HasOne(movement => movement.User)
                .WithMany(user => user.InvestmentMovements)
                .HasForeignKey(movement => movement.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MonthlySpendingPlan>(entity =>
        {
            entity.Property(plan => plan.Amount).HasPrecision(18, 2);
            entity.HasIndex(plan => new { plan.UserId, plan.Year, plan.Month }).IsUnique();

            entity.HasOne(plan => plan.User)
                .WithMany(user => user.MonthlySpendingPlans)
                .HasForeignKey(plan => plan.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
