using Platform.Queue;

namespace Platform.Triggers;

/// <summary>
/// In Azure this would run from a a timer or a file-arrival trigger or some adhoc "run" button in an app
///
/// For demo purposes it fires the same schedule twice (should fail), then after the run has finished it 
/// reruns the same file under a new RunId, the way a Customer would. 
/// 
/// The rerun posts zero new transactions because PostTransactions can run multiple times without side effects.
/// </summary>
public class ScheduleTrigger(IRunQueue queue, ILogger<ScheduleTrigger> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // connect the worker to the scheduler
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);   

        var schedule = new { Id = "anthony-daily-statement", CustomerId = "anthony", ApprovedVersion = 3 };
        var fireTime = DateTimeOffset.UtcNow;

        var run = new RunRequest(
            RunId: $"{schedule.Id}-{fireTime:yyyyMMddHHmm}",
            CustomerId: schedule.CustomerId,
            Workflow: "BankStatementImport",
            Version: schedule.ApprovedVersion,
            FileUri: "incoming/statement.csv");

        log.LogInformation("Schedule fired: {RunId}", run.RunId);
        await queue.SendAsync(run, stoppingToken);

        // Proving that a double fire is handled gracefully
        log.LogInformation("Schedule fired again by mistake: {RunId} (should be dropped)", run.RunId);
        await queue.SendAsync(run, stoppingToken);

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        var rerun = run with { RunId = $"{run.RunId}-rerun-{DateTimeOffset.UtcNow:HHmmss}" };
        log.LogInformation("Customer reran the file: {RunId} (should insert nothing new)", rerun.RunId);
        await queue.SendAsync(rerun, stoppingToken);
    }
}
