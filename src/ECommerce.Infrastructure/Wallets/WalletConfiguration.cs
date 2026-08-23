using ECommerce.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Wallets;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.CustomerId).IsRequired();
        builder.Property(w => w.Balance).HasPrecision(18, 4);
        builder.Property(w => w.Currency).HasMaxLength(3).IsRequired();
        builder.Property(w => w.LoyaltyPoints).IsRequired();

        builder.HasMany(w => w.WalletTransactions)
            .WithOne()
            .HasForeignKey(wt => wt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.LoyaltyTransactions)
            .WithOne()
            .HasForeignKey(lt => lt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.CustomerId).IsUnique();
    }
}

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(wt => wt.Id);

        builder.Property(wt => wt.Amount).HasPrecision(18, 4);
        builder.Property(wt => wt.BalanceAfter).HasPrecision(18, 4);
        builder.Property(wt => wt.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(wt => wt.ReferenceId).HasMaxLength(100);
    }
}

public sealed class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.HasKey(lt => lt.Id);

        builder.Property(lt => lt.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(lt => lt.ReferenceId).HasMaxLength(100);
    }
}
