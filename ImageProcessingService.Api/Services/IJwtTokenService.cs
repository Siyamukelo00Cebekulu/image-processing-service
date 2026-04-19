using ImageProcessingService.Api.Contracts.Auth;
using ImageProcessingService.Api.Models;

namespace ImageProcessingService.Api.Services;

public interface IJwtTokenService
{
    AuthResponse CreateToken(User user);
}
