using ImageProcessingService.Api.Contracts.Images;

namespace ImageProcessingService.Api.Services.ImageProcessing;

public interface IImageProcessor
{
    Task<ImageValidationResult> ValidateUploadAsync(IFormFile file, CancellationToken cancellationToken);

    Task<ProcessedImageResult> TransformAsync(Stream source, TransformationRequest request, CancellationToken cancellationToken);
}
