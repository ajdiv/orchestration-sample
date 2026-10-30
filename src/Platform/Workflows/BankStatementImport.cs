using Microsoft.DurableTask;
using Platform.Components;

namespace Platform.Workflows;

/// <summary>
/// This is the orchestator. It's replayed from history after any crash, and because of that it 
/// should not be responsible for any I/O on its own (outside of logging state as part of its Durable Trask infrastructure)
/// Every step is either an activity call or a wait. This is the stateful piece. The components are the stateless piece.
/// </summary>
public class BankStatementImport : TaskOrchestrator<RunRequest, RunResult>
{
    public override async Task<RunResult> RunAsync(TaskOrchestrationContext context, RunRequest run)
    {
        var log = context.CreateReplaySafeLogger<BankStatementImport>();

        // Timeouts and dropped connections retry with growing waits then fail the step with a reason
        var retry = TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 4,
            firstRetryInterval: TimeSpan.FromSeconds(10),
            backoffCoefficient: 2.0));

        log.LogInformation("Run {RunId} for Customer {CustomerId} starting on workflow v{Version}", run.RunId, run.CustomerId, run.Version);

        // Step 1 (.NET): an activity that runs inside this worker process.
        var parsed = await context.CallActivityAsync<ParsedStatement>(nameof(ParseStatement), run, retry);

        // Step 2 (scripted): one generic activity starts the container job and returns at once.
        // The orchestration does the waiting so no activity is blocked for the length of the job.
        var jobId = await context.CallActivityAsync<string>(nameof(StartContainerJob), new JobRequest(Component: "enrich-fx-rates", Version: "1.0.0", InputUri: parsed.OutputUri), retry);

        // The 30-minute limit here is what would be set in the component's manifest timeout.
        // A timeout throws TaskCanceledException and fails the run with that reason.
        var enriched = await context.WaitForExternalEvent<JobResult>($"job-{jobId}", TimeSpan.FromMinutes(30));

        // Step 3 (.NET with side effects): See method summary of PostResult as to why or how a retry or a rerun never posts twice.
        var posted = await context.CallActivityAsync<PostResult>(nameof(PostTransactions), new PostInput(run.CustomerId, run.RunId, enriched.OutputUri), retry);

        // Step 4. Notify subscribers once
        await context.CallActivityAsync(nameof(Notify), new Notice(run.RunId, posted.Inserted, parsed.RejectedCount));

        return new RunResult(posted.Inserted, parsed.RejectedCount);
    }
}
