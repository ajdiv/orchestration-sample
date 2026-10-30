namespace Platform.Infrastructure;

/// <summary>Files and the records set aside for the Customer. Would be in Blob Storage in Azure but using a folder locally.</summary>
public interface IFileStore
{
    Task<string> ReadTextAsync(string uri);

    /// <summary>Writes the content at a path relative to the store and returns the uri a component can read it from.</summary>
    Task<string> WriteTextAsync(string relativePath, string content);
}

public sealed class LocalFileStore(PlatformOptions options) : IFileStore
{
    private readonly string root = Path.GetFullPath(options.DataRoot);

    public Task<string> ReadTextAsync(string uri) =>
        File.ReadAllTextAsync(Path.IsPathRooted(uri) ? uri : Path.Combine(root, uri));

    public async Task<string> WriteTextAsync(string relativePath, string content)
    {
        var full = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllTextAsync(full, content);
        return full;
    }
}