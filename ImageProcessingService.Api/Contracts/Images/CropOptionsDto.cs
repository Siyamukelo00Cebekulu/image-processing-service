namespace ImageProcessingService.Api.Contracts.Images;

public sealed class CropOptionsDto
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int X { get; set; }

    public int Y { get; set; }
}
