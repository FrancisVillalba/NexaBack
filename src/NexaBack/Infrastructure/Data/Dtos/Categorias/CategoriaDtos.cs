namespace NexaBack.Infrastructure.Data.Dtos.Categorias;
public sealed record CategoriaOpcionDto(int Id, string Nombre, bool Activo);
public sealed record CategoriasOpcionesPaginaDto(IReadOnlyList<CategoriaOpcionDto> Items, int Total, int Pagina, int TamanoPagina);
