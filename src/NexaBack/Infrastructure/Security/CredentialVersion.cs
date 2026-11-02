using System.Security.Cryptography;
using System.Text;

namespace NexaBack.Infrastructure.Security;

internal static class CredentialVersion
{
    // El JWT lleva una huella del hash, nunca la contraseña ni PasswordHash.
    // Al cambiar la contraseña, la huella cambia e invalida las sesiones anteriores sin agregar columnas.
    public static string FromHash(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
}
