namespace NexaBack.Infrastructure.Data.Dtos.Inventario;

public sealed record DepositoAdministracionDto(int Id, string Nombre, string? Descripcion, bool Activo, Guid Version,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor);
public sealed record DepositosPaginaDto(IReadOnlyList<DepositoAdministracionDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record GuardarDepositoRequest(string Nombre, string? Descripcion, Guid? Version = null);
public sealed record CambiarEstadoDepositoRequest
{
    public required bool Activo { get; init; }
    public required Guid Version { get; init; }
}
