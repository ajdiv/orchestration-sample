namespace Platform;

/// <summary>One queue message = one run. The RunId is the schedule plus the fire time so a double fire has the same id.</summary>
public record RunRequest(string RunId, string CustomerId, string Workflow, int Version, string FileUri);

public record ParsedStatement(string OutputUri, string? RejectedUri, int RejectedCount);

public record JobRequest(string Component, string Version, string InputUri);

/// <summary>What the platform raises to the waiting orchestration when a scripted component's job finishes.</summary>
public record JobResult(string JobId, string OutputUri);

public record PostInput(string CustomerId, string RunId, string InputUri);

public record PostResult(int Inserted, int Skipped);

public record Notice(string RunId, int Posted, int Rejected);

public record RunResult(int Posted, int Rejected);

public record Transaction(string BankRef, string PostedOn, string Currency, decimal Amount, decimal? AmountUsd);

public record RejectedRecord(int Line, string Raw, string Reason);

public class PlatformOptions
{
    public string DataRoot { get; set; } = "../../data";
    public string ComponentsRoot { get; set; } = "../../components";
    public string PythonExe { get; set; } = "python";
}
