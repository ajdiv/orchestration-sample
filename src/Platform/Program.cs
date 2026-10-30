using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;
using Platform;
using Platform.Components;
using Platform.Infrastructure;
using Platform.Queue;
using Platform.Triggers;
using Platform.Workflows;

var builder = WebApplication.CreateBuilder(args);

var options = builder.Configuration.GetSection("Platform").Get<PlatformOptions>() ?? new PlatformOptions();
builder.Services.AddSingleton(options);

// I used docker run run emulate durable scheduler locally
var scheduler = "Endpoint=http://localhost:8080;TaskHub=default;Authentication=None";

// The worker hosts the orchestration and the .NET components (activities) in this process.
builder.Services.AddDurableTaskWorker(worker =>
{
    worker.AddTasks(tasks =>
    {
        tasks.AddOrchestrator<BankStatementImport>();
        tasks.AddActivity<ParseStatement>();
        tasks.AddActivity<StartContainerJob>();
        tasks.AddActivity<PostTransactions>();
        tasks.AddActivity<Notify>();
    });
    worker.UseDurableTaskScheduler(scheduler);
});

// The client schedules runs (from the queue consumer) and raises events (when a scripted component finishes).
builder.Services.AddDurableTaskClient(client => client.UseDurableTaskScheduler(scheduler));

builder.Services.AddSingleton<IRunQueue, LocalRunQueue>();
builder.Services.AddSingleton<IFileStore, LocalFileStore>();
builder.Services.AddSingleton<IContainerJobs, LocalProcessJobs>();
builder.Services.AddSingleton<ITransactionStore, InMemoryTransactionStore>();
builder.Services.AddHostedService<ScheduleTrigger>();        // demo schedule; in Azure a timer sends the same message

builder.Services.AddHostedService<RunQueueConsumer>();

var app = builder.Build();

app.MapGet("/", () => "Orchestration sample is running. Dashboard for the emulator: http://localhost:8082");

app.Run();
