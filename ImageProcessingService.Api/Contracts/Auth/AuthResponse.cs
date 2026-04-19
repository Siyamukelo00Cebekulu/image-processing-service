namespace ImageProcessingService.Api.Contracts.Auth;

public sealed class AuthResponse
{
    public Guid Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Token { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }
}
