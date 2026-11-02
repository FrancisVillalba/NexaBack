using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

namespace NexaBack.Repository.Usuarios.Personas;

public interface IPersonaRepository
{
    Task<IReadOnlyList<Persona>> ListarAsync(int offset, int limit, string? buscar = null, bool? activo = null, CancellationToken cancellationToken = default);
    Task<Persona?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<Persona?> ObtenerParaActualizarAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteDocumentoAsync(string value, CancellationToken cancellationToken = default, int? excluirId = null);
    Task AgregarAsync(Persona entity, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
    Task<CambioEstadoPersona> CambiarEstadoAsync(int id, bool activo, CancellationToken cancellationToken = default);
}

// El repositorio comunica el resultado; el servicio decide el mensaje HTTP que corresponde.
public enum CambioEstadoPersona { Actualizada, NoEncontrada, UltimoAdministrador }

