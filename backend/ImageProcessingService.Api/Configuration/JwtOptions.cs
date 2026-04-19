namespace ImageProcessingService.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ImageProcessingService";

    public string Audience { get; set; } = "ImageProcessingService.Client";

    public string SecretKey { get; set; } = "change-this-secret-key-in-production-to-a-long-random-value";

    public int ExpirationMinutes { get; set; } = 120;
}
