# Orchestration sample

Part 3 architecture demo.A run becomes one Service Bus message, the message becomes one Durable Task orchestration, and the orchestration calls a combination of compiled .NET components and a scripted component in a container, then posts transactions idempotently.

## What's where

| Path | What it is |
|---|---|
| `src/Platform/Workflows/BankStatementImport.cs` | The orchestrator. Holds the state. |
| `src/Platform/Components/ParseStatement.cs` | .NET component. Sets aside records that fail validation. |
| `src/Platform/Components/PostTransactions.cs` | .NET component with side effects BUT is written to be idempotent. |
| `src/Platform/Components/StartContainerJob.cs` | The one generic activity every scripted component goes through. |
| `src/Platform/Components/Notify.cs` | Once-per-run notification (logs for this demo but would go to Event Grid and email in Azure). |
| `src/Platform/Queue/` | `IRunQueue` with Service Bus and local implementations, plus the consumer. |
| `src/Platform/Triggers/ScheduleTrigger.cs` | Demo schedule fires twice (duplicate dropped), then a Customer rerun. |
| `src/Platform/Infrastructure/` | File store (folder for local demo), container jobs (local process for demo), transaction store (in-memory for demo). |
| `components/enrich-fx-rates/` | Example scripted component containing `main.py`, its `manifest.json`, and a `Dockerfile`. |
| `data/incoming/statement.csv` | Sample bank statement with two bad rows. |

## Run it locally

Prerequisites: .NET 8 SDK, Docker, Python 3.

1. Start the Durable Task Scheduler emulator (gRPC on 8080, dashboard on 8082), first time pull the image of the emulator from Microsoft. 

   ```
   docker run -d --name dts -p 8080:8080 -p 8082:8082 mcr.microsoft.com/dts/dts-emulator:latest
   ```
   
   Subsequent runs can just run:
   
   ```
   docker start dts
   ```
   
2. From this folder (the one with the `.sln`), run the worker. The relative paths in `appsettings.json` assume the working directory is `src/Platform`, which is what `dotnet run --project` uses:

   ```
   dotnet run --project src/Platform
   ```

3. Watch the log. You should see, in order:

   - `Schedule fired` and `Schedule fired again by mistake`, then `already exists ... dropping the duplicate message`
   - the Python job exit with `enriched 4 rows`
   - `Run ... finished: 4 transactions posted, 2 records set aside for the Customer`
   - about 30 seconds later, the rerun under a new RunId finishing with `0 transactions posted` because every transaction already exists

4. Open http://localhost:8082 to see each orchestration's history step by step. The parsed, rejected, and enriched files land under `data/runs/<RunId>/`.