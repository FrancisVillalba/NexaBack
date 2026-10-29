namespace NexaBack.Infrastructure.Data.Dtos.Personas;

public sealed record CrearPersonaRequest(string Nombres, string Apellidos, string? Documento = null, string? Correo = null,
    string? Telefono = null, string? Direccion = null);
public sealed record PersonaDto(int Id, string Nombres, string Apellidos, string? Documento, string? Correo,
    string? Telefono, string? Direccion, bool Activo, DateTime CreadoEn);
