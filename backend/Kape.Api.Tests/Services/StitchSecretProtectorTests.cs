using System.Security.Cryptography;
using Kape.Api.Configuration;
using Kape.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kape.Api.Tests.Services;

public sealed class StitchSecretProtectorTests
{
    [Fact]
    public void Protect_RoundTripsWithoutEmbeddingPlaintext()
    {
        var protector = CreateProtector(RandomNumberGenerator.GetBytes(32));
        const string plaintext = "refresh-token-that-must-never-be-stored-in-plain-text";

        var protectedPayload = protector.Protect(plaintext);

        Assert.DoesNotContain(plaintext, protectedPayload, StringComparison.Ordinal);
        Assert.Equal(plaintext, protector.Unprotect(protectedPayload));
    }

    [Fact]
    public void Unprotect_RejectsTamperedCiphertext()
    {
        var protector = CreateProtector(RandomNumberGenerator.GetBytes(32));
        var envelope = Convert.FromBase64String(protector.Protect("sensitive-token"));
        envelope[^1] ^= 0x01;

        Assert.Throws<CryptographicException>(() =>
            protector.Unprotect(Convert.ToBase64String(envelope)));
    }

    [Fact]
    public void Unprotect_RejectsAnotherEnvironmentKey()
    {
        var first = CreateProtector(RandomNumberGenerator.GetBytes(32));
        var second = CreateProtector(RandomNumberGenerator.GetBytes(32));
        var protectedPayload = first.Protect("sandbox-refresh-token");

        Assert.Throws<CryptographicException>(() => second.Unprotect(protectedPayload));
    }

    private static AesGcmStitchSecretProtector CreateProtector(byte[] key) =>
        new(Options.Create(new StitchIntegrationOptions
        {
            StorageEncryptionKey = Convert.ToBase64String(key),
        }));
}
