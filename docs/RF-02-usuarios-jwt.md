# RF-02 — Autenticación de usuarios con JWT

| Campo | Valor |
|---|---|
| Etapa | 2 — Seguridad |
| Alcance | `BaezStone.Fullstack.Api`: entidad `Usuario`, registro, consulta, login con contraseña hasheada y emisión de token JWT |
| Estado | Implementado y verificado; contrato `{ userName, token }` y `CancellationToken` en `145a1aa`. **Sin commitear**: 400 en `Login`, 401 en `GET`, `ErrorMessages` del 409 y trim de `UserName` |
| Fecha del documento | 2026-11-13 |
| Commits | `5f9511b`, `271194e`, `c039e8e`, `c1a412e` (parte API), `9739286` (protección + CORS), `1696dae` (documentación), `cdff22c` (auto-login), `145a1aa` (contrato del token + `CancellationToken` + carrera) |
| Depende de | `RF-01` (sobre `ResponseDto` y middleware de errores) |

## 1. Alcance

Autenticación usuario/contraseña sin roles ni refresh token: `Register` crea la cuenta con hash HMAC-SHA512 y devuelve un JWT (auto-login), `Login` valida credenciales y devuelve un JWT HS512 de 24 h, `GetUsuario` expone la ficha pública. La autorización de endpoints se aplica con `[Authorize]` sobre JWT.

## 2. Cambios implementados

| Elemento | Detalle |
|---|---|
| `Models/Usuario.cs` | `Id`, `UserName` (`[Required, MaxLength(50)]`), `PasswordHash`/`PasswordSalt` (`byte[]`); `[Index(nameof(UserName), IsUnique = true)]` |
| `Data/ApplicationDbContext.cs` | `DbSet<Usuario> Usuarios` |
| Migración `20261107051358_AgregarUsuario` | Tabla `Usuarios` (10.ª migración del proyecto), PK + índice único `IX_Usuarios_UserName`; aplicada en `ClienteDB` |
| `Dtos/UsuarioDto.cs` | `UserName` requerido, 4–50 (setter con `Trim()`); `Password` requerido, 6–50 |
| `Dtos/UsuarioReadDto.cs` | `Id`, `UserName` — nunca expone hash ni salt |
| `Dtos/UsuarioTokenReadDto.cs` | `UserName`, `Token` — **sin `Id`**; lo devuelven `Login` y `Register` (auto-login) |
| `Profiles/MappingProfile.cs` | `Usuario → UsuarioReadDto` y `Usuario → UsuarioTokenReadDto` |
| `Repositorio/IUsuarioRepositorio.cs` | `Register` (`UsuarioTokenReadDto?`, con `CancellationToken`), `Login` (`UsuarioTokenReadDto?`, con `CancellationToken`), `Existe` (`CancellationToken = default`), `GetUsuarioById` (con `CancellationToken`) |
| `Repositorio/UsuarioRepositorio.cs` | `Existe`/`GetUsuarioById` con `AsNoTracking()` (+`ProjectTo`) y `CancellationToken`; `Register` hashea, devuelve `null` si el usuario ya existe o si el INSERT choca con el índice único (`DbUpdateException` 2601/2627) y, tras `SaveChangesAsync(ct)`, mapea a `UsuarioTokenReadDto` + `Token = CrearToken(usuario)`; usa `Add` (no `AddAsync`); `Login` devuelve `null` uniforme en fallo; `VerificarPasswordHash` privado con `CryptographicOperations.FixedTimeEquals`; `CrearToken` HS512 con `Issuer`/`Audience` desde `AppSettings:*` (`UsuarioRepositorio.cs:102-114`) |
| `Controllers/UsuariosController.cs` | `POST api/usuarios/Login` (200 con `Result = { userName, token }` /400/401), `POST api/usuarios/Register` (201 con `Result = { userName, token }` y `Location` con el `Id` server-side /400/409), `GET api/usuarios/{id:int}` (200/401/404); los tres reciben `CancellationToken`; `[Authorize]` solo en `GET {id}` (`UsuariosController.cs:71`), login y registro anónimos |
| `Program.cs` (JWT) | `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` con `TokenValidationParameters` que validan firma, emisor, audiencia y vigencia (`Program.cs:24-42`); `ClockSkew` desde `AppSettings:ClockSkewMinutes` |
| `Program.cs` (pipeline) | `UseCors("Angular")` → `UseAuthentication()` → `UseAuthorization()` (`Program.cs:105-111`), commit `9739286` |
| `Program.cs` (CORS) | Política nombrada `"Angular"` para `http://localhost:4200` y `https://localhost:4200` (`Program.cs:63-67`); se registra antes de la autenticación |
| `Program.cs` (Swagger) | `SecurityRequirementsOperationFilter` + esquema `"oauth2"` (`Type = Http`, `Scheme = "bearer"`, `BearerFormat = "JWT"`) y `SwaggerDoc("v1")` (`Program.cs:44-61`) |
| `BaezStone.Fullstack.Api.csproj` | `Microsoft.AspNetCore.Authentication.JwtBearer` **6.0.36** (10.x/8.x rompen con net6 → `NU1202`); `Swashbuckle.AspNetCore.Filters` **8.0.3** (9.x/10.x no soportan net6) |
| `appsettings.json` | `AppSettings:Token` = 128 caracteres (la frase de 64 duplicada), `Issuer` = `BaezStone.Fullstack.Api`, `Audience` = `BaezStone.Fullstack.WebUI`, `ClockSkewMinutes` = 5; el token **está commiteado** |

## 3. Decisiones técnicas

1. **`Login` devuelve `null` en cualquier fallo** (usuario inexistente o contraseña incorrecta) → el controlador responde un **401 idéntico** en ambos casos. Evita enumeración de usuarios. Se descartó el patrón de strings mágicos (`"nouser"`/`"wrongpassword"`) y comparar `ToLower()` en la consulta (rompe el uso del índice único).
2. **Token separado en `UsuarioTokenReadDto`**: `GET /api/usuarios/{id}` usa `UsuarioReadDto` y no puede filtrar tokens; el DTO del token no expone el `Id`.
3. **`FixedTimeEquals`** para comparar hashes: evita ataques de temporización.
4. **HMAC-SHA512 con salt aleatorio por usuario** (`new HMACSHA512()` genera la clave/salt); no se usa ASP.NET Identity para mantener la demo sin dependencias extra.
5. **HS512 + clave de 128 caracteres**: HS512 exige ≥ 512 bits (64 bytes); el valor actual duplica el mínimo y es el mismo que se usó en jwt.io.
6. **Claims `ClaimTypes.NameIdentifier` y `ClaimTypes.Name`** → en el payload aparecen como `nameid` y `unique_name`; `exp = iat + 86400` (1 día).
7. **`ValidateIssuer`/`ValidateAudience` en `true`**: `CrearToken` y `AddJwtBearer` leen las mismas claves `AppSettings:Issuer`/`AppSettings:Audience` (emisor y validador alineados) con `?? throw` fail-fast; `ClockSkew` de 5 min es el default del framework, aquí explícito para poder reducirlo.
8. **JWT Bearer 6.0.36**: versión alineada con el resto del stack EF/JwtBearer de net6.
9. **`UseCors` antes de `UseAuthentication`**: al revés, el 401 de `[Authorize]` salía sin `Access-Control-Allow-Origin` y el navegador lo mostraba como error de CORS. Verificado con `curl` (401 con cabecera).
10. **Esquema Swagger `Http`/`bearer`** (no `ApiKey`): con `ApiKey` Swagger UI añadía el prefijo `Bearer` y el header salía duplicado; con `Http` el token se pega desnudo.
11. **Auto-login en `Register`**: el 201 devuelve el mismo `UsuarioTokenReadDto` que `Login` (con token) para ahorrar el segundo `POST /Login`. El token se emite **después** de `SaveChangesAsync`: si se emitiera antes, `usuario.Id` sería el valor temporal de EF y el claim `nameid` saldría en 0.
12. **`null` como «ya existe»** en `Register`: sustituye al `-1` mágico y alinea el contrato con `Login` (`UsuarioTokenReadDto?`); el controlador mapea `null` a 409. Cubre también la carrera `Existe → Insert` (violación del índice único → `null` → 409, nunca 500).
13. **El `Id` no viaja en el body**: `Login` y `Register` devuelven solo `{ userName, token }`. El `Id` sigue en el claim `nameid` (el JWT es legible) y en el `Location` del 201, que el controlador arma con `usuario.Id` (EF lo puebla tras `SaveChangesAsync`).
14. **`CancellationToken` de extremo a extremo** en controlador y repositorio: `FirstOrDefaultAsync`/`AnyAsync`/`SaveChangesAsync` lo reciben; si el cliente corta, el middleware ignora la `OperationCanceledException` (`ErrorHandlingMiddleware.cs:27-30`) y no se completa trabajo inútil. Hashing y firma del token son CPU y no lo usan.
15. **`UserName` normalizado** en el setter de `UsuarioDto`: se trima antes de la validación, así `" juan "` y `"juan"` son el mismo usuario y `"    "` falla `[Required]`. `Password` no se trima.

## 4. Endpoints

| Verbo | Ruta | Cuerpo | Respuestas |
|---|---|---|---|
| POST | `/api/usuarios/Login` | `UsuarioDto` (`userName`, `password`) | 200 con `ResponseDto.Result = { userName, token }`; 400 (validación); 401 uniforme |
| POST | `/api/usuarios/Register` | `UsuarioDto` | 201 + `Location` a `GET /api/usuarios/{id}` con `Result = { userName, token }` (auto-login; el `Id` solo está en el `Location`), 400 (validación), 409 (usuario existente) |
| GET | `/api/usuarios/{id:int}` | — | 200 (`id`, `userName`) con Bearer; 401 sin token; 404 |

## 5. Verificación

- Registro: 201 con `Location` al id real; body `{ userName, token }` sin `id`; `nameid` del token = id real (no 0); duplicado → 409 con `errorMessages` y sin insertar fila.
- Validaciones: 400 con `ResponseDto` (password de 5 caracteres, `UserName` de 3, `UserName` solo espacios).
- Normalización: `Register` con `"  esp…  "` guarda `esp…` y `POST Login` sin espacios → 200.
- Login correcto: 200 con `userName` y `token` (sin `id`).
- Password incorrecta vs. usuario inexistente: 401 con **cuerpos idénticos**.
- jwt.io: cabecera `{"alg":"HS512"}`, payload con `nameid`/`unique_name`, `exp − iat = 86400`, `iss` = `BaezStone.Fullstack.Api`, `aud` = `BaezStone.Fullstack.WebUI` y **`Signature Verified`** con la clave de `appsettings.json`.
- BD: índice único `IX_Usuarios_UserName` presente; migración registrada en `__EFMigrationsHistory`.
- `GET /api/clientes` sin token → 401 + `WWW-Authenticate: Bearer`; con token → 200.
- Swagger UI: `Authorize` con el token (sin prefijo `Bearer`) → `GET /api/clientes` 200.
- CORS: origen permitido → `Access-Control-Allow-Origin`; `http://evil.com` → sin cabecera; preflight `OPTIONS` → 204 con `Access-Control-Allow-Headers: authorization`.
- Verificado con `curl` (2026-11-13): `Register` 201 (`Location` `/api/Usuarios/4`, `nameid` 4), duplicado 409, `"    "` → 400, trim + login 200, 401 idénticos, `GET 4` 401/200 y dos `Register` concurrentes → 201 + 409.

## 6. Pendientes y deudas

| Prioridad | Ítem |
|---|---|
| Resuelto | `UseAuthentication()` (`Program.cs:109`) y `[Authorize]` de `ClientesController` commiteados en `9739286`; verificado 200/401 |
| Resuelto | Issuer/audience configurados en `AppSettings:Issuer`/`AppSettings:Audience` y validados (`Validate*=true`) |
| Resuelto | CORS activo para `localhost:4200` (ver §5) |
| Resuelto | `GET /api/usuarios/{id}` protegido con `[Authorize]` (`UsuariosController.cs:67`) |
| Resuelto | `Register` ya no usa `-1`: devuelve `null` cuando el usuario existe |
| Crítica | Secreto `AppSettings:Token` **commiteado** en `appsettings.json`: mover a User Secrets y dejar un placeholder vacío en el repo |
| Resuelto | Verificar el flujo `Register` → token → `GET {id}` (hecho con `curl` el 2026-11-13) y commitear (`cdff22c`, `145a1aa`) |
| Media | **IDOR**: `GET /api/usuarios/{id}` solo exige autenticación; cualquier usuario autenticado puede leer cualquier `id` (no valida propiedad ni roles) |
| Resuelto | Carrera `Existe → Insert`: `DbUpdateException` 2601/2627 → `null` → 409 (verificado: 201 + 409 concurrentes) |
| Resuelto | `UsuarioLoginReadDto` renombrado a `UsuarioTokenReadDto` (sin `Id`) y usado por `Login` y `Register` |
| Resuelto | `CancellationToken` en `UsuariosController`/`UsuarioRepositorio` (Clientes sigue sin él) |
| Resuelto | 409 con `ErrorMessages` y `UserName` trimado en `UsuarioDto` (working tree) |
| Baja | El `Location` del 201 apunta a `GET {id}`, que exige Bearer: un cliente que siga el header sin token recibe 401 |
| Baja | Mensajes mixtos español/inglés en `UsuarioRepositorio.cs:103` (`"AppSettings:Token is not configured."`) |
| Baja | Sin roles/políticas y sin revocación/refresh token |
| Baja | Cambio de contraseña, bloqueo por intentos y política de complejidad fuera de alcance |

## 7. Próximos pasos

1. Commitear los ajustes del working tree (400/401 en Swagger, `ErrorMessages` del 409 y trim de `UserName`) y actualizar `AGENTS.md` con el HEAD.
2. Mover la clave a User Secrets (`dotnet user-secrets set "AppSettings:Token" "..."`) y dejar un placeholder en el repo.
3. Añadir validación de propiedad/roles en `GET /api/usuarios/{id}` (IDOR) y endurecer `ClockSkew` si se requiere.
4. Escribir el cliente Angular (`RF-03`): login con `{ userName, token }`, interceptor y guard.
