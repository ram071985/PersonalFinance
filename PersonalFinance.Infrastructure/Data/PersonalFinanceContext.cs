using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;

namespace PersonalFinance.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.Institution).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.Icon).HasMaxLength(50);
            e.Property(x => x.Color).HasMaxLength(20);
            e.HasIndex(x => x.Type);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);

            e.HasOne(x => x.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.TransferToAccount)
                .WithMany()
                .HasForeignKey(x => x.TransferToAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.Date);
            e.HasIndex(x => x.AccountId);
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(200);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Budgets)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.CategoryId, x.Year, x.Month }).IsUnique();
        });
    }
}