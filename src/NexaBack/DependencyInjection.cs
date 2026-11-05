using NexaBack.Repository.Reportes;
using NexaBack.Services.Reportes;
using NexaBack.Repository.Presupuestos;
using NexaBack.Services.Presupuestos;
using NexaBack.Repository.Pagos;
using NexaBack.Services.Pagos;
using NexaBack.Repository.CuentasProveedores;
using NexaBack.Services.CuentasProveedores;
using NexaBack.Repository.Cobros;
using NexaBack.Services.Cobros;
using NexaBack.Repository.CuentasCorrientes;
using NexaBack.Services.CuentasCorrientes;
using NexaBack.Repository.Ventas;
using NexaBack.Services.Ventas;
using NexaBack.Repository.Compras;
using NexaBack.Services.Compras;
using NexaBack.Repository.Usuarios.Perfiles;
using NexaBack.Repository.Usuarios.Usuarios;
using NexaBack.Repository.Usuarios.Personas;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.AspNetCore.Identity;
using NexaBack.Services.Usuarios.Personas;
using NexaBack.Services.Usuarios.Perfiles;
using NexaBack.Services.Usuarios.Usuarios;
using NexaBack.Infrastructure.Persistence;
using NexaBack.Repository.Productos;
using NexaBack.Services.Productos;
using NexaBack.Repository.Inventario;
using NexaBack.Services.Inventario;
using NexaBack.Repository.Clientes;
using NexaBack.Repository.Proveedores;
using NexaBack.Services.Clientes;
using NexaBack.Services.Proveedores;

namespace NexaBack;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Scoped: todos los repositorios de una solicitud comparten el mismo DbContext.
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioAuditoria, UsuarioAuditoria>();
        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPersonaService, PersonaService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<IAccesoRepository, AccesoRepository>();
        services.AddScoped<IAccesoService, AccesoService>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IInventarioRepository, InventarioRepository>();
        services.AddScoped<IInventarioService, InventarioService>();
        services.AddScoped<IDepositoRepository, DepositoRepository>();
        services.AddScoped<IDepositoService, DepositoService>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<ICompraRepository, CompraRepository>();
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<ICobroRepository, CobroRepository>();
        services.AddScoped<ICobroService, CobroService>();
        services.AddScoped<ICuentaCorrienteRepository, CuentaCorrienteRepository>();
        services.AddScoped<ICuentaCorrienteService, CuentaCorrienteService>();
        services.AddScoped<IPagoRepository, PagoRepository>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<ICuentaProveedorRepository, CuentaProveedorRepository>();
        services.AddScoped<ICuentaProveedorService, CuentaProveedorService>();
        services.AddScoped<IPresupuestoRepository, PresupuestoRepository>();
        services.AddScoped<IPresupuestoService, PresupuestoService>();
        services.AddScoped<IReportesRepository, ReportesRepository>();
        services.AddScoped<IReportesService, ReportesService>();
        return services;
    }
}
