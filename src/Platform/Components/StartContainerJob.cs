using Microsoft.DurableTask;
using Platform.Infrastructure;

namespace Platform.Components;

/// <summary>
/// The one generic activity that every scripted component goes through. It starts the container job
/// with the component's input and returns immediately. When the job finishes the platform stores its output 
/// and raises the event the orchestration is waiting for (via the manifest) so nothing is blocking while the script runs.
/// </summary>
public class StartContainerJob(IContainerJobs jobs) : TaskActivity<JobRequest, string>
{
    public override async Task<string> RunAsync(TaskActivityContext context, JobRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N");
        await jobs.StartAsync(context.InstanceId, jobId, request.Component, request.Version, request.InputUri);
        return jobId;
    }
}
