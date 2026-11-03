namespace NexaBack.Infrastructure.Data.Dtos.Comercial;

public sealed record ContactoComercialDto(int Id, string NombreRazonSocial, string? TipoDocumento, string Documento, string? Correo,
    string? Telefono, string? Direccion, string? Observaciones, int PlazoPagoDias, decimal LimiteCredito,
    bool Activo, Guid Version, DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor);
public sealed record ContactosPaginaDto(IReadOnlyList<ContactoComercialDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record GuardarContactoRequest(string NombreRazonSocial, string TipoDocumento, string Documento, string? Correo,
    string? Telefono, string? Direccion, string? Observaciones, int PlazoPagoDias, decimal LimiteCredito, Guid? Version = null);
public sealed record CambiarEstadoContactoRequest
{
    public required bool Activo { get; init; }
    public required Guid Version { get; init; }
}
