using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace Platform.Queue;

/// <summary>
/// Turns each queue message into one orchestration instance. The "instance id" is the RunId.
/// Even if the queue lets a duplicate through (would be a bug), the scheduler will not start a second run with that id
/// </summary>
public class RunQueueConsumer(IRunQueue queue, DurableTaskClient durable, ILogger<RunQueueConsumer> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        queue.StartReceivingAsync(async run =>
        {
            var existing = await durable.GetInstanceAsync(run.RunId, stoppingToken);
            if (existing is not null)
            {
                log.LogWarning("Run {RunId} already exists ({Status}); dropping the duplicate message", run.RunId, existing.RuntimeStatus);
                return;
            }

            await durable.ScheduleNewOrchestrationInstanceAsync(
                run.Workflow, // "BankStatementImport" in this example                                  
                run,
                new StartOrchestrationOptions { InstanceId = run.RunId },
                stoppingToken);

            log.LogInformation("Run {RunId} scheduled", run.RunId);
        }, stoppingToken);
}
