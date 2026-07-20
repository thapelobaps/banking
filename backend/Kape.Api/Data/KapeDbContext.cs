using Kape.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Data;

public sealed class KapeDbContext(DbContextOptions<KapeDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.MobileNumber).HasMaxLength(16).IsRequired();
            entity.Property(user => user.AddressLine1).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Suburb).HasMaxLength(60).IsRequired();
            entity.Property(user => user.City).HasMaxLength(60).IsRequired();
            entity.Property(user => user.Province).HasMaxLength(30).IsRequired();
            entity.Property(user => user.PostalCode).HasMaxLength(4).IsRequired();
            entity.Property(user => user.Country).HasMaxLength(30).IsRequired();
            entity.HasIndex(user => user.MobileNumber);
        });

        builder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(account => account.Id);
            entity.Property(account => account.ProviderId).HasMaxLength(50).IsRequired();
            entity.Property(account => account.BankId).HasMaxLength(50).IsRequired();
            entity.Property(account => account.BankName).HasMaxLength(80).IsRequired();
            entity.Property(account => account.AccountNumber).HasMaxLength(40).IsRequired();
            entity.Property(account => account.BranchCode).HasMaxLength(6).IsRequired();
            entity.Property(account => account.AccountType).HasMaxLength(30).IsRequired();
            entity.Property(account => account.CurrentBalance).HasPrecision(18, 2);
            entity.Property(account => account.AvailableBalance).HasPrecision(18, 2);
            entity.Property(account => account.Currency).HasMaxLength(3).IsRequired();
            entity.HasIndex(account => account.AccountNumber).IsUnique();
            entity.HasOne(account => account.User)
                .WithMany(user => user.BankAccounts)
                .HasForeignKey(account => account.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BankTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Name).HasMaxLength(120).IsRequired();
            entity.Property(transaction => transaction.StatementDescription).HasMaxLength(160).IsRequired();
            entity.Property(transaction => transaction.Beneficiary).HasMaxLength(120);
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Direction).HasMaxLength(10).IsRequired();
            entity.Property(transaction => transaction.Category).HasMaxLength(50).IsRequired();
            entity.Property(transaction => transaction.Channel).HasMaxLength(30).IsRequired();
            entity.Property(transaction => transaction.Status).HasMaxLength(20).IsRequired();
            entity.HasIndex(transaction => new { transaction.BankAccountId, transaction.TransactionDate });
            entity.HasOne(transaction => transaction.BankAccount)
                .WithMany(account => account.Transactions)
                .HasForeignKey(transaction => transaction.BankAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
