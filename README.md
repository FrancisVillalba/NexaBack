# NEXA — Backend

API en C# y ASP.NET Core 10 con PostgreSQL y Entity Framework Core. Incluye login JWT, administración de personas, usuarios, perfiles y permisos, ABM de productos y existencias y movimientos de inventario.

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

## Integración con el frontend

El frontend consume las API con Authorization: Bearer. Los permisos se consultan en PostgreSQL y se incluyen en el JWT. Si cambian, se requiere iniciar sesión nuevamente. La sesión vence a los 30 minutos.

Productos, Inventario, Clientes, Proveedores, Compras y Ventas consultan la base real. Presupuestos, Inicio y Reportes también consultan sus APIs reales.

## Clientes y proveedores: preparación manual

Revisar y ejecutar `src/NexaBack/Infrastructure/Persistence/Sql/006_Separar_Clientes_Proveedores.sql`. El arranque no aplica este script. Crea `comercial.clientes` y `comercial.proveedores` y registra los permisos, sin asignarlos a perfiles ni cargar datos de ejemplo.


Clientes y Proveedores conservan sus datos de identificación, contacto y observaciones. Compras, Ventas, pagos y cobros usan sus APIs reales. Configurar las nuevas acciones de estado desde Permisos y volver a iniciar sesión.

## Inventario: preparación manual

Revisar y ejecutar en NexaDB el script `src/NexaBack/Infrastructure/Persistence/Sql/003_Inventario.sql`. Requiere la tabla `catalogo.productos`. El código no ejecuta este script al arrancar.

El script crea depósitos, existencias y movimientos con auditoría. Incluye Casa central y Depósito Norte; revisar los nombres antes de ejecutarlo. No carga cantidades de ejemplo.

- Crear productos desde el ABM de Productos. Su existencia inicial se consulta como cero.
- En Inventario, elegir un depósito y registrar una Entrada para cargar el saldo inicial.
- Ajuste establece el saldo final deseado; Salida descuenta; Transferencia descuenta del origen y suma al destino de forma atómica.
- Movimientos de stock muestra el historial inmutable, saldos y usuario responsable.
- Permisos: `inventario.ver` para consulta; `movimientos.ver` para historial; `movimientos.crear` y acceso a Inventario para registrar. No se amplían permisos de Operador automáticamente.
- No se permiten saldos negativos, y solo metro y kilo admiten fracciones (hasta tres decimales). La unidad no se cambia después del primer movimiento.
- Los formularios envían versiones y un identificador de operación para rechazar saldos desactualizados y evitar registros duplicados al reintentar.

## PostgreSQL y EF Core

`Infrastructure/Data/Context/Usuarios/UsuariosDbContext.cs` representa la sesión de acceso a datos. Este contexto comparte las entidades de usuarios, productos e inventario, con sus configuraciones y auditoría centralizada. El arranque no crea tablas ni modifica la base.

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

## ABM de Personas

Personas admite búsqueda (`buscar`), filtro opcional `activo`, edición con `PUT /api/usuarios/personas/{id}`
y baja/reactivación con `PATCH /api/usuarios/personas/{id}/estado` y cuerpo `{ "activo": false }` o `true`.
La edición usa las mismas validaciones del alta y excluye el Id actual al comprobar documentos duplicados.
Las operaciones requieren Administrador. No hay eliminación física ni cambios en el esquema de NexaDB.

La baja protege al último administrador activo dentro de una transacción Serializable. Las solicitudes
concurrentes que entren en conflicto reciben 409 para que el usuario actualice y reintente.
Una persona inactiva invalida el acceso de su cuenta mediante la validación JWT existente.

## Frontend conectado

El frontend ya consume POST /api/usuarios/auth/login y GET /api/usuarios/auth/me. Este último devuelve UsuarioSesionDto y permite restaurar la sesión para cualquier perfil autenticado; no requiere Administrador. La validación del JWT comprueba vigencia del usuario, persona y perfil antes de devolver los datos.

El frontend envía el token como Bearer y guarda solo token/vencimiento: sessionStorage por defecto o localStorage con Recordar mi sesión. Cerrar sesión borra el token del navegador; no revoca copias existentes. Usuarios consulta los endpoints reales y puede crear cuentas sobre personas y perfiles registrados. Vite y el host .NET del front reenvían /api a NexaBack, por lo que no se habilitó CORS abierto.

Inventario, Compras y Ventas usan sus APIs reales. Los comprobantes locales anteriores se mantienen separados de estas operaciones; Caja no está integrada; las cuentas corrientes de clientes usan la API real.

El perfil https de NexaBack y el perfil IIS Express usan los mismos puertos locales: HTTPS 44388 y HTTP 56953. Ejecutar uno de esos perfiles, no ambos al mismo tiempo. El frontend usa HTTPS 44388 por defecto. El perfil http alternativo conserva 5261 y requiere configurar NEXA_API_URL explícitamente.


## ABM de depósitos

Administración > Depósitos permite crear, editar, activar y desactivar con auditoría. Ejecutar manualmente `src/NexaBack/Infrastructure/Persistence/Sql/004_Depositos.sql` después de tener Inventario instalado; agrega Descripcion, Version y los permisos depositos.ver/crear/editar/estado. No crea depósitos de ejemplo. Reiniciar la API e iniciar sesión nuevamente.

El nombre es único sin distinguir mayúsculas ni espacios extremos. No se desactiva un depósito con existencias; la comprobación y el cambio de estado son transaccionales. Los movimientos también actualizan su versión para proteger operaciones simultáneas. La administración de depósitos es exclusiva de Administrador.

Clientes y Proveedores guardan nombre o razón social, documento / RUC, correo, teléfono y dirección en sus propias tablas. Personas queda destinada a los usuarios del sistema. Los registros comerciales no exigen apellidos ni seleccionan o crean una Persona. Un mismo documento puede existir en Personas, Clientes y Proveedores; dentro de cada tabla comercial se controla la duplicación por tipo y número de documento. El alta y la edición modifican únicamente el módulo correspondiente, con auditoría y control de versión.

Sobre una instalación que ya tenga comercial.clientes y comercial.proveedores, ejecutar manualmente 009_Clientes_Proveedores_Datos_Propios.sql con el backend detenido. Copia los datos antes asociados a Personas, conserva IDs, auditoría y compras, y elimina PersonaId de las tablas comerciales. No elimina personas existentes. Si faltan datos obligatorios de origen, aborta sin dejar cambios parciales para permitir corregirlos y repetir el script. Luego reiniciar backend y recargar front; no requiere permisos nuevos.

Si ya se ejecutó la versión del 006 que conservaba `vinculos_respaldo`, ejecutar manualmente `007_Eliminar_Vinculos_Respaldo.sql` para quitar esa tabla.

Los ABM comerciales tienen capas independientes: `Repository/Clientes` y `Services/Clientes` contienen `IClienteRepository`, `ClienteRepository`, `IClienteService` y `ClienteService`; Proveedores tiene sus equivalentes en sus propias carpetas. Cada controlador recibe el servicio de su módulo y cada repositorio consulta exclusivamente su tabla. Los DTO de estructura común permanecen compartidos en `Infrastructure/Data/Dtos/Comercial`.

Los catálogos comerciales se consultan en `/api/clientes` y `/api/proveedores`, recorriendo todas las páginas y respetando los permisos de consulta. Presupuestos permite seleccionar clientes activos; Ventas usa su propia API de clientes, productos y depósitos; Compras usa su API propia para buscar proveedores y productos activos y elegir depósitos reales. No se usa un consumidor final ficticio ni contactos del template como alternativa ante errores. Los catálogos se actualizan al navegar o volver a la ventana y se mantienen en memoria, fuera de localStorage. Los IDs reales usan prefijos `clientes:` y `proveedores:` para impedir que los antiguos comprobantes locales se asocien por error a personas reales. Los contactos antiguos no disponibles se muestran como tales; no se borran los comprobantes locales anteriores.

## Compras conectadas a NexaBack

Ejecutar manualmente `back/src/NexaBack/Infrastructure/Persistence/Sql/008_Compras.sql` (desde el backend: `src/NexaBack/Infrastructure/Persistence/Sql/008_Compras.sql`). Requiere las tablas de Proveedores, Productos e Inventario ya preparadas. El arranque no crea tablas ni ejecuta SQL. Después reiniciar el backend; asignar `compras.ver`, `compras.crear`, `compras.recibir` y el nuevo `compras.cancelar` según el perfil y volver a iniciar sesión. El script no asigna permisos a perfiles.

El flujo es Pendiente → Recibida o Pendiente → Cancelada. Crear una orden guarda cabecera y detalle sin cambiar stock. La recepción es completa: valida proveedor, depósito, productos y unidades activos; registra una entrada por línea y actualiza las existencias en la misma transacción. Cada línea guarda el ID del movimiento para consultar su trazabilidad. Los reintentos de recepción no vuelven a sumar cantidades. Las versiones y la transacción serializable rechazan modificaciones simultáneas; ante un conflicto, actualizar y reintentar. Las compras recibidas no se cancelan; devoluciones y recepciones parciales quedan para una etapa posterior.

`comercial.compras` y `comercial.compras_detalles` guardan auditoría de creación y modificación. Los nombres, códigos, unidades y costos de la orden se conservan como datos históricos. El total se calcula en el servidor redondeando cada subtotal a dos decimales; no incluye impuestos, facturación fiscal ni pagos. Recibir una compra modifica existencias y movimientos, sin reemplazar automáticamente el costo ni el precio de venta del catálogo.

El front usa `comprasListado.tsx`, `comprasEdicion.tsx`, `comprasDetalle.tsx`, `comprasServicio.ts` y `comprasTipos.ts`. Los catálogos mínimos de Compras requieren `compras.crear`; no necesitan acceso administrativo a Depósitos. No se importan órdenes del template. Inicio y Reportes consultan las compras y ventas reales mediante su API.

Antecedente del SQL 010: incorporó CI, RUC, PASAPORTE y OTRO como códigos en Clientes/Proveedores. A partir del SQL 015, los tres ABM usan el catálogo compartido y envían TipoDocumentoId; ya no existe una lista fija en el front o en la validación del back. Ejecutar manualmente 010_Tipo_Documento_Comercial.sql después del 009, con el backend detenido. Los registros anteriores quedan con tipo nulo y se muestran como Sin definir, hasta que se editen para clasificarlos. El índice único es por tipo y número dentro de cada tabla; ante una coincidencia con un registro anterior sin clasificar, se solicita editar ese registro para evitar duplicarlo. Las compras nuevas conservan ProveedorTipoDocumento junto al documento histórico; las anteriores no se reclasifican con el tipo actual del proveedor. El SQL 010 no modificaba Personas; el SQL 015 sí incorpora su relación con el catálogo.

## Ventas reales

Ejecutar manualmente 011_Ventas.sql con el backend detenido, sobre la base preparada con Clientes independientes y Tipo de documento (009 y 010), Productos e Inventario. El script crea comercial.ventas y comercial.ventas_detalles, sin importar datos del template ni asignar permisos a perfiles. Reiniciar el backend y configurar ventas.ver, ventas.crear y ventas.anular según corresponda.

Flujo del front: buscar un cliente activo, seleccionar depósito y productos reales, agregar cantidades, calcular el total y confirmar la venta. El servidor calcula precios y descuentos con decimales, redondeando cada línea a dos posiciones. El cálculo no reserva stock. Al confirmar se verifican nuevamente las versiones, precios y saldos; si cambiaron, recalcular y revisar antes de confirmar. Los precios y el total no se aceptan desde el cliente como valores autoritativos.

Confirmar guarda venta, detalle, salidas de inventario y nuevos saldos en una transacción serializable. El UUID de operación y el hash de contenido permiten recuperar la venta en reintentos, sin repetir salidas. Si se pierde una respuesta, el formulario bloquea cambios y ofrece reintentar exactamente la misma confirmación. No crear otra venta hasta comprobar el resultado.

La anulación requiere permiso, versión y motivo. Devuelve toda la cantidad al depósito original mediante nuevas entradas, conserva los movimientos de salida y guarda fecha, usuario y motivo de anulación. Repetir la misma anulación no devuelve stock nuevamente. El depósito original debe estar activo; se permite devolver productos que se desactivaron después de venderlos. No hay anulaciones parciales en esta etapa.

La venta conserva nombre, tipo y número de documento del cliente; depósito; nombres, códigos, unidades, costos y precios de los productos. Confirmada describe la operación de stock: no significa Pagada. Los cobros y las cuentas corrientes de clientes usan la API real. No integra caja ni impuestos desglosados y no emite documentación fiscal. Inicio y Reportes muestran datos reales. El módulo de Presupuestos usa su API real; no importa ni convierte los presupuestos locales anteriores del template.

Código del front en ventasListado.tsx, ventasEdicion.tsx, ventasDetalle.tsx, ventasServicio.ts y ventasTipos.ts. El editor de presupuestos en presupuestosEdicion.tsx utiliza la API real. Inicio muestra las últimas ventas reales y abre su detalle conectado; no muestra comprobantes del template.

## Cobros y cuentas corrientes de clientes

Ejecutar manualmente 012_Cobros_Cuentas_Corrientes.sql, ubicado en back/src/NexaBack/Infrastructure/Persistence/Sql, después del 011 y con el backend detenido. El script crea comercial.cobros con auditoría y agrega el catálogo de permisos; no asigna permisos a perfiles ni carga datos de ejemplo. Reiniciar backend, configurar ventas.cobrar, ventas.anular_cobro y clientes.cuenta_corriente, y volver a iniciar sesión para renovar el token.

En Ventas > Ver detalle > Cobros se registran importes parciales o totales, fecha, medio, referencia y observaciones. El backend impide superar el saldo, protege operaciones simultáneas mediante transacciones y versiones y recupera reintentos con el mismo UUID. Ante una respuesta perdida, reintentar la misma operación antes de registrar otra.

Anular un cobro exige permiso y motivo, conserva su historial y reabre el saldo. No ejecuta devoluciones bancarias ni movimientos de stock. Una venta con cobros vigentes no puede anularse: primero deben anularse sus cobros.

Clientes > Cuenta corriente muestra ventas, cobros, anulaciones y saldo, incluso para clientes inactivos. Los totales se calculan con ventas confirmadas y cobros vigentes; no se almacena un saldo duplicado. Ambos historiales tienen paginación independiente. Inicio y Reportes consultan los saldos reales; no hay integración con Caja ni bancos.

El código mantiene servicios y repositorios independientes para Cobros y CuentasCorrientes. En React, los módulos principales son cobrosVenta.tsx, cobrosServicio.ts y clientesCuentaCorriente.tsx, con sus tipos y comentarios en español.

## Pagos y cuenta corriente de proveedores

Ejecutar manualmente 013_Pagos_Cuentas_Proveedores.sql (back/src/NexaBack/Infrastructure/Persistence/Sql), con el backend detenido. Requiere Compras (008) y los datos comerciales de Proveedores (009 y 010). No ejecuta los scripts anteriores ni modifica stock. Crea comercial.pagos con fechas y usuarios de creación/modificación, versiones, identificador único de operación e historial de anulación. No hay otra tabla de saldos.

Después de ejecutarlo, reiniciar backend, asignar compras.pagar, compras.anular_pago y proveedores.cuenta_corriente desde Permisos y volver a iniciar sesión. El script solo incorpora el catálogo; no amplía permisos de perfiles automáticamente. Para operar desde el detalle se requiere también compras.ver; para abrir el listado de Proveedores, proveedores.ver.

Compras > Ver detalle > Pagos permite registrar pagos parciales o totales de compras recibidas, con fecha, medio, referencia y observaciones. Las compras pendientes y canceladas no generan deuda ni admiten pagos. Los proveedores inactivos conservan su cuenta y se permite pagar sus compras ya recibidas.

El backend calcula el saldo y no acepta sobrepagos. La operación modifica la versión de la compra y registra el pago en una transacción serializable para proteger pagos simultáneos. El UUID y el hash de contenido permiten recuperar una respuesta perdida sin duplicar el pago; la pantalla bloquea cambios y ofrece reintentar la misma operación. Un conflicto requiere actualizar y revisar el saldo antes de continuar.

Anular un pago exige permiso, versión y motivo: conserva el registro, fecha y usuario de anulación y reabre el saldo. No modifica inventario ni realiza transferencias bancarias o movimientos de Caja. Una compra recibida sigue sin poder cancelarse; las devoluciones quedan para otra etapa.

Proveedores > Cuenta corriente muestra compras, pagos y saldo pendiente, con dos historiales paginados y acceso al detalle si se dispone de compras.ver. La deuda se calcula únicamente con compras recibidas menos pagos vigentes, en una instantánea consistente de lectura. El historial conserva compras pendientes/canceladas y pagos anulados sin sumarlos a la deuda.

El backend usa IPagoRepository/PagoRepository, IPagoService/PagoService y sus equivalentes CuentaProveedor. El front mantiene pagosCompra.tsx, pagosServicio.ts, pagosTipos.ts y proveedoresCuentaCorriente.tsx con su servicio y tipos. Inicio y Reportes reflejan estas operaciones reales.

## Presupuestos reales

Ejecutar manualmente 014_Presupuestos.sql, ubicado en back/src/NexaBack/Infrastructure/Persistence/Sql, con el backend detenido. Requiere Clientes independientes (009/010), Productos, Inventario y Ventas (011). Crea comercial.presupuestos y comercial.presupuestos_detalles, con auditoría de cabecera/líneas, versiones y vínculo único a la venta. No importa propuestas del template ni modifica existencias. No hay ejecución automática de scripts al iniciar.

Reiniciar backend y renovar la sesión después de asignar presupuestos.ver/crear/editar/aprobar/cancelar/convertir. El script no amplía permisos de perfiles y desactiva el antiguo presupuestos.eliminar: las propuestas se cancelan con motivo, conservando historial. Convertir exige simultáneamente presupuestos.convertir y ventas.crear; consultar la venta exige ventas.ver. Los catálogos y el cálculo permiten crear o editar según los permisos firmados en el token, sin exigir acceso administrativo.

El circuito es Borrador → Aprobado → Convertido. Solo se editan borradores. Borradores y aprobados se pueden cancelar con motivo; un presupuesto convertido no se cancela ni vuelve a convertirse, aunque luego se anule su venta. Vencido es una condición calculada, no un cambio automático de estado: la fecha es inclusiva según el calendario de Argentina. Un borrador vencido puede editarse para renovar su vencimiento; un aprobado vencido ya no se convierte.

Crear o editar permite buscar cliente activo, depósito y productos reales, cantidades, descuento, observaciones y vencimiento. Calcular total consulta el backend y guardar valida que no hayan cambiado las versiones revisadas. Se conservan nombre/documento del cliente, depósito, códigos/nombres/unidades/precios de productos e importes históricos. Los presupuestos no reservan ni descuentan stock y pueden superar las existencias actuales. Se exige un total positivo y se redondea por línea a dos decimales.

Desde el detalle, aprobar congela el borrador. Revisar y convertir consulta precios y stock actuales: muestra precios originales frente a actuales, descuento conservado y nuevo total. El usuario debe confirmar la revisión. La venta usa precios actuales del catálogo y cantidades del presupuesto; si cambia un producto o falta stock después de revisar, la API rechaza la confirmación y exige recalcular. Los importes originales del presupuesto no se reemplazan por los de la venta.

Venta, movimientos de salida, existencias y estado/vínculo del presupuesto se guardan en una sola transacción serializable. La conversión reutiliza el repositorio de Ventas dentro de esa unidad de trabajo. UUID, hash de contenido, versión del presupuesto y de productos impiden duplicar ventas en reintentos o convertir dos veces. Si se pierde una respuesta, las pantallas bloquean edición y permiten reenviar la misma operación. Crear y editar también conservan su identificador de reintento. Las anulaciones/cobros de la venta siguen usando su circuito normal.

Código del front: presupuestosListado.tsx, presupuestosEdicion.tsx, presupuestosDetalle.tsx, presupuestosConversion.tsx, presupuestosServicio.ts y presupuestosTipos.ts. El listado usa paginación, búsqueda y filtros de estado/vencido. Inicio y Reportes consultan las operaciones reales. No se agregó facturación fiscal.

## Inicio y Reportes conectados

No hay SQL nuevo: solo consultas sobre las tablas ya instaladas de Productos/Inventario, Ventas, Compras, Cobros y Pagos. No guarda indicadores ni duplica saldos. Reiniciar backend y recargar front. Se conservan los permisos inicio.ver, reportes.ver y reportes.exportar; si se cambia su asignación, renovar la sesión.

El backend tiene IReportesRepository/ReportesRepository e IReportesService/ReportesService, con endpoints GET /api/inicio, /api/inicio/depositos, /api/reportes, /api/reportes/depositos y /api/reportes/exportar. Las consultas usan una transacción RepeatableRead para mantener consistentes totales, páginas, análisis y exportación. No ejecuta SaveChanges. Los depósitos se filtran por ID real, incluidos inactivos; el selector de cada pantalla es independiente de la antigua selección local por nombre del template.

Inicio muestra ventas confirmadas, compras recibidas, cobros y pagos vigentes del mes hasta hoy. También muestra deuda actual por cobrar/pagar, valorización actual del inventario, productos activos con stock menor o igual al mínimo, últimas 6 ventas confirmadas y gráfico diario de los últimos 7 días. Permite abrir el detalle real de una venta. Las categorías y el stock se consultan desde las tablas actuales, sin datos locales como alternativa ante un error. Con Todos los depósitos se suma el stock de cada producto en todos los depósitos, incluidos los inactivos, y se compara ese total con su mínimo.

Reportes ofrece Ventas, Compras recibidas, Cobros, Pagos, Inventario, Cuentas por cobrar y Cuentas por pagar. Paginación de 20 filas, totales de todos los registros y filtros por depósito y período (hasta 366 días). Ventas usa CreadoEn; compras, RecibidoEn; cobros y pagos, FechaCobro/FechaPago. El período es inclusivo según el calendario de Argentina: para timestamps se consulta desde la medianoche local hasta la medianoche posterior a Hasta. No depende de la zona horaria del servidor. El gráfico diario también agrupa con esa zona horaria.

Los reportes muestran únicamente operaciones vigentes al consultar. Una venta anulada, compra pendiente/cancelada o cobro/pago anulado no suma a sus totales operativos. Las cuentas y el inventario son actuales, de todos los períodos: no representan un cierre histórico. En ventas/compras, los importes aplicados y saldos incluyen todos los cobros/pagos vigentes de la operación, incluso si su fecha cae fuera del período elegido. Las cuentas agrupan por cliente/proveedor y conservan contactos inactivos.

El análisis de ventas incluye costo histórico de sus líneas, margen bruto estimado (total neto menos ese costo), ticket promedio, ventas por día y hasta 5 productos por importe bruto sin el descuento general. El agrupamiento de productos conserva códigos, nombres y unidades históricos. No suma unidades heterogéneas ni usa el costo actual del catálogo para estimar el margen. El inventario se valoriza al costo actual y también se agrupa por categoría actual; incluye productos inactivos para no ocultar existencias.

Inicio filtra las secciones desde el backend según los permisos firmados en el token. Reportes exige reportes.ver y, según la sección, ventas.ver, compras.ver, inventario.ver, clientes.cuenta_corriente o proveedores.cuenta_corriente. El front oculta las secciones que no corresponden; la API rechaza consultas directas sin permiso. Exportar exige además reportes.exportar. No cambia ni amplía perfiles automáticamente.

Exportar CSV completo consulta nuevamente el backend y exporta todas las filas filtradas, no solo la página visible. Cada archivo incorpora depósito, período/situación actual, fecha de generación y totales de esa misma instantánea. Máximo 10.000 filas: si se supera, solicita reducir el filtro y rechaza la exportación completa en lugar de truncarla. El CSV conserva decimales, escapa separadores/comillas y protege los textos interpretables como fórmulas. Imprimir página imprime la vista visible.

El front usa inicioServicio.ts y reportesServicio.ts/reportesTipos.ts/reportesTabla.tsx/reportesColumnas.ts/reportesGraficos.tsx, con comentarios en español. Se retiró el gráfico de demostración con fechas fijas. Configuración conserva su demostración local. No se agregó integración con Caja, bancos ni facturación fiscal.

## Catálogo de tipos de documento

Ejecutar manualmente 015_Tipos_Documento.sql (back/src/NexaBack/Infrastructure/Persistence/Sql), con el backend detenido, después de tener instalados los módulos anteriores hasta Presupuestos (014). Si ya se ejecutaron los scripts previos, ejecutar solo el 015. No volver a ejecutar el antiguo 010 sobre la estructura normalizada.

El script crea catalogo.tipos_documento con Id, Codigo único, Nombre, Activo, Version y fechas/usuarios de creación y modificación. Incorpora como referencias iniciales CI, RUC, PASAPORTE y OTRO, buscando sus IDs por código. Los IDs no están fijos en el código ni en los formularios. Los tipos adicionales cargados en esa tabla aparecen al volver a abrir los formularios, sin recompilar.

Clientes y Proveedores (comercial) y Personas (usuarios) guardan TipoDocumentoId como FK restrictiva al catálogo. El campo de texto anterior de Clientes/Proveedores se transforma mediante correspondencia de códigos y se elimina solo después de validar las relaciones. Cada módulo conserva su tabla, servicio y repositorio. El catálogo tiene ITipoDocumentoRepository/TipoDocumentoRepository e ITipoDocumentoService/TipoDocumentoService.

Las personas y contactos anteriores sin clasificar conservan TipoDocumentoId nulo; no se les asigna CI ni OTRO por suposición. En altas y ediciones, tanto tipo como número son obligatorios. Se valida en el servidor que el ID exista y esté activo. Un registro puede conservar su tipo actual si luego se desactivó; no se permite asignar un tipo inactivo a otro registro.

La unicidad pasa a (TipoDocumentoId, Documento) en cada una de las tres tablas: el mismo número puede pertenecer a tipos distintos y también a módulos distintos. Un índice adicional protege documentos históricos aún sin tipo. Si un número coincide con un registro sin clasificar, primero debe clasificarse ese registro, evitando duplicarlo. Las FKs impiden borrar un tipo utilizado.

El SQL aborta y revierte si encuentra códigos anteriores sin correspondencia, IDs contradictorios o documentos duplicados dentro del mismo tipo/sin clasificar. Conserva IDs de personas/contactos, usuarios, operaciones, stock, saldos y auditoría existente. No usa CASCADE para quitar dependencias externas; si hubiera una, se debe revisar antes de migrar.

GET /api/tipos-documento entrega Id, Codigo, Nombre y Activo. Requiere autenticación y al menos un permiso de consulta/alta/edición de Clientes, Proveedores o Personas. No agrega permisos a perfiles. Se devuelve el catálogo completo para poder representar tipos inactivos existentes; los formularios ofrecen tipos activos y, al editar, el tipo actual inactivo. No hay una lista fija ni una alternativa desde localStorage si falla la API.

Los DTO de Clientes/Proveedores y Personas devuelven tipoDocumentoId y tipoDocumento (descripción). Sus solicitudes de escritura reciben solo tipoDocumentoId numérico. Las búsquedas para Compras/Ventas/Presupuestos y las cuentas corrientes resuelven el nombre mediante la relación. La selección de personas en Usuarios también muestra el tipo para distinguir números iguales de tipos diferentes.

Compras, Ventas y Presupuestos conservan el tipo de documento como dato histórico en sus cabeceras. Los comprobantes nuevos copian la descripción completa del catálogo; los anteriores conservan el código/texto que tenían, incluso si se renombra el catálogo. El SQL amplía esas columnas a 100 caracteres, sin reescribir sus valores ni exigir relaciones nuevas a comprobantes antiguos.

El front comparte tiposDocumentoServicio.ts, tiposDocumentoEstado.ts, tiposDocumentoSelector.tsx y tiposDocumentoTipos.ts. Muestra carga/error/reintento y bloquea guardar sin una selección válida. Reiniciar backend y recargar el front después del SQL. No se agregó un ABM de mantenimiento de este catálogo; esta etapa incorpora tabla, lectura y selección en los tres formularios.

## Retiro de condiciones comerciales del contacto

Ejecutar manualmente 016_Quitar_Condiciones_Comerciales.sql con el backend detenido, después de los scripts anteriores hasta 015. Elimina PlazoPagoDias y LimiteCredito de comercial.clientes y comercial.proveedores y sus restricciones. No conserva ni traslada sus valores a comprobantes. No usa CASCADE ni modifica operaciones, inventario o cuentas corrientes. Reiniciar el backend y recargar el front.

Los formularios, listados, contratos, entidades, validaciones y catálogos del frontend ya no contienen plazo de pago o límite de crédito. Observaciones permanece disponible. Las condiciones de pago por factura quedan pendientes de implementación; este cambio no agrega campos ni reglas a Ventas/Compras.

## ABM de Marcas y relación con Productos

Ejecutar manualmente 017_Marcas.sql, ubicado en back/src/NexaBack/Infrastructure/Persistence/Sql, con el backend detenido y después de los scripts anteriores hasta 016. Crea catalogo.marcas con Nombre, Descripcion, Activo, Version y auditoría de creación/modificación. No crea marcas de demostración. La migración obtiene las marcas que ya existían como texto en los productos, agrupa por nombre sin distinguir mayúsculas ni espacios extremos y las asocia mediante MarcaId. Los productos sin marca conservan NULL.

El nombre es obligatorio, hasta 100 caracteres. La descripción es opcional, hasta 300. Una columna calculada NombreNormalizado e índice único en PostgreSQL impiden duplicados, incluso ante solicitudes simultáneas o marcas inactivas. Las versiones protegen ediciones y cambios de estado. La auditoría usa el mismo UsuariosDbContext que los demás módulos.

Administración > Marcas permite buscar, filtrar por estado, paginar, crear, editar, activar y desactivar. Es un catálogo administrativo, con permisos marcas.ver/crear/editar/estado y rol Administrador en sus endpoints. El script incorpora el catálogo de permisos, sin asignaciones nuevas a perfiles. Reiniciar backend y renovar la sesión después del SQL. Los perfiles no administrativos no obtienen permisos de mantenimiento de Marcas.

Productos reemplaza el campo de texto por un selector. Guarda MarcaId y devuelve Marca como nombre de lectura obtenido por JOIN. La marca sigue siendo opcional: Sin marca guarda NULL. Se pueden elegir marcas activas; al editar, también se puede conservar la marca actual aunque esté inactiva. No se permite asignar una marca inactiva a un producto nuevo ni cambiar hacia otra inactiva. Desactivar una marca no desactiva ni desvincula sus productos.

GET /api/productos/marcas entrega opciones mínimas paginadas (Id, Nombre, Activo) con permisos de consulta/alta/edición de Productos. El selector recorre todas las páginas y no requiere permisos administrativos ni auditoría de Marcas. Al volver a la ventana o actualizar el catálogo, consulta la API nuevamente; no hay nombres fijos ni datos del template como alternativa ante errores.

El listado de Productos mantiene la búsqueda por nombre de marca y muestra la descripción de la relación. La FK restrictiva impide borrar una marca usada. El ABM realiza baja lógica; no ofrece eliminación física. No modifica códigos de productos, cantidades, costos, precios, movimientos ni comprobantes.

El SQL comprueba la correspondencia antes de quitar el texto antiguo. Si encuentra IDs contradictorios o dependencias externas, revierte para revisión manual; no utiliza CASCADE. Una marca existente no se reemplaza si su nombre normalizado ya coincide. Las operaciones conservan sus datos históricos.

El backend mantiene IMarcaRepository/MarcaRepository e IMarcaService/MarcaService. El front usa marcasListado.tsx, marcasEdicion.tsx, marcasEstado.tsx, marcasServicio.ts, marcasTipos.ts y marcasCatalogo.ts, con comentarios en español.

## Categorías y unidades de medida de Productos

Ejecutar manualmente `back/src/NexaBack/Infrastructure/Persistence/Sql/018_Categorias_UnidadesMedida.sql` después del script 017 de Marcas, con el backend detenido. El archivo SQL no se ejecuta desde la aplicación.

- `catalogo.categorias` reúne las categorías actuales, ignorando diferencias de mayúsculas y espacios exteriores.
- `catalogo.unidades_medida` contiene las seis unidades que ya admite el sistema: unidad, caja, metro, kilo, bolsa y par. El código técnico es estable para conservar las reglas de cantidades y los comprobantes históricos; el nombre es la etiqueta visible.
- Productos guarda `CategoriaId` y `UnidadMedidaId` obligatorios, con claves foráneas y borrado restringido. Los textos originales se eliminan después de migrar las relaciones.
- Ambas tablas tienen fecha y usuario de creación y modificación. Los registros iniciales del script quedan sin usuario porque se crean mediante una migración manual.
- El formulario consulta `GET /api/productos/categorias` y `GET /api/productos/unidades-medida`, con los permisos existentes de consultar, crear o editar productos. Los selectores recorren todas las páginas y cargan automáticamente al abrir el formulario o volver a la ventana.
- No hay valores de respaldo locales. Una consulta fallida o una selección inválida bloquea el guardado. Una opción inactiva solo puede conservarse en el producto que ya la tenía.
- La unidad no se puede cambiar si el producto tiene movimientos de stock. Metro y kilo admiten fracciones; las demás unidades requieren cantidades enteras. Compras, ventas, presupuestos, inventario y reportes consultan las nuevas relaciones; los comprobantes conservan sus unidades históricas.

El mantenimiento de Categorías se realiza desde Administración → Categorías después de aplicar el script 019. Unidades de medida conserva por ahora únicamente su tabla y selector. Si no existían productos, el catálogo de categorías queda vacío hasta cargar las categorías reales del negocio desde su ABM. Reiniciar el backend y recargar el frontend después de aplicar el SQL.

## ABM de Categorías

Ejecutar manualmente `back/src/NexaBack/Infrastructure/Persistence/Sql/019_Abm_Categorias.sql` después del 018, con el backend detenido. Amplía la misma tabla `catalogo.categorias` con descripción opcional y versión de concurrencia; conserva IDs, productos y auditoría. No carga categorías de ejemplo.

Administración → Categorías permite crear, editar, buscar por nombre o descripción, filtrar por estado y activar/desactivar. El nombre es obligatorio (100 caracteres), la descripción admite 300 y los nombres no se pueden repetir, incluso entre inactivos. La baja es lógica. Editar o cambiar el estado requiere la versión consultada para evitar sobrescribir cambios ajenos.

El mantenimiento usa `/api/categorias` y permisos `categorias.ver`, `categorias.crear`, `categorias.editar`, `categorias.estado`, restringidos al administrador. Reiniciar el backend, volver a iniciar sesión para cargar los permisos y recargar el frontend después de ejecutar el SQL. El operador conserva la consulta del selector mediante los permisos de Productos; no recibe acceso administrativo.

El selector de Productos se actualiza al abrir el formulario, recuperar el foco o recibir una confirmación de cambio del ABM. Una categoría inactiva puede conservarse en un producto existente, pero no asignarse a uno nuevo. Renombrar una categoría actualiza su etiqueta en los productos asociados sin cambiar sus IDs.


### Compras: datos consultados por ID

Aplicar manualmente `src/NexaBack/Infrastructure/Persistence/Sql/Compras_Consultar_Datos_Por_Id.sql` con backup y el backend detenido, antes de iniciar esta version. El script no se ejecuta automaticamente.

Compras y sus detalles ya no almacenan copias del nombre/documento/tipo de documento del proveedor, nombre del deposito ni nombre/codigo del producto. Se consultan las relaciones por ProveedorId, DepositoId y ProductoId en listado, detalle, busqueda, documentos y reportes de compras/pagos. El contrato de la API conserva sus campos para el frontend. Un cambio posterior en esos maestros se refleja tambien en compras anteriores. Esto reemplaza para Compras las referencias anteriores de este documento a nombres y documentos historicos; Ventas y Presupuestos mantienen su comportamiento actual.

Cantidad, costo unitario, subtotal, total y unidad acordada siguen perteneciendo a la operacion. Conservar la unidad permite validar la recepcion y detectar un cambio de unidad posterior a la creacion de una orden pendiente.
