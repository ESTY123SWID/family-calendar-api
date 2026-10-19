using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using my_project.Core.Entities;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Core.Models;
using my_project.Service.Options;

namespace my_project.Service.Security;

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtSettings> settings, IClock clock)
    {
        _settings = settings.Value;
        _clock = clock;

        var keyBytes = Encoding.UTF8.GetBytes(_settings.Key);
        if (keyBytes.Length < JwtSettings.MinKeyBytes)
        {
            // Never issue weak tokens (the API also validates the settings when the server starts)
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {JwtSettings.MinKeyBytes} bytes. Set it in User Secrets.");
        }

        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateToken(User user)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
