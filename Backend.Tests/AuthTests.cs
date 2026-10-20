using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Backend.Dtos;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests;

public class AuthTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AuthTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    private async Task<LoginResponseDto> RegisterAndLogin()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto("teszt", email, "Jelszo123!"));
        reg.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto(email, "Jelszo123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!;
    }

    private Task<HttpResponseMessage> GetMe(string? token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        if (token != null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(req);
    }

    private static string CraftToken(string key = AuthApiFactory.Key, string issuer = AuthApiFactory.Issuer,
        string audience = AuthApiFactory.Audience, DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var exp = expires ?? now.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = exp.AddMinutes(-20),
            NotBefore = exp.AddMinutes(-20),
            Expires = exp,
            Subject = new ClaimsIdentity([new Claim("sub", "1"), new Claim("role", "Admin")]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        });
    }

    [Fact]
    public async Task Login_ReturnsValidToken_ThatAuthorizesMe()
    {
        var login = await RegisterAndLogin();
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.True(login.ExpiresAt > DateTime.UtcNow);

        var jwt = new JsonWebToken(login.AccessToken);
        Assert.Equal(AuthApiFactory.Issuer, jwt.Issuer);
        Assert.Equal("Customer", jwt.GetClaim("role").Value);

        var me = await GetMe(login.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var profile = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        var user = profile.RootElement;
        Assert.True(user.GetProperty("id").GetInt32() > 0);
        Assert.Equal("teszt", user.GetProperty("username").GetString());
        Assert.EndsWith("@example.com", user.GetProperty("email").GetString());
        Assert.Equal("Customer", user.GetProperty("role").GetString());
        Assert.True(user.GetProperty("createdAt").TryGetDateTimeOffset(out var createdAt));
        Assert.True(createdAt <= DateTimeOffset.UtcNow);
        Assert.False(user.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto("nincs@example.com", "rossz"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMe(null)).StatusCode);

    [Fact]
    public async Task Me_TamperedToken_Returns401()
    {
        var token = (await RegisterAndLogin()).AccessToken;
        var tampered = token[..^4] + (token[^4..] == "AAAA" ? "BBBB" : "AAAA");
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMe(tampered)).StatusCode);
    }

    [Fact]
    public async Task Me_WrongSigningKey_Returns401() =>
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await GetMe(CraftToken(key: "another-signing-key-that-is-long-enough-0987654321"))).StatusCode);

    [Fact]
    public async Task Me_WrongIssuer_Returns401() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMe(CraftToken(issuer: "evil"))).StatusCode);

    [Fact]
    public async Task Me_WrongAudience_Returns401() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMe(CraftToken(audience: "evil"))).StatusCode);

    [Fact]
    public async Task Me_ExpiredToken_Returns401() =>
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await GetMe(CraftToken(expires: DateTime.UtcNow.AddMinutes(-5)))).StatusCode);

    [Fact]
    public async Task Me_UnsignedAlgNoneToken_Returns401()
    {
        static string B64(string s) => Base64UrlEncoder.Encode(s);
        var token = $"{B64("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{B64($"{{\"sub\":\"1\",\"iss\":\"{AuthApiFactory.Issuer}\",\"aud\":\"{AuthApiFactory.Audience}\",\"exp\":{DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()}}}")}.";
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMe(token)).StatusCode);
    }
}
