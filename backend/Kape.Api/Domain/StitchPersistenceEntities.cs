namespace Kape.Api.Domain;

public sealed class StitchAuthorizationRequestRecord
{
    public string State { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string ProtectedPayload { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StitchConnectionSecretRecord
{
    public string ExternalConnectionId { get; set; } = string.Empty;
    public string ProtectedPayload { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
