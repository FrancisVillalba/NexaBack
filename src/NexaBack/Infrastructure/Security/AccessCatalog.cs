namespace NexaBack.Infrastructure.Security;

public sealed record AccessDefinition(string Codigo, string Modulo, string Accion, string Nombre);

public static class AccessCatalog
{
    // El catálogo describe operaciones reales del sistema. La pantalla asigna estos códigos,
    // no inventa permisos que ningún endpoint o botón pueda interpretar.
    public static readonly IReadOnlyList<AccessDefinition> Definitions = Build();
    public static readonly string[] OperatorModules = ["clientes", "proveedores", "productos", "inventario", "compras", "ventas", "presupuestos"];
    public static bool IsAdministrative(string module) => module is "personas" or "usuarios" or "perfiles" or "permisos" or "configuracion";

    private static IReadOnlyList<AccessDefinition> Build()
    {
        var result = new List<AccessDefinition>();
        void Add(string module, params string[] actions)
        {
            foreach (var action in actions) result.Add(new($"{module}.{action}", module, action, action switch
            {
                "ver" => "Ver", "crear" => "Crear", "editar" => "Editar", "estado" => "Activar / desactivar",
                "password" => "Cambiar contraseñas", "recibir" => "Recibir mercadería", "anular" => "Anular ventas",
                "cobrar" => "Registrar cobros", "eliminar" => "Eliminar", "convertir" => "Convertir en venta",
                "cuenta_corriente" => "Consultar cuenta corriente", "exportar" => "Exportar",
                "restablecer" => "Restablecer datos", _ => action
            }));
        }
        Add("inicio", "ver"); Add("productos", "ver", "crear", "editar"); Add("inventario", "ver");
        Add("movimientos", "ver", "crear"); Add("compras", "ver", "crear", "recibir");
        Add("ventas", "ver", "crear", "anular", "cobrar"); Add("presupuestos", "ver", "crear", "eliminar", "convertir");
        Add("clientes", "ver", "crear", "editar", "cuenta_corriente"); Add("proveedores", "ver", "crear", "editar");
        Add("reportes", "ver", "exportar"); Add("configuracion", "ver", "editar", "restablecer");
        Add("personas", "ver", "crear", "editar", "estado"); Add("usuarios", "ver", "crear", "editar", "estado", "password");
        Add("perfiles", "ver", "crear", "editar", "estado"); Add("permisos", "ver", "editar");
        return result;
    }
}
