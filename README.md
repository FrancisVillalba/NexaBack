# NEXA — Backend

API en C# y ASP.NET Core 10. Base inicial: incluye configuración de PostgreSQL con Entity Framework Core; incluye login JWT y administración básica de personas, perfiles y usuarios; todavía no incluye los módulos comerciales.

## Visual Studio

Abrir `NexaBack.sln` con Visual Studio compatible con .NET 10 y la carga de trabajo **Desarrollo de ASP.NET y web**. Seleccionar `NexaBack` como proyecto de inicio y el perfil `https`. Ejecutar con F5.

## Terminal

```powershell
dotnet build NexaBack.sln
dotnet run --project src/NexaBack --launch-profile https
```

HTTPS: https://localhost:7003. Perfil HTTP alternativo: http://localhost:5261. Si HTTPS está configurado, las peticiones HTTP se redirigen a HTTPS. Usar las herramientas de certificados de desarrollo de .NET si el equipo solicita confiar en el certificado.

## Estructura

- `Program.cs`: servicios y pipeline HTTP.
- `appsettings.json`: configuración general sin secretos.
- `Properties/launchSettings.json`: perfiles locales de Visual Studio.
- `NexaBack.http`: peticiones para probar desde el editor.
- `global.json`: selecciona .NET 10 estable, permitiendo actualizaciones del SDK instalado.

Organización por funcionalidad: cada módulo tendrá sus controladores, contratos y servicios cuando se necesiten. Los controladores reciben solicitudes; los servicios ejecutarán reglas de negocio. Separaremos dominio e infraestructura en proyectos cuando las reglas y la persistencia lo justifiquen.

Se incluyen controladores con `[ApiController]`, referencias anulables, inyección de dependencias y Problem Details para errores. `/health` comprueba disponibilidad de la aplicación. `/health/ready` verifica la conexión a PostgreSQL y devuelve 200 o 503 sin modificar tablas. No se habilita CORS abierto.

## Integración pendiente

El frontend sigue usando su host de demostración para login y usuarios. Esta API no lo reemplaza todavía. El login JWT del backend está implementado; falta adaptar el frontend para consumirlo y enviar Authorization: Bearer. No se copiaron usuarios ni contraseñas de demostración.

Base elegida: PostgreSQL, NexaDB. La administración de personas, usuarios y perfiles usa PostgreSQL y exige Administrador. Falta integrar el frontend y desarrollar los módulos comerciales.

No guardar secretos en el repositorio. Usar User Secrets para desarrollo y configuración segura del entorno para despliegue.


## PostgreSQL y EF Core

`Infrastructure/Data/Context/Usuarios/UsuariosDbContext.cs` representa la sesión de acceso a datos. Este contexto contiene Persona, Usuario y Perfil y aplica solamente sus configuraciones. El arranque no crea tablas ni modifica la base.

La conexión se conserva en Secretos de usuario y en appsettings.Local.json para el desarrollo local con Visual Studio. En Visual Studio: clic derecho en NexaBack → Administrar secretos de usuario. Clave: `ConnectionStrings:NexaDB`. Usar el nombre exacto `NexaDB`, host localhost y puerto 5432. No copiar contraseñas a appsettings.json. Los perfiles locales usan Development y cargan esos secretos automáticamente; en producción usar `ConnectionStrings__NexaDB` en la configuración segura del entorno.

El usuario postgres se usa solamente para la conexión inicial local. Antes del despliegue, utilizar un usuario de aplicación con los permisos necesarios.



## Creación manual de tablas

El script database/001_personas_perfiles_usuarios.sql crea las tres tablas en NexaDB. Se entrega para ejecución manual, no se aplica al arrancar. Si se ejecuta manualmente, habrá que establecer una migración base antes de aplicar futuras migraciones EF: no ejecutar una migración inicial que vuelva a crear las mismas tablas. No crea usuarios ni perfiles de ejemplo. PasswordHash debe generarse desde C# con PasswordHasher<Usuario>.


## Esquema usuarios

Las entidades Persona, Usuario y Perfil se mapean explícitamente al esquema usuarios. Para una base nueva, ejecutar solamente database/001_personas_perfiles_usuarios.sql. Si ya se ejecutó el script anterior que creaba tablas en public, ejecutar solamente database/002_trasladar_a_esquema_usuarios.sql: conserva los datos y relaciones. Son alternativas según el estado de la base; no ejecutar ambos en secuencia.


## Repositorios y servicios

La organización de acceso a datos sigue el esquema PostgreSQL: Repository/Usuarios contiene una carpeta por tabla (Personas, Usuarios y Perfiles), cada una con su repositorio y su interfaz. Las entidades Persona, Usuario y Perfil se ubican en Infrastructure/Data/Context/Usuarios/Entity. Los servicios y sus interfaces se organizan en Services/Usuarios/{Personas,Usuarios,Perfiles} y los DTOs se ubican en Infrastructure/Data/Dtos, separados en Personas, Usuarios y Perfiles.

- `Repository/Usuarios/{Personas,Usuarios,Perfiles}/*Repository.cs`: implementaciones EF Core por tabla.
- `Repository/Usuarios/{Personas,Usuarios,Perfiles}/I*Repository.cs`: interfaz junto a su implementación.
- `Services/Usuarios/{Personas,Usuarios,Perfiles}`: clase e interfaz de servicio por tabla; no dependen directamente de DbContext.
- `Infrastructure/Data/Dtos/{Personas,Usuarios,Perfiles}`: solicitudes y respuestas DTO organizadas por tabla, sin propiedades de navegación ni hashes.

`DependencyInjection.cs` registra los servicios y repositorios con duración Scoped. Cada petición comparte un DbContext. Los listados usan paginación (20 por defecto, máximo 100), orden estable y AsNoTracking; todas las operaciones admiten CancellationToken.

Los servicios validan campos y duplicados. UsuarioService normaliza el nombre de usuario a minúsculas, exige persona y perfil activos, impide dos usuarios para la misma persona y genera PasswordHash con PasswordHasher. La contraseña de la cuenta debe tener entre 12 y 128 caracteres. Nunca registrar el contenido de CrearUsuarioRequest en logs porque incluye la contraseña recibida.

PersistenceErrors convierte conflictos únicos y referencias inexistentes de PostgreSQL en BusinessException sin filtrar detalles SQL. Las restricciones de la base protegen también contra altas concurrentes. Cada alta actual guarda una sola entidad; si una futura operación crea persona y usuario juntos, deberá guardarlos en una única transacción.

Controllers/Usuarios agrupa directamente los tres controladores del esquema. Los endpoints de creación, listado y consulta por ID requieren el rol Administrador y devuelven DTOs. BusinessExceptionHandler traduce errores a Problem Details (400/401/404/409). El login valida credenciales y entrega un JWT; las llamadas sin token válido reciben 401. Las bajas y modificaciones se incorporarán con reglas específicas.


## Controladores

- Controllers/Usuarios/PersonasController.cs: /api/usuarios/personas.
- Controllers/Usuarios/UsuariosController.cs: /api/usuarios/usuarios.
- Controllers/Usuarios/PerfilesController.cs: /api/usuarios/perfiles.

Cada ruta admite GET paginado, GET /{id} y POST. POST responde 201 con Location. Las operaciones requieren Administrador. La autenticación usa JWT Bearer; no emite cookies. El frontend deberá enviar Authorization: Bearer <token> por HTTPS. No se crean usuarios automáticamente.


## Identificadores enteros autoincrementales

Persona, Usuario y Perfil utilizan int en entidades, DTOs, interfaces y controladores. PostgreSQL genera los Id mediante integer GENERATED BY DEFAULT AS IDENTITY y EF Core usa UseIdentityByDefaultColumn. No enviar Id al crear un registro; PersonaId y PerfilId sí deben señalar registros existentes.

Para una base nueva, 001 crea directamente los Id integer. Para las tablas ya creadas con UUID, detener la API y ejecutar solamente database/004_convertir_ids_a_integer.sql desde pgAdmin en NexaDB. Conserva datos y relaciones, pero sustituye los UUID por nuevos números; las referencias fuera de estas tablas requieren adaptación. No utiliza CASCADE: dependencias adicionales hacen que se cancele la transacción. No volver a ejecutar 001 sobre las tablas existentes.

El script 003 de alta de administrador toma el tipo de los identificadores desde las tablas mediante %TYPE. No repetirlo si fvillalba ya existe. La numeración se incrementa automáticamente, aunque las secuencias pueden tener saltos por inserciones fallidas o eliminaciones; no usar estos Id como numeración fiscal.

## Login JWT

`POST /api/usuarios/auth/login` recibe `nombreUsuario` y `password`. El servicio normaliza el usuario, verifica PasswordHasher y exige usuario, persona y perfil activos. Las credenciales incorrectas y los usuarios inexistentes reciben el mismo mensaje 401. El login tiene un límite de 10 peticiones por minuto e IP, en memoria por instancia.

La respuesta contiene `token`, `tipoToken` (Bearer), `expiraEn` (UTC) y `usuario`: usuarioId, nombreUsuario, nombre, apellido, documento, perfilId y perfil. El JWT contiene esos datos (documento solo cuando está registrado), más sub, role, iss, aud, jti, iat, nbf y exp. La contraseña y PasswordHash nunca se incluyen. Un token firmado es legible: no enviarlo a servicios externos para decodificarlo y no registrar tokens ni credenciales en logs.

`Infrastructure/Security` configura y firma los JWT. Se valida firma HS256, emisor, audiencia, vencimiento y vigencia del usuario/perfil en PostgreSQL en cada petición. Cambiar/desactivar el perfil o desactivar el usuario/persona invalida el acceso sin esperar al vencimiento. La duración predeterminada es 15 minutos (tolerancia del reloj: 30 segundos).

`appsettings.json` solo guarda Jwt:Issuer, Jwt:Audience y Jwt:ExpirationMinutes (entre 1 y 60). La clave aleatoria Base64 de al menos 32 bytes está en User Secrets bajo Jwt:SigningKey y en appsettings.Local.json, excluido de Git y de la publicación. En despliegue, configurar Jwt__SigningKey en el almacén de secretos del entorno, además de la conexión. La aplicación valida la configuración al arrancar y no utiliza una clave de ejemplo si falta.

No hay refresh token ni endpoint de logout/revocación individual. El frontend deberá descartar su token al salir, pero este seguirá siendo válido hasta expirar si la cuenta y el perfil permanecen activos. Un cambio de contraseña por sí solo todavía no revoca tokens anteriores. La integración del frontend queda pendiente.

Referencia de validación: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
### Configuración privada para Visual Studio

En Development se carga appsettings.Local.json desde la raíz del proyecto. Contiene Jwt:SigningKey y ConnectionStrings:NexaDB para que el arranque no dependa de la ubicación de User Secrets del proceso de IIS Express. Las variables de entorno y argumentos conservan su prioridad. El archivo está excluido por .gitignore y marcado CopyToOutputDirectory=Never y CopyToPublishDirectory=Never. No se carga en Production.

El archivo ya está configurado en este equipo. No copiar sus valores a appsettings.json, launchSettings.json ni al repositorio. En otro equipo, configurar User Secrets o crear su propio archivo local privado. Para producción usar el almacén seguro del entorno. No se genera una clave nueva automáticamente al arrancar: se mantiene la clave existente y la validación JWT sigue siendo obligatoria.

Después de modificar Program.cs o esta configuración, detener completamente la depuración con Shift+F5 y volver a ejecutar con F5. No agregar AddUserSecrets con optional:false ni crear secrets.json en la carpeta del proyecto.

## Swagger

En Development, Swagger UI está disponible en /swagger y su documento en /swagger/v1/swagger.json. La raíz / redirige a /swagger. Los perfiles de Visual Studio abren esta página automáticamente. Swagger no se registra en Production.

Para probar:

1. Abrir POST /api/usuarios/auth/login, pulsar Try it out y completar nombreUsuario y password.
2. Pulsar Execute y copiar solamente el valor token de la respuesta.
3. Pulsar Authorize y pegar el token sin el prefijo Bearer; Swagger lo agrega automáticamente.
4. Probar las consultas o altas autorizadas. Las altas sí escriben en NexaDB.

El login es público; las operaciones de administración llevan candado y requieren Administrador. Swagger no persiste la autorización tras recargar. No se incluyen contraseñas ni tokens reales en ejemplos ni en el documento. La configuración está en Infrastructure/Documentation/SwaggerSetup.cs.

## Frontend conectado

El frontend ya consume POST /api/usuarios/auth/login y GET /api/usuarios/auth/me. Este último devuelve UsuarioSesionDto y permite restaurar la sesión para cualquier perfil autenticado; no requiere Administrador. La validación del JWT comprueba vigencia del usuario, persona y perfil antes de devolver los datos.

El frontend envía el token como Bearer y guarda solo token/vencimiento: sessionStorage por defecto o localStorage con Recordar mi sesión. Cerrar sesión borra el token del navegador; no revoca copias existentes. Usuarios consulta los endpoints reales y puede crear cuentas sobre personas y perfiles registrados. Vite y el host .NET del front reenvían /api a NexaBack, por lo que no se habilitó CORS abierto.

Los módulos comerciales del frontend aún usan datos locales. La autenticación real no implica que inventario, ventas o caja estén implementados en el backend.

El perfil https de NexaBack y el perfil IIS Express usan los mismos puertos locales: HTTPS 44388 y HTTP 56953. Ejecutar uno de esos perfiles, no ambos al mismo tiempo. El frontend usa HTTPS 44388 por defecto. El perfil http alternativo conserva 5261 y requiere configurar NEXA_API_URL explícitamente.

