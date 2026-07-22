using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kape.Api.Configuration;
using Kape.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Kape.Api.Services;

public sealed class StitchFinancialDataClient : IStitchFinancialDataClient
{
    private const int PageSize = 100;
    private const int MaximumPagesPerAccount = 20;

    private const string AccountsQuery = """
        query GetAccounts {
          user {
            id
            bankAccounts {
              accountNumber
              accountType
              bankId
              branchCode
              id
              name
              currentBalance
              availableBalance
            }
          }
        }
        """;

    private const string TransactionsQuery = """
        query TransactionsByBankAccount($accountId: ID!, $first: UInt, $after: Cursor) {
          node(id: $accountId) {
            ... on BankAccount {
              transactions(first: $first, after: $after) {
                pageInfo {
                  hasNextPage
                  endCursor
                }
                edges {
                  node {
                    id
                    amount
                    reference
                    description
                    date
                    runningBalance
                  }
                }
              }
            }
          }
        }
        """;

    private const string DebitOrderPaymentsQuery = """
        query DebitOrderPaymentsByBankAccount($accountId: ID!, $first: UInt, $after: Cursor) {
          node(id: $accountId) {
            ... on BankAccount {
              debitOrderPayments(first: $first, after: $after) {
                pageInfo {
                  hasNextPage
                  endCursor
                }
                edges {
                  node {
                    id
                    amount
                    reference
                    date
                  }
                }
              }
            }
          }
        }
        """;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly StitchIntegrationOptions _options;

    public StitchFinancialDataClient(
        IHttpClientFactory httpClientFactory,
        IOptions<StitchIntegrationOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<BankProviderSyncResult> SyncAsync(
        StitchUserTokenSet tokens,
        string? syncCursor,
        CancellationToken cancellationToken)
    {
        using var accountsDocument = await SendGraphQlAsync(
            tokens.AccessToken,
            "GetAccounts",
            AccountsQuery,
            variables: null,
            cancellationToken);

        var user = RequireProperty(
            RequireProperty(accountsDocument.RootElement, "data"),
            "user");
        var sourceAccounts = RequireProperty(user, "bankAccounts");
        if (sourceAccounts.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Stitch returned an invalid bankAccounts payload.");
        }

        var accounts = new List<ProviderLinkedAccount>();
        var transactions = new List<ProviderLinkedTransaction>();
        var debitOrders = new List<ProviderDebitOrder>();

        foreach (var source in sourceAccounts.EnumerateArray())
        {
            var externalAccountId = ReadRequiredString(source, "id");
            var bankId = ReadString(source, "bankId") ?? "unknown";
            var currentBalance = ReadMoney(source, "currentBalance");
            var availableBalance = ReadMoney(source, "availableBalance");
            var currency = availableBalance.Currency ?? currentBalance.Currency ?? "ZAR";

            accounts.Add(new ProviderLinkedAccount(
                externalAccountId,
                InstitutionName(bankId),
                ReadString(source, "name") ?? "Bank account",
                NormaliseAccountType(ReadString(source, "accountType")),
                MaskAccountNumber(ReadString(source, "accountNumber")),
                currency,
                currentBalance.Quantity,
                availableBalance.Quantity));

            transactions.AddRange(await GetTransactionsAsync(
                tokens.AccessToken,
                externalAccountId,
                cancellationToken));
            debitOrders.AddRange(await GetDebitOrderPaymentsAsync(
                tokens.AccessToken,
                externalAccountId,
                cancellationToken));
        }

        return new BankProviderSyncResult(accounts, transactions, debitOrders);
    }

    private async Task<IReadOnlyList<ProviderLinkedTransaction>> GetTransactionsAsync(
        string accessToken,
        string accountId,
        CancellationToken cancellationToken)
    {
        var results = new List<ProviderLinkedTransaction>();
        string? after = null;

        for (var page = 0; page < MaximumPagesPerAccount; page++)
        {
            using var document = await SendGraphQlAsync(
                accessToken,
                "TransactionsByBankAccount",
                TransactionsQuery,
                new { accountId, first = PageSize, after },
                cancellationToken);
            var connection = RequireProperty(
                RequireProperty(
                    RequireProperty(document.RootElement, "data"),
                    "node"),
                "transactions");

            foreach (var edge in RequireProperty(connection, "edges").EnumerateArray())
            {
                var node = RequireProperty(edge, "node");
                var money = ReadMoney(node, "amount");
                var quantity = money.Quantity;
                var description = ReadString(node, "description") ?? "Bank transaction";
                results.Add(new ProviderLinkedTransaction(
                    accountId,
                    ReadRequiredString(node, "id"),
                    description,
                    null,
                    Math.Abs(quantity),
                    quantity >= 0m ? "credit" : "debit",
                    "Uncategorised",
                    "posted",
                    ReadDate(node, "date")));
            }

            var pageInfo = RequireProperty(connection, "pageInfo");
            var hasNextPage = ReadBoolean(pageInfo, "hasNextPage");
            after = ReadString(pageInfo, "endCursor");
            if (!hasNextPage || string.IsNullOrWhiteSpace(after))
            {
                break;
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<ProviderDebitOrder>> GetDebitOrderPaymentsAsync(
        string accessToken,
        string accountId,
        CancellationToken cancellationToken)
    {
        var results = new List<ProviderDebitOrder>();
        string? after = null;

        for (var page = 0; page < MaximumPagesPerAccount; page++)
        {
            using var document = await SendGraphQlAsync(
                accessToken,
                "DebitOrderPaymentsByBankAccount",
                DebitOrderPaymentsQuery,
                new { accountId, first = PageSize, after },
                cancellationToken);
            var connection = RequireProperty(
                RequireProperty(
                    RequireProperty(document.RootElement, "data"),
                    "node"),
                "debitOrderPayments");

            foreach (var edge in RequireProperty(connection, "edges").EnumerateArray())
            {
                var node = RequireProperty(edge, "node");
                var reference = ReadString(node, "reference");
                var money = ReadMoney(node, "amount");
                results.Add(new ProviderDebitOrder(
                    accountId,
                    ReadRequiredString(node, "id"),
                    string.IsNullOrWhiteSpace(reference) ? "Debit order payment" : reference,
                    Math.Abs(money.Quantity),
                    "unknown",
                    "posted",
                    null,
                    ReadDate(node, "date")));
            }

            var pageInfo = RequireProperty(connection, "pageInfo");
            var hasNextPage = ReadBoolean(pageInfo, "hasNextPage");
            after = ReadString(pageInfo, "endCursor");
            if (!hasNextPage || string.IsNullOrWhiteSpace(after))
            {
                break;
            }
        }

        return results;
    }

    private async Task<JsonDocument> SendGraphQlAsync(
        string accessToken,
        string operationName,
        string query,
        object? variables,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.GraphQlEndpoint)
        {
            Content = JsonContent.Create(new
            {
                operationName,
                query,
                variables,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClientFactory
            .CreateClient("Stitch")
            .SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new StitchUnauthenticatedException("The Stitch user token has expired or is invalid.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Stitch GraphQL request failed with HTTP {(int)response.StatusCode}: {SafeBody(body)}");
        }

        var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("errors", out var errors) &&
            errors.ValueKind == JsonValueKind.Array &&
            errors.GetArrayLength() > 0)
        {
            var unauthenticated = errors.EnumerateArray().Any(error =>
                error.TryGetProperty("extensions", out var extensions) &&
                extensions.TryGetProperty("code", out var code) &&
                string.Equals(code.GetString(), "UNAUTHENTICATED", StringComparison.OrdinalIgnoreCase));
            if (unauthenticated)
            {
                document.Dispose();
                throw new StitchUnauthenticatedException("The Stitch user token has expired or is invalid.");
            }

            var message = errors.EnumerateArray()
                .Select(error => ReadString(error, "message"))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
                ?? "Unknown GraphQL error.";
            document.Dispose();
            throw new InvalidOperationException($"Stitch GraphQL request failed: {message}");
        }

        return document;
    }

    private static JsonElement RequireProperty(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Stitch response is missing '{propertyName}'.");
        }

        return value;
    }

    private static string ReadRequiredString(JsonElement source, string propertyName) =>
        ReadString(source, propertyName)
        ?? throw new InvalidOperationException($"Stitch response is missing '{propertyName}'.");

    private static string? ReadString(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static bool ReadBoolean(JsonElement source, string propertyName) =>
        source.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.True;

    private static DateTimeOffset ReadDate(JsonElement source, string propertyName)
    {
        var raw = ReadString(source, propertyName);
        return DateTimeOffset.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var value)
            ? value
            : DateTimeOffset.UtcNow;
    }

    private static MoneyValue ReadMoney(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return new MoneyValue(0m, null);
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric))
        {
            return new MoneyValue(numeric, null);
        }

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return new MoneyValue(parsed, null);
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var quantity = 0m;
            if (value.TryGetProperty("quantity", out var quantityElement))
            {
                if (quantityElement.ValueKind == JsonValueKind.Number)
                {
                    quantityElement.TryGetDecimal(out quantity);
                }
                else if (quantityElement.ValueKind == JsonValueKind.String)
                {
                    decimal.TryParse(
                        quantityElement.GetString(),
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out quantity);
                }
            }

            var currency = value.TryGetProperty("currency", out var currencyElement)
                ? currencyElement.GetString()
                : null;
            return new MoneyValue(quantity, currency);
        }

        return new MoneyValue(0m, null);
    }

    private static string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return "0000";
        }

        var normalized = new string(accountNumber.Where(char.IsLetterOrDigit).ToArray());
        return normalized.Length <= 4 ? normalized : normalized[^4..];
    }

    private static string NormaliseAccountType(string? accountType) =>
        string.IsNullOrWhiteSpace(accountType)
            ? "unknown"
            : accountType.Trim().ToLowerInvariant();

    private static string InstitutionName(string bankId) =>
        bankId.Trim().ToLowerInvariant() switch
        {
            "absa" => "Absa",
            "capitec" => "Capitec",
            "discovery" or "discovery-bank" => "Discovery Bank",
            "fnb" => "FNB",
            "investec" => "Investec",
            "nedbank" => "Nedbank",
            "standard-bank" or "standard_bank" or "standardbank" => "Standard Bank",
            "tymebank" or "tyme-bank" => "TymeBank",
            _ => bankId,
        };

    private static string SafeBody(string body) =>
        string.IsNullOrWhiteSpace(body)
            ? "No response body."
            : body.Length <= 400 ? body : body[..400];

    private sealed record MoneyValue(decimal Quantity, string? Currency);
}
