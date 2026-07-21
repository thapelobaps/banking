using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Data;

public static class WalletPlatformRelationshipConfiguration
{
    public static void ApplyWalletPlatformRelationships(this ModelBuilder builder)
    {
        builder.Entity<LedgerAccount>()
            .HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(account => account.WalletId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
