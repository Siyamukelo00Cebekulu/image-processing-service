namespace ImageProcessingService.Api.Services.ImageProcessing;

public sealed class ProcessedImageResult
{
    public required MemoryStream Content { get; init; }

    public required string ContentType { get; init; }

    public required string Extension { get; init; }

    public required string Format { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required long SizeBytes { get; init; }

    public required string TransformationSignature { get; init; }

    public required string TransformationSummary { get; init; }
}
