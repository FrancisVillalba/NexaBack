using System.ComponentModel.DataAnnotations;

namespace NexaBack.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required] public string SigningKey { get; set; } = string.Empty;
    [Range(1, 60)] public int ExpirationMinutes { get; set; } = 15;

    public static bool HasValidKey(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey)) return false;
        Span<byte> buffer = new byte[options.SigningKey.Length];
        return Convert.TryFromBase64String(options.SigningKey, buffer, out var count) && count >= 32;
    }
}
