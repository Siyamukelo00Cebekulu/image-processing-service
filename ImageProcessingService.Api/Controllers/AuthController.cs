using ImageProcessingService.Api.Contracts.Auth;
using ImageProcessingService.Api.Data;
using ImageProcessingService.Api.Models;
using ImageProcessingService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessingService.Api.Controllers;

[ApiController]
[Route("")]
public sealed class AuthController(
    ApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(user => user.Username == request.Username, cancellationToken))
        {
            return Conflict(new { message = "Username is already registered." });
        }

        var user = new User
        {
            Username = request.Username.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(jwtTokenService.CreateToken(user));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim();
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Username == normalizedUsername, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(jwtTokenService.CreateToken(user));
    }
}
