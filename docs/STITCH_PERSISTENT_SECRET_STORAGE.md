# Stitch persistent secret storage

Kape stores Stitch OAuth state and connection tokens in SQL Server. The database contains only authenticated ciphertext plus operational identifiers and timestamps.

## Required environment variable

Generate a dedicated 256-bit encryption key once per environment. Do not reuse the JWT signing key.

```powershell
$key = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($key)
$storageKey = [Convert]::ToBase64String($key)
$storageKey
```

Store the result in a secret manager or .NET user secrets:

```powershell
dotnet user-secrets init --project backend/Kape.Api
dotnet user-secrets set "Providers:Stitch:StorageEncryptionKey" "<base64-256-bit-key>" --project backend/Kape.Api
```

Never commit the generated value. Every API instance for the same environment must receive the same key. Rotating the key requires a controlled token re-encryption process or reconnecting affected bank connections.

## Database migration

Apply the generated migration before enabling Stitch:

```powershell
dotnet ef database update `
  --project backend/Kape.Api/Kape.Api.csproj `
  --startup-project backend/Kape.Api/Kape.Api.csproj
```

The migration creates:

- `StitchAuthorizationRequests` for short-lived, single-use OAuth state and PKCE data.
- `StitchConnectionSecrets` for encrypted user access tokens, refresh tokens, ID tokens and sync cursors.

## Security behaviour

- AES-256-GCM provides encryption and tamper detection.
- Every encrypted envelope uses a random 96-bit nonce.
- OAuth state is consumed inside a serializable transaction and deleted before the callback continues.
- Expired authorization requests are removed during new authorization-session creation.
- Corrupted or undecryptable connection records fail closed and are deleted.
- Disconnecting a Stitch connection deletes its encrypted connection-secret record.
- Access tokens, refresh tokens, authorization codes and encryption keys are never logged or returned by Kape APIs.

## Local demo regression

Stitch remains disabled by default. After applying the migration, run the application with the demo provider and verify bank connection, synchronization and disconnection still work.

## Production key management

Use a managed secret store such as Azure Key Vault for the encryption key. Restrict access to the API workload identity, enable audit logging and maintain separate keys for development, sandbox and production.
