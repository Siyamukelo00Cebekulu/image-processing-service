namespace ImageProcessingService.Api.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ImageAsset> Images { get; set; } = new List<ImageAsset>();
}
