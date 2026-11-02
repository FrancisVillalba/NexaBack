namespace NexaBack.Infrastructure.Data.Dtos.Perfiles;

public sealed record CrearPerfilRequest(string Nombre, string? Descripcion = null);
public sealed record ActualizarPerfilRequest(string Nombre, string? Descripcion = null);
public sealed record CambiarEstadoPerfilRequest
{
    // Un cuerpo vacío debe devolver 400, no interpretarse como una baja.
    public required bool Activo { get; init; }
}
public sealed record PerfilDto(int Id, string Nombre, string? Descripcion, bool Activo, DateTime CreadoEn);
