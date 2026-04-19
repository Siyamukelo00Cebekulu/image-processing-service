using ImageProcessingService.Api.Contracts.Images;
using ImageProcessingService.Api.Models;

namespace ImageProcessingService.Api.Extensions;

public static class ImageAssetMappingExtensions
{
    public static ImageResponse ToResponse(this ImageAsset image) =>
        new()
        {
            Id = image.Id,
            FileName = image.FileName,
            Url = $"/images/{image.Id}",
            ContentType = image.ContentType,
            Format = image.Format,
            SizeBytes = image.SizeBytes,
            Width = image.Width,
            Height = image.Height,
            IsTransformed = image.IsTransformed,
            OriginalImageId = image.OriginalImageId,
            TransformationSummary = image.TransformationSummary,
            CreatedAtUtc = image.CreatedAtUtc
        };
}
