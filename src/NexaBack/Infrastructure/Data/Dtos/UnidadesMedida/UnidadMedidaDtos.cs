namespace NexaBack.Infrastructure.Data.Dtos.UnidadesMedida;
public sealed record UnidadMedidaOpcionDto(int Id, string Nombre, bool Activo, string Codigo, bool PermiteFracciones);
public sealed record UnidadesMedidaOpcionesPaginaDto(IReadOnlyList<UnidadMedidaOpcionDto> Items, int Total, int Pagina, int TamanoPagina);
