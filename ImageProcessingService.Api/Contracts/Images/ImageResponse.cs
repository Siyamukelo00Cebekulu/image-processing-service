namespace ImageProcessingService.Api.Contracts.Images;

public sealed class ImageResponse
{
    public Guid Id { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public string Format { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public bool IsTransformed { get; init; }

    public Guid? OriginalImageId { get; init; }

    public string? TransformationSummary { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
