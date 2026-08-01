using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Core.Entities;
using PersonalFinance.Infrastructure.Identity;

namespace PersonalFinance.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
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
        
        modelBuilder.HasDefaultSchema("pfa");
        
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("AspNetUsers", "pfa");
            e.Property(u => u.DisplayName).HasMaxLength(100);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<IdentityRole>(e => e.ToTable("AspNetRoles", "pfa"));
        modelBuilder.Entity<IdentityUserRole<string>>(e => e.ToTable("AspNetUserRoles", "pfa"));
        modelBuilder.Entity<IdentityUserClaim<string>>(e => e.ToTable("AspNetUserClaims", "pfa"));
        modelBuilder.Entity<IdentityUserLogin<string>>(e => e.ToTable("AspNetUserLogins", "pfa"));
        modelBuilder.Entity<IdentityUserToken<string>>(e => e.ToTable("AspNetUserTokens", "pfa"));
        modelBuilder.Entity<IdentityRoleClaim<string>>(e => e.ToTable("AspNetRoleClaims", "pfa"));
        
        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.Institution).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.UserId);
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