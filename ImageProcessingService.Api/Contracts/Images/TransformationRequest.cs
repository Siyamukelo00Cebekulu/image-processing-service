namespace ImageProcessingService.Api.Contracts.Images;

public sealed class TransformationRequest
{
    public ResizeOptionsDto? Resize { get; set; }

    public CropOptionsDto? Crop { get; set; }

    public float? Rotate { get; set; }

    public WatermarkOptionsDto? Watermark { get; set; }

    public bool Flip { get; set; }

    public bool Mirror { get; set; }

    public int? Quality { get; set; }

    public string? Format { get; set; }

    public FilterOptionsDto? Filters { get; set; }
}
