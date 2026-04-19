namespace ImageProcessingService.Api.Contracts.Images;

public sealed class WatermarkOptionsDto
{
    public string Text { get; set; } = string.Empty;

    public int Padding { get; set; } = 16;
}
