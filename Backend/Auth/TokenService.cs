using System.Security.Claims;
using System.Text;
using Backend.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Auth;

public record AuthToken(string AccessToken, DateTime ExpiresAt);

public interface ITokenService
{
    AuthToken CreateToken(User user);
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _credentials;
    private readonly TimeProvider _time;

    public TokenService(IOptions<JwtSettings> options, TimeProvider time)
    {
        _settings = options.Value;
        _credentials = new SigningCredentials(CreateSigningKey(_settings), SecurityAlgorithms.HmacSha256);
        _time = time;
    }

    public static SymmetricSecurityKey CreateSigningKey(JwtSettings settings) =>
        new(Encoding.UTF8.GetBytes(settings.Key));

    public AuthToken CreateToken(User user)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Username),
                new Claim(AuthConstants.RoleClaim, user.Role)
            ])
        };

        return new AuthToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
