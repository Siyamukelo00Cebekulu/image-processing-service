using ImageProcessingService.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ImageProcessingService.Api.Services;

public sealed class LocalFileStorageService(IOptions<StorageOptions> storageOptions) : IFileStorageService
{
    private readonly string _rootPath = Path.GetFullPath(storageOptions.Value.RootPath);

    public async Task<string> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken)
    {
        var fullPath = GetAbsolutePath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var output = File.Create(fullPath);
        content.Position = 0;
        await content.CopyToAsync(output, cancellationToken);
        await output.FlushAsync(cancellationToken);
        return relativePath.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = GetAbsolutePath(relativePath);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public string GetAbsolutePath(string relativePath)
    {
        var combined = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!combined.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Attempted to access a file outside the configured storage root.");
        }

        return combined;
    }
}
