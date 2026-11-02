namespace NexaBack.Infrastructure.Data.Dtos.Perfiles;

public sealed record AccesoDto(string Codigo, string Modulo, string Accion, string Nombre, bool ExclusivoAdministrador);
public sealed record PerfilAccesosDto(int PerfilId, string NombrePerfil, bool Administrador, string[] Codigos, string Version);
public sealed record GuardarAccesosRequest
{
    public required string[] Codigos { get; init; }
    // La versión evita pisar cambios guardados por otro administrador mientras se editaba.
    public required string Version { get; init; }
}
