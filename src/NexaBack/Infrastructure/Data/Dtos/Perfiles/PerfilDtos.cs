namespace NexaBack.Infrastructure.Data.Dtos.Perfiles;

public sealed record CrearPerfilRequest(string Nombre, string? Descripcion = null);
public sealed record PerfilDto(int Id, string Nombre, string? Descripcion, bool Activo, DateTime CreadoEn);
