namespace Kape.Api.Services;

public sealed record DemoRecipient(
    Guid Id,
    string BankName,
    string AccountMask,
    string AccountType,
    string Currency);

public static class DemoRecipientDirectory
{
    private static readonly IReadOnlyList<DemoRecipient> Recipients =
    [
        new(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "FNB Demo Recipient",
            "9021",
            "transaction",
            "ZAR"),
        new(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            "Absa Demo Recipient",
            "4816",
            "savings",
            "ZAR"),
        new(
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            "Standard Bank Demo Recipient",
            "7754",
            "transaction",
            "ZAR"),
    ];

    public static IReadOnlyList<DemoRecipient> All => Recipients;

    public static DemoRecipient? Find(Guid accountId) =>
        Recipients.FirstOrDefault(recipient => recipient.Id == accountId);
}
