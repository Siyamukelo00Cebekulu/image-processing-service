using System.ComponentModel.DataAnnotations;

namespace ImageProcessingService.Api.Contracts.Images;

public sealed class TransformImageRequest
{
    [Required]
    public TransformationRequest Transformations { get; set; } = new();
}
