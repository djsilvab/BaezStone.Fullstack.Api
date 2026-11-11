# RF-01 — API base: CRUD de Clientes y contrato de errores

| Campo | Valor |
|---|---|
| Etapa | 1 — API base |
| Alcance | `BaezStone.Fullstack.Api`: entidad `Cliente`, repositorio, endpoints CRUD, sobre de respuesta `ResponseDto` y manejo global de errores (400/404/500) |
| Estado | Implementado y verificado; `[Authorize]`, `UseAuthentication()` y CORS commiteados en `9739286` (ver §5 y §6) |
| Fecha del documento | 2026-11-11 |
| Commits | `f38c4aa`, `2e59459`, `fa13c52`, `b0c9e4e`, `7a9a2d0`, `e040432`, `0e0f047`, `9739286` (protección + CORS) |
| Fuera de alcance | Legado `Category`/`Movie` (commits `cc075a2`…`8a760b0`), autenticación (ver `RF-02`), frontend (ver `RF-03`) |

## 1. Alcance

CRUD completo de clientes sobre SQL Server (`ClienteDB`) con paginación pendiente, separación entidad/DTO/AutoMapper y un único contrato de respuesta (`ResponseDto`) para éxito y error en toda la API.

## 2. Cambios implementados

| Elemento | Detalle |
|---|---|
| `Models/Cliente.cs` | `Id`, `Nombres`, `Apellidos`, `Direccion`, `Telefono`; `[Key]` y `[Required]` con `MaxLength(100/100/200/20)`; `FechaCreacion` (no nullable) y `FechaActualizacion` (nullable) |
| `Data/ApplicationDbContext.cs` | `DbSet<Cliente> Clientes`; `OnModelCreating` aplica `HasDefaultValueSql("SYSUTCDATETIME()")` a `FechaCreacion` |
| `Dtos/ClienteCreateDto.cs` | **Un solo DTO** para alta y edición, incluido `Id`, con las mismas validaciones que la entidad |
| `Dtos/ClienteReadDto.cs` | Proyección de lectura con `NombreCompleto`; **no** expone fechas |
| `Profiles/MappingProfile.cs` | `Cliente → ClienteReadDto` (concatena `Nombres + " " + Apellidos`); `ClienteCreateDto → Cliente` ignora `Id`, `FechaCreacion` y `FechaActualizacion` |
| `Repositorio/IClienteRepositorio.cs` + `ClienteRepositorio.cs` | `CreateUpdateCliente`, `DeleteCliente`, `GetClienteById`, `GetClientes`; lecturas con `AsNoTracking().ProjectTo<>()` y orden `Apellidos`, `Nombres` |
| `Dtos/ResponseDto.cs` | Sobre único: `IsSuccess` (default `true`), `Result`, `DisplayMessage`, `ErrorMessages` |
| `Middleware/ErrorHandlingMiddleware.cs` | Registrado primero; captura cualquier excepción no controlada y responde 500 con `ResponseDto`; el stack va en `ErrorMessages` solo en Development; respeta `Response.HasStarted` (relanza) e ignora `OperationCanceledException` si `RequestAborted` está cancelado |
| `Program.cs` | `InvalidModelStateResponseFactory` devuelve `ResponseDto` con 400 (sustituye `ProblemDetails`, `Program.cs:72-87`); política CORS `"Angular"` y validación JWT (ver `RF-02`) |
| `Controllers/ClientesController.cs` | Ruta `api/clientes`, inyección de `IClienteRepositorio`, `ProducesResponseType`, sin `try/catch` y `[Authorize]` a nivel de controlador (`ClientesController.cs:10`, commit `9739286`) |
| Migraciones | `20261101053603_AgregarCliente`, `20261105060429_UpdCliente-FechaCreacionAct`, `20261105065326_ClienteFechaCreacionDefault`, `20261105220604_ClienteMaxLength` |

## 3. Decisiones técnicas

1. **Un solo DTO de escritura con `Id`**: evita duplicar `ClienteCreateDto`/`ClienteUpdateDto`. `POST` exige `Id == 0`; `PUT` exige `{id:int} > 0` e igual al `Id` del body.
2. **`ResponseDto` en toda la respuesta**: 2xx, 400 de validación y 500 del middleware comparten forma; el cliente del frontend solo necesita un modelo.
3. **Fechas en UTC** (`DateTime.UtcNow` en el repositorio, `SYSUTCDATETIME()` en BD). La máquina está en `SA Pacific Standard Time` (UTC-5), por lo que `06:05` en BD son `01:05` locales. La conversión se hace al presentar, nunca con `DateTime.Now`/`GETDATE()`.
4. **Sin `try/catch` en controladores**: el middleware es el único punto de traducción a 500.
5. **Lecturas sin tracking**: `AsNoTracking().ProjectTo<ClienteReadDto>()` para que AutoMapper genere la proyección en SQL.
6. **Una sola cadena de conexión activa**: `ClienteConnection` (`Server=localhost;Database=ClienteDB`) para todo el `DbContext`; `DefaultConnection` (LocalDB `PeliculasDB`) queda sin uso.

## 4. Endpoints

| Verbo | Ruta | Respuestas |
|---|---|---|
| GET | `/api/clientes` | 200 lista ordenada; 401 sin token (`[Authorize]` a nivel de controlador) |
| GET | `/api/clientes/{id:int}` | 200, 404 |
| POST | `/api/clientes` | 201 + `Location` (`CreatedAtAction`), 400 (validación o `Id != 0`), 500 |
| PUT | `/api/clientes/{id:int}` | 200, 400 (`id` inválido o distinto del body), 404 |
| DELETE | `/api/clientes/{id:int}` | 200, 404 |

Todo el cuerpo de respuesta es `ResponseDto`. Errores de validación: `IsSuccess=false`, `DisplayMessage="Error de validación"` y la lista de mensajes en `ErrorMessages`.

## 5. Verificación

- `dotnet build BaezStone.Fullstack.Api` sin errores (línea base: solo `NETSDK1138`).
- Smoke test con la API en ejecución: `GET /api/clientes` → 200 `application/json` camelCase con el sobre `ResponseDto`.
- Flujos 201/400/404 comprobados desde Swagger.
- `GET /api/clientes` sin token → **401** con `access-control-allow-origin`; con `Authorization: Bearer` → **200** (commit `9739286`).
- CORS: `Origin: http://localhost:4200` → `Access-Control-Allow-Origin` + `Vary: Origin`; `Origin: http://evil.com` → **sin** cabecera CORS; preflight `OPTIONS` → **204** con `Access-Control-Allow-Headers: authorization`.
- No existe proyecto de tests automatizados; la verificación es manual vía Swagger y `curl`.

## 6. Pendientes y deudas

| Prioridad | Ítem |
|---|---|
| Resuelto | `[Authorize]` en `ClientesController` + `UseAuthentication()` commiteados en `9739286` (`Program.cs:109`); verificado 401 sin token / 200 con token |
| Resuelto | CORS: política `"Angular"` para `localhost:4200` (http y https), probada con 5 casos y preflight (ver §5) |
| Media | `GetClientes` sin paginación ni filtros |
| Media | Sin `CancellationToken` en repositorio ni controlador |
| Media | `CreateUpdateCliente` mezcla alta y edición; el 500 manual de `POST` duplica la responsabilidad del middleware |
| Baja | `ResponseDto` sin `traceId` para correlacionar con logs |
| Baja | `GetClienteById` usa `c.Id.Equals(id)` en lugar de `==` |
| Baja | Sin tests automatizados |

## 7. Próximos pasos

1. Paginación (`page`/`pageSize`) y `CancellationToken`.
2. Separar `CreateUpdateCliente` en alta/edición y eliminar el 500 manual de `POST`.
3. Añadir `traceId` a `ResponseDto` para correlacionar con logs.
