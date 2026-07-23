namespace Kape.Api.Services;

public static class PaymentMethodRules
{
    public static bool IsExpired(int expiryMonth, int expiryYear, DateTimeOffset asOf)
    {
        if (expiryMonth is < 1 or > 12)
        {
            return true;
        }

        return expiryYear < asOf.Year ||
               (expiryYear == asOf.Year && expiryMonth < asOf.Month);
    }
}
