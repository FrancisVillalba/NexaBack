namespace NexaBack.Infrastructure.Data.Dtos.Personas;

public sealed record CrearPersonaRequest(string Nombres, string Apellidos, int TipoDocumentoId, string Documento, string? Correo = null,
    string? Telefono = null, string? Direccion = null);

// La edición no recibe Id, Activo ni CreadoEn: cada operación modifica solo sus propios campos.
public sealed record ActualizarPersonaRequest(string Nombres, string Apellidos, int TipoDocumentoId, string Documento, string? Correo = null,
    string? Telefono = null, string? Direccion = null);

public sealed record CambiarEstadoPersonaRequest
{
    // required obliga a enviar el estado; un JSON vacío no debe desactivar una persona.
    public required bool Activo { get; init; }
}

public sealed record PersonaDto(int Id, string Nombres, string Apellidos, int? TipoDocumentoId, string? TipoDocumento, string? Documento, string? Correo,
    string? Telefono, string? Direccion, bool Activo, DateTime CreadoEn);
