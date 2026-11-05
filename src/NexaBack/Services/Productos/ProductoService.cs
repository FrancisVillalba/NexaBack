using NexaBack.Services.Categorias;
using NexaBack.Services.UnidadesMedida;
using NexaBack.Services.Marcas;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Data.Dtos.Productos;
using NexaBack.Repository.Productos;

namespace NexaBack.Services.Productos;

public sealed class ProductoService(IProductoRepository repository, IMarcaService marcas, ICategoriaService categorias, IUnidadMedidaService unidades) : IProductoService
{

    public async Task<ProductosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var (items, total) = await repository.ListarAsync(offset, size, Validation.Optional(buscar, "Búsqueda", 200), activo, ct);
        return new(items.Select(Map).ToArray(), total, pagina, size);
    }

    public async Task<ProductoDto> ObtenerAsync(int id, CancellationToken ct)
        => Map(await ObtenerEntidadAsync(id, false, ct));

    public async Task<ProductoDto> CrearAsync(GuardarProductoRequest request, CancellationToken ct)
    {
        var datos = Validar(request);
        var marca = await marcas.ValidarSeleccionAsync(datos.MarcaId, null, ct);
        var categoria = await categorias.ValidarSeleccionAsync(datos.CategoriaId, null, ct);
        var unidad = await unidades.ValidarSeleccionAsync(datos.UnidadMedidaId, null, ct);
        ValidarStockMinimo(datos.StockMinimo, unidad.Codigo);
        await ValidarDuplicadosAsync(datos, null, ct);
        var producto = new Producto { Codigo = datos.Codigo, Nombre = datos.Nombre, CategoriaId = categoria.Id, UnidadMedidaId = unidad.Id };
        Aplicar(producto, datos);
        producto.Marca = marca;
        producto.CategoriaCatalogo = categoria;
        producto.UnidadMedidaCatalogo = unidad;
        await repository.AgregarAsync(producto, ct);
        return Map(producto);
    }

    public async Task<ProductoDto> ActualizarAsync(int id, GuardarProductoRequest request, CancellationToken ct)
    {
        var datos = Validar(request);
        var producto = await ObtenerEntidadAsync(id, true, ct);
        ValidarVersion(producto, request.Version);
        var marca = await marcas.ValidarSeleccionAsync(datos.MarcaId, producto.MarcaId, ct);
        var categoria = await categorias.ValidarSeleccionAsync(datos.CategoriaId, producto.CategoriaId, ct);
        var unidad = await unidades.ValidarSeleccionAsync(datos.UnidadMedidaId, producto.UnidadMedidaId, ct);
        ValidarStockMinimo(datos.StockMinimo, unidad.Codigo);
        await ValidarDuplicadosAsync(datos, id, ct);
        if (producto.UnidadMedidaId != datos.UnidadMedidaId && await repository.TieneMovimientosAsync(id, ct))
            throw new BusinessException("No se puede cambiar la unidad de medida de un producto que ya tiene movimientos de stock.", 409);
        Aplicar(producto, datos);
        producto.Marca = marca;
        producto.CategoriaCatalogo = categoria;
        producto.UnidadMedidaCatalogo = unidad;
        producto.Version = Guid.NewGuid();
        await repository.GuardarCambiosAsync(ct);
        return Map(producto);
    }

    public async Task CambiarEstadoAsync(int id, CambiarEstadoProductoRequest request, CancellationToken ct)
    {
        var producto = await ObtenerEntidadAsync(id, true, ct);
        ValidarVersion(producto, request.Version);
        // La baja es lógica: se conserva el producto para sus futuras relaciones comerciales.
        if (producto.Activo == request.Activo) return;
        producto.Activo = request.Activo;
        producto.Version = Guid.NewGuid();
        await repository.GuardarCambiosAsync(ct);
    }

    private async Task<Producto> ObtenerEntidadAsync(int id, bool paraActualizar, CancellationToken ct)
        => await repository.ObtenerAsync(id, paraActualizar, ct) ?? throw new BusinessException("Producto no encontrado.", 404);

    private static void ValidarVersion(Producto producto, Guid? version)
    {
        if (version is null || version == Guid.Empty) throw new BusinessException("Falta la versión del producto. Actualizá el listado.");
        if (producto.Version != version) throw new BusinessException("El producto cambió mientras lo editabas. Actualizá el listado y volvé a intentar.", 409);
    }

    private async Task ValidarDuplicadosAsync(GuardarProductoRequest datos, int? id, CancellationToken ct)
    {
        if (await repository.ExisteCodigoAsync(datos.Codigo, id, ct)) throw new BusinessException("Ya existe un producto con ese código, incluso entre los inactivos.", 409);
        if (datos.CodigoBarras is not null && await repository.ExisteCodigoBarrasAsync(datos.CodigoBarras, id, ct))
            throw new BusinessException("Ya existe un producto con ese código de barras.", 409);
    }

    private static GuardarProductoRequest Validar(GuardarProductoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Costo < 0 || request.Costo > 999999999999.99m || decimal.Round(request.Costo, 2) != request.Costo)
            throw new BusinessException("El costo debe ser mayor o igual a cero y admite hasta 12 enteros y 2 decimales.");
        if (request.PrecioVenta <= 0 || request.PrecioVenta > 999999999999.99m || decimal.Round(request.PrecioVenta, 2) != request.PrecioVenta)
            throw new BusinessException("El precio de venta debe ser mayor que cero y admite hasta 12 enteros y 2 decimales.");
        if (request.StockMinimo < 0 || request.StockMinimo > 99999999999.999m || decimal.Round(request.StockMinimo, 3) != request.StockMinimo)
            throw new BusinessException("El stock mínimo debe ser mayor o igual a cero y admite hasta 11 enteros y 3 decimales.");
        return request with
        {
            Codigo = Validation.Required(request.Codigo, "Código", 50).ToUpperInvariant(),
            Nombre = Validation.Required(request.Nombre, "Nombre", 150),
            Descripcion = Validation.Optional(request.Descripcion, "Descripción", 1000),
            CodigoBarras = Validation.Optional(request.CodigoBarras, "Código de barras", 80)
        };
    }

    private static void ValidarStockMinimo(decimal minimo, string unidad)
    {
        if (unidad is not ("metro" or "kilo") && decimal.Truncate(minimo) != minimo)
            throw new BusinessException("El stock mínimo debe ser entero para esta unidad de medida.");
    }

    private static void Aplicar(Producto producto, GuardarProductoRequest datos)
    {
        producto.Codigo = datos.Codigo;
        producto.Nombre = datos.Nombre;
        producto.Descripcion = datos.Descripcion;
        producto.CodigoBarras = datos.CodigoBarras;
        producto.MarcaId = datos.MarcaId;
        producto.CategoriaId = datos.CategoriaId;
        producto.UnidadMedidaId = datos.UnidadMedidaId;
        producto.Costo = datos.Costo;
        producto.PrecioVenta = datos.PrecioVenta;
        producto.StockMinimo = datos.StockMinimo;
    }

    private static ProductoDto Map(Producto x) => new(x.Id, x.Codigo, x.Nombre, x.Descripcion, x.CodigoBarras,
        x.MarcaId, x.Marca?.Nombre, x.CategoriaId, x.CategoriaCatalogo.Nombre, x.UnidadMedidaId, x.UnidadMedidaCatalogo.Codigo, x.Costo, x.PrecioVenta, x.StockMinimo, x.Activo, x.Version,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor);
}
