using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace bcnofficechallengeapi.Services;

public sealed class ParticipantTokenService(IConfiguration configuration)
{
    private readonly string issuer = configuration["ParticipantJwt:Issuer"]
        ?? "bcnofficechallengeapi";
    private readonly string audience = configuration["ParticipantJwt:Audience"]
        ?? "bcnofficechallenge-front";
    private readonly string signingKey = configuration["ParticipantJwt:SigningKey"]
        ?? throw new InvalidOperationException("Missing ParticipantJwt:SigningKey configuration.");

    public AuthToken Create(Guid userId, string email)
    {
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public sealed record AuthToken(string AccessToken, DateTime ExpiresAt);
