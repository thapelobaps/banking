using Kape.Api.Domain;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService
{
    private const string FeeRevenueCode = "SYS:FEE_REVENUE:ZAR";

    private sealed record JournalLine(string AccountCode, string EntryType, decimal Amount);

    private Task AddTopUpJournalAsync(
        WalletTransaction transaction,
        string walletCode,
        decimal amount,
        decimal fee,
        CancellationToken cancellationToken)
    {
        var lines = new List<JournalLine>
        {
            new(CashClearingCode, "debit", amount + fee),
            new(walletCode, "credit", amount),
        };

        if (fee > 0m)
        {
            lines.Add(new JournalLine(FeeRevenueCode, "credit", fee));
        }

        return AddBalancedJournalAsync(Guid.NewGuid(), transaction, lines, cancellationToken);
    }

    private Task AddWalletDebitJournalAsync(
        WalletTransaction transaction,
        string walletCode,
        string settlementCode,
        decimal amount,
        decimal fee,
        CancellationToken cancellationToken)
    {
        var lines = new List<JournalLine>
        {
            new(walletCode, "debit", amount + fee),
            new(settlementCode, "credit", amount),
        };

        if (fee > 0m)
        {
            lines.Add(new JournalLine(FeeRevenueCode, "credit", fee));
        }

        return AddBalancedJournalAsync(Guid.NewGuid(), transaction, lines, cancellationToken);
    }

    private async Task AddBalancedJournalAsync(
        Guid journalId,
        WalletTransaction transaction,
        IReadOnlyCollection<JournalLine> lines,
        CancellationToken cancellationToken)
    {
        if (lines.Count < 2 || lines.Any(line => line.Amount <= 0m))
        {
            throw new InvalidOperationException("A ledger journal requires at least two positive lines.");
        }

        var debitTotal = lines
            .Where(line => line.EntryType == "debit")
            .Sum(line => line.Amount);
        var creditTotal = lines
            .Where(line => line.EntryType == "credit")
            .Sum(line => line.Amount);

        if (debitTotal != creditTotal)
        {
            throw new InvalidOperationException(
                $"Ledger journal {journalId} is not balanced. Debits: {debitTotal:N2}; credits: {creditTotal:N2}.");
        }

        var resolvedAccounts = new Dictionary<string, LedgerAccount>(StringComparer.Ordinal);
        foreach (var code in lines.Select(line => line.AccountCode).Distinct(StringComparer.Ordinal))
        {
            resolvedAccounts[code] = code == FeeRevenueCode
                ? await GetOrCreateFeeRevenueAccountAsync(cancellationToken)
                : await GetLedgerAccountAsync(code, cancellationToken);
        }

        _repository.AddRange(lines.Select(line => new LedgerEntry
        {
            JournalId = journalId,
            LedgerAccountId = resolvedAccounts[line.AccountCode].Id,
            WalletTransactionId = transaction.Id,
            EntryType = line.EntryType,
            Amount = line.Amount,
            Reference = transaction.Reference,
        }));
    }

    private async Task<LedgerAccount> GetOrCreateFeeRevenueAccountAsync(
        CancellationToken cancellationToken)
    {
        var account = await _repository.GetAsync<LedgerAccount>(
            item => item.Code == FeeRevenueCode,
            tracking: true,
            cancellationToken);
        if (account is not null)
        {
            return account;
        }

        account = new LedgerAccount
        {
            Code = FeeRevenueCode,
            Name = "Kape fee revenue",
            AccountType = "revenue",
            Currency = "ZAR",
            IsSystem = true,
        };
        _repository.Add(account);
        return account;
    }
}
