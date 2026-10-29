using NexaBack.Repository.Usuarios.Perfiles;
using NexaBack.Repository.Usuarios.Usuarios;
using NexaBack.Repository.Usuarios.Personas;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.AspNetCore.Identity;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Usuarios;
using NexaBack.Infrastructure.Security;
using System.Security.Cryptography;

namespace NexaBack.Services.Usuarios.Usuarios;

public sealed class UsuarioService(IUsuarioRepository repository,
    IPersonaRepository personas,
    IPerfilRepository perfiles,
    IPasswordHasher<Usuario> passwordHasher,
    ITokenService tokens) : IUsuarioService
{
    private static readonly Usuario DummyUser = CreateDummyUser();

    private static Usuario CreateDummyUser()
    {
        var user = new Usuario { NombreUsuario = "dummy", PasswordHash = string.Empty };
        user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        return user;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || request.NombreUsuario.Length > 100
            || string.IsNullOrEmpty(request.Password) || request.Password.Length > 128)
            throw new BusinessException("Usuario o contraseña incorrectos.", 401);

        var nombre = request.NombreUsuario.Trim().ToLowerInvariant();
        var user = await repository.ObtenerPorNombreConRelacionesAsync(nombre, cancellationToken);
        var candidate = user ?? DummyUser;
        PasswordVerificationResult verification;
        try
        {
            // Incluso para un usuario inexistente se realiza el trabajo del hash.
            verification = passwordHasher.VerifyHashedPassword(candidate, candidate.PasswordHash, request.Password);
        }
        catch (FormatException)
        {
            verification = PasswordVerificationResult.Failed;
        }
        if (user is null || verification == PasswordVerificationResult.Failed
            || !user.Activo || !user.Persona.Activo || !user.Perfil.Activo)
            throw new BusinessException("Usuario o contraseña incorrectos.", 401);

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newHash = passwordHasher.HashPassword(user, request.Password);
            if (!await repository.ActualizarPasswordHashAsync(user.Id, user.PasswordHash, newHash, cancellationToken))
                throw new BusinessException("Volvé a iniciar sesión.", 401);
        }

        return tokens.Crear(new UsuarioSesionDto(user.Id,
                                                 user.NombreUsuario,
                                                 user.Persona.Nombres,
                                                 user.Persona.Apellidos,
                                                 user.Persona.Documento,
                                                 user.PerfilId,
                                                 user.Perfil.Nombre));
    }

    public async Task<UsuarioSesionDto> ObtenerSesionAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await repository.ObtenerConRelacionesAsync(id, cancellationToken);
        if (user is null || !user.Activo || !user.Persona.Activo || !user.Perfil.Activo)
            throw new BusinessException("La sesión dejó de ser válida.", 401);
        return new UsuarioSesionDto(user.Id, user.NombreUsuario, user.Persona.Nombres,
            user.Persona.Apellidos, user.Persona.Documento, user.PerfilId, user.Perfil.Nombre);
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(
        int pagina = 1, int tamanoPagina = 20, CancellationToken cancellationToken = default)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var entities = await repository.ListarAsync(offset, size, cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<UsuarioDto> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.ObtenerAsync(id, cancellationToken)
            ?? throw new BusinessException("Usuario no encontrado.", 404);
        return Map(entity);
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var nombre = Validation.Required(request.NombreUsuario, "Nombre de usuario", 100).ToLowerInvariant();
        if (nombre.Any(char.IsWhiteSpace))
            throw new BusinessException("El nombre de usuario no puede contener espacios.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length is < 12 or > 128)
            throw new BusinessException("La contraseña debe tener entre 12 y 128 caracteres.");
        if (await repository.ExisteNombreUsuarioAsync(nombre, cancellationToken))
            throw new BusinessException("Ese nombre de usuario ya existe.", 409);
        var persona = await personas.ObtenerAsync(request.PersonaId, cancellationToken)
            ?? throw new BusinessException("La persona no existe.", 404);
        var perfil = await perfiles.ObtenerAsync(request.PerfilId, cancellationToken)
            ?? throw new BusinessException("El perfil no existe.", 404);
        if (!persona.Activo || !perfil.Activo)
            throw new BusinessException("La persona y el perfil deben estar activos.");
        if (await repository.ExistePersonaAsync(request.PersonaId, cancellationToken))
            throw new BusinessException("La persona ya tiene un usuario.", 409);
        var entity = new Usuario
        {
            NombreUsuario = nombre, PersonaId = request.PersonaId,
            PerfilId = request.PerfilId, PasswordHash = string.Empty
        };
        entity.PasswordHash = passwordHasher.HashPassword(entity, request.Password);
        await repository.AgregarAsync(entity, cancellationToken);
        return Map(entity);
    }

    private static UsuarioDto Map(Usuario x) => new(x.Id, x.NombreUsuario, x.PersonaId, x.PerfilId, x.Activo, x.CreadoEn);
}
