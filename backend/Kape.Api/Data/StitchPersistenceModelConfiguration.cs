using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Data;

public static class StitchPersistenceModelConfiguration
{
    public static void ConfigureStitchPersistence(this ModelBuilder builder)
    {
        builder.Entity<StitchAuthorizationRequestRecord>(entity =>
        {
            entity.ToTable("StitchAuthorizationRequests");
            entity.HasKey(record => record.State);
            entity.Property(record => record.State).HasMaxLength(128).IsRequired();
            entity.Property(record => record.ProtectedPayload).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(record => record.ExpiresAt).HasColumnType("datetimeoffset(7)");
            entity.Property(record => record.CreatedAt).HasColumnType("datetimeoffset(7)");
            entity.HasIndex(record => record.ExpiresAt)
                .HasDatabaseName("IX_StitchAuthorizationRequests_ExpiresAt");
            entity.HasIndex(record => new { record.UserId, record.CreatedAt })
                .HasDatabaseName("IX_StitchAuthorizationRequests_User_CreatedAt");
        });

        builder.Entity<StitchConnectionSecretRecord>(entity =>
        {
            entity.ToTable("StitchConnectionSecrets");
            entity.HasKey(record => record.ExternalConnectionId);
            entity.Property(record => record.ExternalConnectionId).HasMaxLength(180).IsRequired();
            entity.Property(record => record.ProtectedPayload).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(record => record.AccessTokenExpiresAt).HasColumnType("datetimeoffset(7)");
            entity.Property(record => record.CreatedAt).HasColumnType("datetimeoffset(7)");
            entity.Property(record => record.UpdatedAt).HasColumnType("datetimeoffset(7)");
            entity.HasIndex(record => record.AccessTokenExpiresAt)
                .HasDatabaseName("IX_StitchConnectionSecrets_AccessTokenExpiresAt");
            entity.HasIndex(record => record.UpdatedAt)
                .HasDatabaseName("IX_StitchConnectionSecrets_UpdatedAt");
        });
    }
}
