namespace NexaBack.Infrastructure.Data.Dtos.Marcas;

public sealed record MarcaAdministracionDto(int Id, string Nombre, string? Descripcion, bool Activo, Guid Version,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor);
public sealed record MarcasPaginaDto(IReadOnlyList<MarcaAdministracionDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record GuardarMarcaRequest(string Nombre, string? Descripcion, Guid? Version = null);
public sealed record CambiarEstadoMarcaRequest
{
    public required bool Activo { get; init; }
    public required Guid Version { get; init; }
}

public sealed record MarcaOpcionDto(int Id, string Nombre, bool Activo);
public sealed record MarcasOpcionesPaginaDto(IReadOnlyList<MarcaOpcionDto> Items, int Total, int Pagina, int TamanoPagina);
