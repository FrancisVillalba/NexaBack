using NexaBack.Infrastructure.Data.Context.Marcas.Entity;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Productos.Entity;

public sealed class Producto : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? CodigoBarras { get; set; }
    public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }
    public int CategoriaId { get; set; }
    public Categorias.Entity.Categoria CategoriaCatalogo { get; set; } = null!;
    public int UnidadMedidaId { get; set; }
    public UnidadesMedida.Entity.UnidadMedida UnidadMedidaCatalogo { get; set; } = null!;
    public decimal Costo { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; } = true;
    // Evita que una edición sobrescriba otra modificación realizada mientras el formulario estaba abierto.
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
