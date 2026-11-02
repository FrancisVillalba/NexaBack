using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Data.Dtos.Productos;
using NexaBack.Repository.Productos;

namespace NexaBack.Services.Productos;

public sealed class ProductoService(IProductoRepository repository) : IProductoService
{
    private static readonly string[] Unidades = ["unidad", "caja", "metro", "kilo", "bolsa", "par"];

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
        await ValidarDuplicadosAsync(datos, null, ct);
        var producto = new Producto { Codigo = datos.Codigo, Nombre = datos.Nombre, Categoria = datos.Categoria, UnidadMedida = datos.UnidadMedida };
        Aplicar(producto, datos);
        await repository.AgregarAsync(producto, ct);
        return Map(producto);
    }

    public async Task<ProductoDto> ActualizarAsync(int id, GuardarProductoRequest request, CancellationToken ct)
    {
        var datos = Validar(request);
        var producto = await ObtenerEntidadAsync(id, true, ct);
        ValidarVersion(producto, request.Version);
        await ValidarDuplicadosAsync(datos, id, ct);
        if (producto.UnidadMedida != datos.UnidadMedida && await repository.TieneMovimientosAsync(id, ct))
            throw new BusinessException("No se puede cambiar la unidad de medida de un producto que ya tiene movimientos de stock.", 409);
        Aplicar(producto, datos);
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
        var unidad = Validation.Required(request.UnidadMedida, "Unidad de medida", 20).ToLowerInvariant();
        if (!Unidades.Contains(unidad)) throw new BusinessException("La unidad de medida no es válida.");
        if (request.Costo < 0 || request.Costo > 999999999999.99m || decimal.Round(request.Costo, 2) != request.Costo)
            throw new BusinessException("El costo debe ser mayor o igual a cero y admite hasta 12 enteros y 2 decimales.");
        if (request.PrecioVenta <= 0 || request.PrecioVenta > 999999999999.99m || decimal.Round(request.PrecioVenta, 2) != request.PrecioVenta)
            throw new BusinessException("El precio de venta debe ser mayor que cero y admite hasta 12 enteros y 2 decimales.");
        if (request.StockMinimo < 0 || request.StockMinimo > 99999999999.999m || decimal.Round(request.StockMinimo, 3) != request.StockMinimo)
            throw new BusinessException("El stock mínimo debe ser mayor o igual a cero y admite hasta 11 enteros y 3 decimales.");
        if (unidad is not ("metro" or "kilo") && decimal.Truncate(request.StockMinimo) != request.StockMinimo)
            throw new BusinessException("El stock mínimo debe ser entero para esta unidad de medida.");
        return request with
        {
            Codigo = Validation.Required(request.Codigo, "Código", 50).ToUpperInvariant(),
            Nombre = Validation.Required(request.Nombre, "Nombre", 150),
            Descripcion = Validation.Optional(request.Descripcion, "Descripción", 1000),
            CodigoBarras = Validation.Optional(request.CodigoBarras, "Código de barras", 80),
            Marca = Validation.Optional(request.Marca, "Marca", 100),
            Categoria = Validation.Required(request.Categoria, "Categoría", 100),
            UnidadMedida = unidad
        };
    }

    private static void Aplicar(Producto producto, GuardarProductoRequest datos)
    {
        producto.Codigo = datos.Codigo;
        producto.Nombre = datos.Nombre;
        producto.Descripcion = datos.Descripcion;
        producto.CodigoBarras = datos.CodigoBarras;
        producto.Marca = datos.Marca;
        producto.Categoria = datos.Categoria;
        producto.UnidadMedida = datos.UnidadMedida;
        producto.Costo = datos.Costo;
        producto.PrecioVenta = datos.PrecioVenta;
        producto.StockMinimo = datos.StockMinimo;
    }

    private static ProductoDto Map(Producto x) => new(x.Id, x.Codigo, x.Nombre, x.Descripcion, x.CodigoBarras,
        x.Marca, x.Categoria, x.UnidadMedida, x.Costo, x.PrecioVenta, x.StockMinimo, x.Activo, x.Version,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor);
}
