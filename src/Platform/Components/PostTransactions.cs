using System.Text.Json;
using Microsoft.DurableTask;
using Platform.Infrastructure;

namespace Platform.Components;

/// <summary>
/// This is a sample .NET component with a side effect (similar to the payment example from Rick)
/// Checking CustomerId and BankRef before each write makes this idempotent. Aka a retry after a crash, or a Customer rerunning the whole run inserts nothing the second time
/// </summary>
public class PostTransactions(ITransactionStore store, IFileStore files) : TaskActivity<PostInput, PostResult>
{
    public override async Task<PostResult> RunAsync(TaskActivityContext context, PostInput input)
    {
        var json = await files.ReadTextAsync(input.InputUri);
        var txns = JsonSerializer.Deserialize<List<Transaction>>(json) ?? [];

        int inserted = 0, skipped = 0;
        foreach (var t in txns)
        {
            if (await store.TryInsertAsync(input.CustomerId, input.RunId, t)) inserted++;
            else skipped++;
        }
        return new PostResult(inserted, skipped);
    }
}
