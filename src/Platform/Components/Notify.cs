using Microsoft.DurableTask;

namespace Platform.Components;

/// <summary>
/// Sends the once-per-run notification. In Azure this would publish to Event Grid and/or Communication Services
/// Here it logs which is enough to see the flow end.
/// </summary>
public class Notify(ILogger<Notify> log) : TaskActivity<Notice, object?>
{
    public override Task<object?> RunAsync(TaskActivityContext context, Notice notice)
    {
        log.LogInformation("Run {RunId} finished: {Posted} transactions posted, {Rejected} records set aside for the Customer",
            notice.RunId, notice.Posted, notice.Rejected);
        return Task.FromResult<object?>(null);
    }
}
