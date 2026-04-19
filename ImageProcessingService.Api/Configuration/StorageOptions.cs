namespace ImageProcessingService.Api.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "Storage";
}
