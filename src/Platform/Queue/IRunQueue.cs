using System.Threading.Channels;

namespace Platform.Queue;

/// <summary>This would be a Service Bus in Azure but is an in-process channel locally.</summary>
public interface IRunQueue
{
    Task SendAsync(RunRequest run, CancellationToken ct = default);
    Task StartReceivingAsync(Func<RunRequest, Task> handler, CancellationToken ct);
}


/// <summary>Local stand-in with the same shape, so the rest of the code doesn't know the difference.</summary>
public sealed class LocalRunQueue : IRunQueue
{
    private readonly Channel<RunRequest> channel = Channel.CreateUnbounded<RunRequest>();

    public Task SendAsync(RunRequest run, CancellationToken ct = default) => channel.Writer.WriteAsync(run, ct).AsTask();

    public Task StartReceivingAsync(Func<RunRequest, Task> handler, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            await foreach (var run in channel.Reader.ReadAllAsync(ct))
            {
                await handler(run);
            }
        }, ct);
        return Task.CompletedTask;
    }
}
