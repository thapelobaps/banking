using Kape.Api.Services;
using Xunit;

namespace Kape.Api.Tests.Services;

public sealed class PaymentMethodRulesTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(6, 2026, true)]
    [InlineData(7, 2026, false)]
    [InlineData(12, 2026, false)]
    [InlineData(1, 2027, false)]
    [InlineData(12, 2025, true)]
    public void IsExpired_UsesExpiryMonthBoundary(int month, int year, bool expected)
    {
        Assert.Equal(expected, PaymentMethodRules.IsExpired(month, year, AsOf));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void IsExpired_RejectsInvalidMonths(int month)
    {
        Assert.True(PaymentMethodRules.IsExpired(month, 2028, AsOf));
    }
}
