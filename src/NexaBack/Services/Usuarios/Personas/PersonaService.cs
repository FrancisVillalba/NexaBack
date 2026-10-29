using NexaBack.Repository.Usuarios.Personas;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Personas;

namespace NexaBack.Services.Usuarios.Personas;

public sealed class PersonaService(IPersonaRepository repository) : IPersonaService
{
    public async Task<IReadOnlyList<PersonaDto>> ListarAsync(
        int pagina = 1, int tamanoPagina = 20, CancellationToken cancellationToken = default)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var entities = await repository.ListarAsync(offset, size, cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<PersonaDto> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.ObtenerAsync(id, cancellationToken)
            ?? throw new BusinessException("Persona no encontrado.", 404);
        return Map(entity);
    }

    public async Task<PersonaDto> CrearAsync(CrearPersonaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var nombres = Validation.Required(request.Nombres, "Nombres", 100);
        var apellidos = Validation.Required(request.Apellidos, "Apellidos", 100);
        var documento = Validation.Optional(request.Documento, "Documento", 30);
        var correo = Validation.Email(request.Correo);
        var telefono = Validation.Optional(request.Telefono, "Teléfono", 30);
        var direccion = Validation.Optional(request.Direccion, "Dirección", 300);
        if (documento is not null && await repository.ExisteDocumentoAsync(documento, cancellationToken))
            throw new BusinessException("Ya existe una persona con ese documento.", 409);
        var entity = new Persona
        {
            Nombres = nombres, Apellidos = apellidos, Documento = documento,
            Correo = correo, Telefono = telefono, Direccion = direccion
        };
        await repository.AgregarAsync(entity, cancellationToken);
        return Map(entity);
    }

    private static PersonaDto Map(Persona x) => new(x.Id, x.Nombres, x.Apellidos, x.Documento, x.Correo, x.Telefono, x.Direccion, x.Activo, x.CreadoEn);
}
