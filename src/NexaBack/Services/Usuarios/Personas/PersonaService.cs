using NexaBack.Repository.Usuarios.Personas;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Personas;

namespace NexaBack.Services.Usuarios.Personas;

public sealed class PersonaService(IPersonaRepository repository) : IPersonaService
{
    public async Task<IReadOnlyList<PersonaDto>> ListarAsync(
        int pagina = 1, int tamanoPagina = 20, string? buscar = null, bool? activo = null, CancellationToken cancellationToken = default)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var filtro = Validation.Optional(buscar, "Búsqueda", 200);
        var entities = await repository.ListarAsync(offset, size, filtro, activo, cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<PersonaDto> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.ObtenerAsync(id, cancellationToken)
            ?? throw new BusinessException("Persona no encontrada.", 404);
        return Map(entity);
    }

    public async Task<PersonaDto> CrearAsync(CrearPersonaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var nombres = Validation.Required(request.Nombres, "Nombres", 100);
        var apellidos = Validation.Required(request.Apellidos, "Apellidos", 100);
        var documento = Validation.Required(request.Documento, "Documento", 30);
        var correo = Validation.Email(request.Correo);
        var telefono = Validation.Optional(request.Telefono, "Teléfono", 30);
        var direccion = Validation.Optional(request.Direccion, "Dirección", 300);
        if (await repository.ExisteDocumentoAsync(documento, cancellationToken))
            throw new BusinessException("Ya existe una persona con ese documento.", 409);
        var entity = new Persona
        {
            Nombres = nombres, Apellidos = apellidos, Documento = documento,
            Correo = correo, Telefono = telefono, Direccion = direccion
        };
        await repository.AgregarAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<PersonaDto> ActualizarAsync(int id, ActualizarPersonaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var persona = await repository.ObtenerParaActualizarAsync(id, cancellationToken)
            ?? throw new BusinessException("Persona no encontrada.", 404);

        // Reutilizamos las mismas reglas del alta. El Id actual se excluye de la
        // búsqueda de duplicados para poder guardar sin cambiar el documento.
        var nombres = Validation.Required(request.Nombres, "Nombres", 100);
        var apellidos = Validation.Required(request.Apellidos, "Apellidos", 100);
        var documento = Validation.Required(request.Documento, "Documento", 30);
        var correo = Validation.Email(request.Correo);
        var telefono = Validation.Optional(request.Telefono, "Teléfono", 30);
        var direccion = Validation.Optional(request.Direccion, "Dirección", 300);
        if (await repository.ExisteDocumentoAsync(documento, cancellationToken, excluirId: id))
            throw new BusinessException("Ya existe una persona con ese documento.", 409);

        persona.Nombres = nombres;
        persona.Apellidos = apellidos;
        persona.Documento = documento;
        persona.Correo = correo;
        persona.Telefono = telefono;
        persona.Direccion = direccion;
        await repository.GuardarCambiosAsync(cancellationToken);
        return Map(persona);
    }

    public async Task CambiarEstadoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var resultado = await repository.CambiarEstadoAsync(id, activo, cancellationToken);
        if (resultado == CambioEstadoPersona.NoEncontrada)
            throw new BusinessException("Persona no encontrada.", 404);
        if (resultado == CambioEstadoPersona.UltimoAdministrador)
            throw new BusinessException("No podés desactivar a la persona del último administrador activo.", 409);
    }

    private static PersonaDto Map(Persona x) => new(x.Id, x.Nombres, x.Apellidos, x.Documento, x.Correo, x.Telefono, x.Direccion, x.Activo, x.CreadoEn);
}
