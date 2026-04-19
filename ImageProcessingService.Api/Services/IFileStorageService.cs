namespace ImageProcessingService.Api.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken);

    string GetAbsolutePath(string relativePath);
}
