using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Data.Dtos.Auditoria;

namespace NexaBack.Infrastructure.Data.Context.Usuarios;

public static class UsuariosAuditoriaNombres
{
    public static async Task<T> ConNombresAsync<T>(this UsuariosDbContext context, T dto, CancellationToken ct)
        where T : IAuditoriaVisible
    {
        await context.CompletarNombresAsync([dto], ct);
        return dto;
    }

    public static async Task CompletarNombresAsync(this UsuariosDbContext context, IEnumerable<IAuditoriaVisible> registros, CancellationToken ct)
    {
        var filas = registros.ToArray();
        var ids = filas.SelectMany(x => new[] { x.CreadoPor, x.ModificadoPor, x.AprobadoPor })
            .OfType<int>().Distinct().ToArray();
        if (ids.Length == 0) return;

        // Una sola consulta por respuesta, limitada a los autores de los registros visibles.
        var usuarios = await context.Usuarios.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Persona.Nombres, x.Persona.Apellidos }).ToListAsync(ct);
        var nombres = usuarios.ToDictionary(x => x.Id, x => string.Join(" ", new[] { Primero(x.Nombres), Primero(x.Apellidos) }
            .Where(parte => parte.Length > 0)));

        string? Nombre(int? id) => id.HasValue && nombres.TryGetValue(id.Value, out var nombre) && nombre.Length > 0 ? nombre : null;
        foreach (var fila in filas)
            fila.NombresAuditoria = new(Nombre(fila.CreadoPor), Nombre(fila.ModificadoPor), Nombre(fila.AprobadoPor));
    }

    private static string Primero(string texto) => texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
}
