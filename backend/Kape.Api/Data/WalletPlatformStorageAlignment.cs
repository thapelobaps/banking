using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Data;

public static class WalletPlatformStorageAlignment
{
    public static void ApplyWalletPlatformStorageAlignment(this ModelBuilder builder)
    {
        builder.Entity<LinkedBankAccount>()
            .Property(x => x.Currency)
            .IsUnicode(false)
            .IsFixedLength()
            .HasMaxLength(3);

        builder.Entity<PaymentMethod>()
            .Property(x => x.Last4)
            .IsUnicode(false)
            .IsFixedLength()
            .HasMaxLength(4);

        builder.Entity<Wallet>()
            .Property(x => x.Currency)
            .IsUnicode(false)
            .IsFixedLength()
            .HasMaxLength(3);

        builder.Entity<LedgerAccount>()
            .Property(x => x.Currency)
            .IsUnicode(false)
            .IsFixedLength()
            .HasMaxLength(3);

        builder.Entity<VoucherProduct>()
            .Property(x => x.Currency)
            .IsUnicode(false)
            .IsFixedLength()
            .HasMaxLength(3);

        builder.Entity<PaymentRequest>()
            .Property(x => x.Currency)
            .IsUnicode(false)
            .IsFixedLength()
            .HasMaxLength(3);
    }
}
