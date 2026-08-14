using System.Security.Claims;
using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Dtos;
using bcnofficechallengeapi.Models;
using bcnofficechallengeapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(AppDbContext db, ParticipantTokenService tokenService) : ControllerBase
{
    private const string RequiredEmailDomain = "@betterask.erni";

    [HttpPost("register")]
    public async Task<ActionResult<AuthenticatedUserResponse>> Register(CreateUserRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (!HasRequiredDomain(email))
            return BadRequest(new { error = $"Email must use the {RequiredEmailDomain} domain." });

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { error = "Email already registered." });

        var user = new User
        {
            Name = request.FullName.Trim(),
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Points = 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMe), CreateAuthenticatedResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUserResponse>> Login(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (!HasRequiredDomain(email))
            return Unauthorized(new { error = "Invalid email or password." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(CreateAuthenticatedResponse(user));
    }

    [Authorize(AuthenticationSchemes = "ParticipantJwt")]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var user = await GetAuthenticatedUserAsync();
        return user is null ? Unauthorized() : Ok(ToResponse(user));
    }

    [Authorize(AuthenticationSchemes = "ParticipantJwt")]
    [HttpGet("me/points")]
    public async Task<IActionResult> GetMyPoints()
    {
        var user = await GetAuthenticatedUserAsync();
        if (user is null)
            return Unauthorized();

        return Ok(new
        {
            userId = user.Id,
            points = user.Points,
            pointsTimestamp = user.PointsTimestamp
        });
    }

    [Authorize(AuthenticationSchemes = "ParticipantJwt")]
    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateUserRequest request)
    {
        var user = await GetAuthenticatedUserAsync();
        if (user is null)
            return Unauthorized();

        if (request.FullName is not null)
            user.Name = request.FullName.Trim();

        if (request.Email is not null)
        {
            var email = NormalizeEmail(request.Email);
            if (!HasRequiredDomain(email))
                return BadRequest(new { error = $"Email must use the {RequiredEmailDomain} domain." });
            if (await db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                return Conflict(new { error = "Email already in use." });
            user.Email = email;
        }

        if (request.Password is not null)
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        if (request.CompanyName is not null)
            user.CompanyName = request.CompanyName.Trim();
        if (request.JobTitle is not null)
            user.JobTitle = request.JobTitle.Trim();

        await db.SaveChangesAsync();
        return Ok(ToResponse(user));
    }

    private async Task<User?> GetAuthenticatedUserAsync()
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? await db.Users.FindAsync(userId) : null;
    }

    private AuthenticatedUserResponse CreateAuthenticatedResponse(User user)
    {
        var token = tokenService.Create(user.Id, user.Email);
        return new AuthenticatedUserResponse
        {
            User = ToResponse(user),
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool HasRequiredDomain(string email) =>
        email.Length > RequiredEmailDomain.Length &&
        email.EndsWith(RequiredEmailDomain, StringComparison.OrdinalIgnoreCase) &&
        email[..^RequiredEmailDomain.Length].All(character =>
            char.IsLetterOrDigit(character) || character is '.' or '_' or '%' or '+' or '-');

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.Name,
        Email = user.Email,
        CompanyName = user.CompanyName,
        JobTitle = user.JobTitle,
        Points = user.Points,
        PointsTimestamp = user.PointsTimestamp
    };
}
