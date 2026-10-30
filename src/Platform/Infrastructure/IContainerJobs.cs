using Microsoft.DurableTask.Client;
using System.Diagnostics;

namespace Platform.Infrastructure;

/// <summary>Starts a scripted component. Would be a Container Apps job in Azure but is a local process here.</summary>
public interface IContainerJobs
{
    Task StartAsync(string instanceId, string jobId, string component, string version, string inputPath);
}

/// <summary>
/// Runs main.py as a child process (in this example). The script only reads its input and prints its result.
/// This class stores what the script prints and raises the event the orchestration is waiting for so the script 
/// doesnt need to worry about callbacks on its own
/// </summary>
public sealed class LocalProcessJobs(
    PlatformOptions options,
    IFileStore files,
    DurableTaskClient durable,
    ILogger<LocalProcessJobs> log) : IContainerJobs
{
    public Task StartAsync(string instanceId, string jobId, string component, string version, string inputPath)
    {
        var script = Path.GetFullPath(Path.Combine(options.ComponentsRoot, component, "main.py"));
        var psi = new ProcessStartInfo(options.PythonExe, $"\"{script}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.Environment["INPUT_PATH"] = inputPath;

        var process = Process.Start(psi)!;
        _ = Task.Run(async () =>
        {
            try
            {
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                log.LogInformation("Job {JobId} ({Component} {Version}) exited {Code}. {Err}",
                    jobId, component, version, process.ExitCode, await stderr);

                // A failed script raises no event so the the workflow workflow's timeout fails the run with that reason.
                if (process.ExitCode != 0) return;

                var outputUri = await files.WriteTextAsync($"runs/{instanceId}/{component}.json", await stdout);
                await durable.RaiseEventAsync(instanceId, $"job-{jobId}", new JobResult(jobId, outputUri));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Job {JobId} ({Component} {Version}) could not be completed", jobId, component, version);
            }
        });
        return Task.CompletedTask;
    }
}
