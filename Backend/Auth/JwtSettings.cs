using System.ComponentModel.DataAnnotations;

namespace Backend.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 kulcs, legalább 32 bájt (256 bit)
    [Required, MinLength(32)] public string Key { get; set; } = string.Empty;
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Range(1, 1440)] public int ExpiryMinutes { get; set; } = 60;
}
