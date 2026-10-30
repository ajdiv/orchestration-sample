using System.Collections.Concurrent;

namespace Platform.Infrastructure;

/// <summary> Idempotency here is based on CustomerId + BankRef </summary>
public interface ITransactionStore
{
    /// <summary>Returns true if the transaction was inserted, false if it already existed.</summary>
    Task<bool> TryInsertAsync(string customerId, string runId, Transaction t);
}

public sealed class InMemoryTransactionStore : ITransactionStore
{
    private readonly ConcurrentDictionary<(string, string), Transaction> rows = new();

    public Task<bool> TryInsertAsync(string customerId, string runId, Transaction t) =>
        Task.FromResult(rows.TryAdd((customerId, t.BankRef), t));
}