namespace ImageProcessingService.Api.Models;

public sealed class ImageAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid? OriginalImageId { get; set; }

    public ImageAsset? OriginalImage { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public bool IsTransformed { get; set; }

    public string? TransformationSignature { get; set; }

    public string? TransformationSummary { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
