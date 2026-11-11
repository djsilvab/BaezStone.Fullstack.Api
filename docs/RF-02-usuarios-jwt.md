# RF-02 — Autenticación de usuarios con JWT

| Campo | Valor |
|---|---|
| Etapa | 2 — Seguridad |
| Alcance | `BaezStone.Fullstack.Api`: entidad `Usuario`, registro, consulta, login con contraseña hasheada y emisión de token JWT |
| Estado | Implementado y verificado (incluida validación de firma en jwt.io); `UseAuthentication()` y `[Authorize]` listos en árbol de trabajo, **sin commitear** |
| Fecha del documento | 2026-11-10 |
| Commits | `5f9511b`, `271194e`, `c039e8e`, `c1a412e` (parte API) |
| Depende de | `RF-01` (sobre `ResponseDto` y middleware de errores) |

## 1. Alcance

Autenticación usuario/contraseña sin roles ni refresh token: `Register` crea la cuenta con hash HMAC-SHA512, `Login` valida credenciales y devuelve un JWT HS512 de 24 h, `GetUsuario` expone la ficha pública. La autorización de endpoints se aplica con `[Authorize]` sobre JWT.

## 2. Cambios implementados

| Elemento | Detalle |
|---|---|
| `Models/Usuario.cs` | `Id`, `UserName` (`[Required, MaxLength(50)]`), `PasswordHash`/`PasswordSalt` (`byte[]`); `[Index(nameof(UserName), IsUnique = true)]` |
| `Data/ApplicationDbContext.cs` | `DbSet<Usuario> Usuarios` |
| Migración `20261107051358_AgregarUsuario` | Tabla `Usuarios` (10.ª migración del proyecto), PK + índice único `IX_Usuarios_UserName`; aplicada en `ClienteDB` |
| `Dtos/UsuarioDto.cs` | `UserName` requerido, 4–50; `Password` requerido, 6–50 |
| `Dtos/UsuarioReadDto.cs` | `Id`, `UserName` — nunca expone hash ni salt |
| `Dtos/UsuarioLoginReadDto.cs` | `Id`, `UserName`, `Token` — solo lo devuelve `Login` |
| `Profiles/MappingProfile.cs` | `Usuario → UsuarioReadDto` y `Usuario → UsuarioLoginReadDto` |
| `Repositorio/IUsuarioRepositorio.cs` | `Register`, `Login` (`UsuarioLoginReadDto?`), `Existe`, `GetUsuarioById` |
| `Repositorio/UsuarioRepositorio.cs` | `Existe`/`GetUsuarioById` con `AsNoTracking()` (+`ProjectTo`); `Register` hashea y devuelve `-1` si el usuario ya existe; `Login` devuelve `null` uniforme en fallo; `VerificarPasswordHash` privado con `CryptographicOperations.FixedTimeEquals`; `CrearToken` HS512 |
| `Controllers/UsuariosController.cs` | `POST api/usuarios/Login` (200/401), `POST api/usuarios/Register` (201/400/409), `GET api/usuarios/{id:int}` (200/404); sin `[Authorize]` (login y registro deben ser anónimos) |
| `Program.cs` | `AddScoped<IUsuarioRepositorio, UsuarioRepositorio>()`; `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` con `TokenValidationParameters` |
| `Program.cs` (árbol de trabajo) | `app.UseAuthentication()` **nuevo**, entre `UseHttpsRedirection()` y `UseAuthorization()` — sin commitear |
| `BaezStone.Fullstack.Api.csproj` | `Microsoft.AspNetCore.Authentication.JwtBearer` **6.0.36** (10.x/8.x rompen con net6 → `NU1202`) |
| `appsettings.json` | `AppSettings:Token` = 128 caracteres (la frase de 64 duplicada); commiteada |

## 3. Decisiones técnicas

1. **`Login` devuelve `null` en cualquier fallo** (usuario inexistente o contraseña incorrecta) → el controlador responde un **401 idéntico** en ambos casos. Evita enumeración de usuarios. Se descartó el patrón de strings mágicos (`"nouser"`/`"wrongpassword"`) y comparar `ToLower()` en la consulta (rompe el uso del índice único).
2. **Token separado en `UsuarioLoginReadDto`**: `GET /api/usuarios/{id}` usa `UsuarioReadDto` y no puede filtrar tokens.
3. **`FixedTimeEquals`** para comparar hashes: evita ataques de temporización.
4. **HMAC-SHA512 con salt aleatorio por usuario** (`new HMACSHA512()` genera la clave/salt); no se usa ASP.NET Identity para mantener la demo sin dependencias extra.
5. **HS512 + clave de 128 caracteres**: HS512 exige ≥ 512 bits (64 bytes); el valor actual duplica el mínimo y es el mismo que se usó en jwt.io.
6. **Claims `ClaimTypes.NameIdentifier` y `ClaimTypes.Name`** → en el payload aparecen como `nameid` y `unique_name`; `exp = iat + 86400` (1 día).
7. **`ValidateIssuer = false` / `ValidateAudience = false`** para simplificar: el token no se ata a emisor ni audiencia (deuda asumida).
8. **JWT Bearer 6.0.36**: versión alineada con el resto del stack EF/JwtBearer de net6.

## 4. Endpoints

| Verbo | Ruta | Cuerpo | Respuestas |
|---|---|---|---|
| POST | `/api/usuarios/Login` | `UsuarioDto` (`userName`, `password`) | 200 con `ResponseDto.Result = { id, userName, token }`; 401 uniforme |
| POST | `/api/usuarios/Register` | `UsuarioDto` | 201 + `Location` a `GET /api/usuarios/{id}`, 400 (validación), 409 (usuario existente) |
| GET | `/api/usuarios/{id:int}` | — | 200 (`id`, `userName`), 404 |

## 5. Verificación

- Registro: 201 con `Location`; duplicado: 409 sin insertar fila.
- Validaciones: 400 con `ResponseDto` (p. ej. password de 5 caracteres).
- Login correcto: 200 con `token`, `id` y `userName`.
- Password incorrecta vs. usuario inexistente: 401 con **cuerpos idénticos**.
- jwt.io: cabecera `{"alg":"HS512"}`, payload con `nameid`/`unique_name`, `exp − iat = 86400` y **`Signature Verified`** con la clave de `appsettings.json`.
- BD: índice único `IX_Usuarios_UserName` presente; migración registrada en `__EFMigrationsHistory`.

## 6. Pendientes y deudas

| Prioridad | Ítem |
|---|---|
| Crítica | Commitear `app.UseAuthentication()` (`Program.cs:78`) y el `[Authorize]` de `ClientesController`. Si falta `UseAuthentication`, `[Authorize]` responde 401 aun con token válido |
| Crítica | Secreto `AppSettings:Token` **commiteado** en `appsettings.json`: mover a User Secrets y dejar un placeholder vacío en el repo |
| Alta | `GET /api/usuarios/{id:int}` es anónimo y expone `userName`; protegerlo con `[Authorize]` o eliminarlo si no se usa |
| Media | `Register` usa `-1` como código de «ya existe» (magia en el contrato del repo); considerar `bool`/`null` o excepción de dominio |
| Media | Carrera `Existe → Insert`: dos registros concurrentes con el mismo `UserName` terminan en `DbUpdateException` → 500. Capturar el índice único y devolver 409 |
| Media | `Jwt:Issuer`/`Jwt:Audience` se leen pero **no existen** en configuración; hoy son inocuos (`Validate*=false`). Configurarlos o eliminar las líneas |
| Baja | Sin `[Authorize]` adicional, sin roles/políticas y sin revocación/refresh token |
| Baja | Cambio de contraseña, bloqueo por intentos y política de complejidad fuera de alcance |
| Baja | Sin CORS (impide probar el login desde el navegador) |

## 7. Próximos pasos

1. Commitear `UseAuthentication()` + `[Authorize]` y validar en Swagger el flujo `Authorize` → `GET /api/clientes`.
2. Mover la clave a User Secrets (`dotnet user-secrets set "AppSettings:Token" "..."`).
3. Resolver issuer/audience y proteger `GET /api/usuarios/{id}`.
4. CORS para el frontend (`RF-03`).
