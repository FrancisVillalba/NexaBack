using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexaBack.Infrastructure.Data.Dtos.Usuarios;

namespace NexaBack.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> settings, TimeProvider clock) : ITokenService
{
    public LoginResponse Crear(UsuarioSesionDto usuario)
    {
        var options = settings.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Convert.ToHexString(RandomNumberGenerator.GetBytes(16))),
            new("usuarioId", usuario.UsuarioId.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer32),
            new("nombreUsuario", usuario.NombreUsuario),
            new("nombre", usuario.Nombre),
            new("apellido", usuario.Apellido),
            new("perfilId", usuario.PerfilId.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer32),
            new("perfil", usuario.Perfil),
            new("role", usuario.Perfil)
        };
        if (usuario.Documento is not null) claims.Add(new Claim("documento", usuario.Documento));

        var key = new SymmetricSecurityKey(Convert.FromBase64String(options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        token.Payload[JwtRegisteredClaimNames.Iat] = now.ToUnixTimeSeconds();

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expires, usuario);
    }
}
