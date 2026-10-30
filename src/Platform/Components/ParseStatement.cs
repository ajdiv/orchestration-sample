using Microsoft.DurableTask;
using Platform.Infrastructure;
using System.Globalization;
using System.Text.Json;

namespace Platform.Components;

/// <summary>
/// This is a sample STATELESS compiled .NET component that takes an input in and sends output out.
/// The framework schedules it, times it out, retries it, and records its result in the run history.
/// Records that fail validation are set aside for the Customer to fix or skip with the rest moving on.
/// </summary>
public class ParseStatement(IFileStore files) : TaskActivity<RunRequest, ParsedStatement>
{
    public override async Task<ParsedStatement> RunAsync(TaskActivityContext context, RunRequest run)
    {
        var text = await files.ReadTextAsync(run.FileUri);
        var (good, bad) = Parse(text);

        var outputUri = await files.WriteTextAsync($"runs/{run.RunId}/parsed.json", JsonSerializer.Serialize(good));
        string? rejectedUri = null;

        if (bad.Count > 0) rejectedUri = await files.WriteTextAsync($"runs/{run.RunId}/rejected.json", JsonSerializer.Serialize(bad));

        return new ParsedStatement(outputUri, rejectedUri, bad.Count);
    }

    /// <summary>Sample business logic</summary>
    public static (List<Transaction> Good, List<RejectedRecord> Bad) Parse(string csv)
    {
        var good = new List<Transaction>();
        var bad = new List<RejectedRecord>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // pretending line 0 is a header row with 4 columns: bankRef, postedOn, currency, amount
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length != 4)
            {
                bad.Add(new RejectedRecord(i + 1, lines[i], "expected 4 columns"));
                continue;
            }
            if (!decimal.TryParse(cols[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                bad.Add(new RejectedRecord(i + 1, lines[i], $"amount '{cols[3]}' is not a number"));
                continue;
            }
            if (!DateOnly.TryParseExact(cols[1], "yyyy-MM-dd", out _))
            {
                bad.Add(new RejectedRecord(i + 1, lines[i], $"postedOn '{cols[1]}' is not yyyy-MM-dd"));
                continue;
            }
            good.Add(new Transaction(cols[0], cols[1], cols[2].ToUpperInvariant(), amount, null));
        }
        return (good, bad);
    }
}
