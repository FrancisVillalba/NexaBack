namespace NexaBack.Infrastructure.Data.Dtos.Categorias;

public sealed record CategoriaAdministracionDto(int Id, string Nombre, string? Descripcion, bool Activo, Guid Version,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor);
public sealed record CategoriasPaginaDto(IReadOnlyList<CategoriaAdministracionDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record GuardarCategoriaRequest(string Nombre, string? Descripcion, Guid? Version = null);
public sealed record CambiarEstadoCategoriaRequest
{
    public required bool Activo { get; init; }
    public required Guid Version { get; init; }
}

public sealed record CategoriaOpcionDto(int Id, string Nombre, bool Activo);
public sealed record CategoriasOpcionesPaginaDto(IReadOnlyList<CategoriaOpcionDto> Items, int Total, int Pagina, int TamanoPagina);
