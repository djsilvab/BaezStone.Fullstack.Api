# BaezStone.Fullstack — Guía para agentes

Dos apps independientes, un solo repo. Lee la sección de la app que vas a tocar.

| App | Ruta | Stack | ¿En `BaezStone.Fullstack.slnx`? |
|---|---|---|---|
| API | `BaezStone.Fullstack.Api/` | .NET 6 Web API, EF Core 6.0.36, AutoMapper 12 (+ `Extensions.Microsoft.DependencyInjection` 12), `JwtBearer` 6.0.36, Swashbuckle | **Sí** (único proyecto) |
| Frontend (existente) | `BaezStone.Fullstack.WebUI/AppWebWithAng11/` | Angular 11.2.19, TypeScript 4.1.6, Angular Material 11.2.13, TSLint, pnpm | No |
| Frontend (base nueva) | `BaezStone.Fullstack.WebUI/AppWebWithAng13/` | Angular 13.1.x, CLI 13.1.1, TypeScript 4.5.2, SCSS, sin linter, pnpm | No |

- El árbol `BaezStone.Fullstack.WebUI/` **sí está versionado en git** (41 archivos en `AppWebWithAng11` desde `44055e4`; 31 en `AppWebWithAng13` desde `c1a412e`). Los artefactos de build (`node_modules/`, `dist/`, `out-tsc/`) están ignorados; no asumas que hay carpetas sin versionar.
- `.github/workflows/` está **vacío**: no hay CI ni puertas de calidad en PR. Toda la verificación es local.
- No existe proyecto de tests para la API (ni xUnit ni NUnit). No inventes uno por tu cuenta.

## API — `BaezStone.Fullstack.Api/`

### Estructura y convenciones
- `Models/` (entidades EF) → `Dtos/` (DTOs separados de creación `*CreateDto` / lectura `*ReadDto`) → `Profiles/MappingProfile.cs` (AutoMapper) → `Controllers/`. Existe además `Repositorio/` (interfaz + implementación), usado por las features `Cliente` y `Usuario` (ver abajo).
- AutoMapper se registra una sola vez en `Program.cs` con `AddAutoMapper(typeof(Program).Assembly)`; **todos los mapas nuevos van en `Profiles/MappingProfile.cs`**.
- Las lecturas usan `AsNoTracking().ProjectTo<T>(_mapper.ConfigurationProvider)`. `Movie`/`Category` mapean entidad→DTO de lectura a mano tras `SaveChangesAsync`; `ClienteRepositorio` usa `_mapper.Map<ClienteReadDto>`.
- `MovieReadDto.CategoryNombre` **no** se mapea explícitamente — la convención de aplanado (flattening) de AutoMapper lo obtiene de `Movie.Category.Nombre`. Un campo aplanado nuevo en el DTO debe provenir de una propiedad de navegación, no de un escalar en `Movie`.
- `Movie.Category` es `[JsonIgnore]` + `[ValidateNever]`; se carga a mano después del guardado (`Controllers/MovieController.cs:55`).
- `Category.Estado` existe en la entidad pero **no** está en `CategoryCreateDto`/`CategoryReadDto` — la API nunca lo expone.
- Las rutas son `api/[controller]` → `api/category`, `api/movie`, `api/clientes`. `Category` y `Movie` tienen **solo GET/POST**; `api/clientes` tiene **GET (lista/por id), POST, PUT y DELETE**.
- `Middleware/ErrorHandlingMiddleware.cs` se registra primero y convierte cualquier excepción no controlada en un 500 con `ResponseDto` (`IsSuccess=false`, mensaje en español y el stack solo en Development dentro de `ErrorMessages`). Si `Response.HasStarted` relanza la excepción; los `OperationCanceledException` con `RequestAborted` cancelado se ignoran. No añadas try/catch a nivel de controlador.
- La validación automática de `[ApiController]` también responde `ResponseDto` mediante `InvalidModelStateResponseFactory` (`Program.cs:37-55`). Todo el contrato de error (400/401/404/409/500) usa `ResponseDto`; ya no se usa `ProblemDetails`.
- Hay autenticación JWT registrada (ver feature `Usuario`): `AddAuthentication(...).AddJwtBearer(...)` con `TokenValidationParameters` (`Program.cs:20-34`) y `app.UseAuthentication()` antes de `app.UseAuthorization()` (`Program.cs:78-80`). `ClientesController` lleva `[Authorize]`; `UsuariosController` queda anónimo a propósito (login/registro).
- **No hay ninguna política de CORS configurada.** Una app de navegador en otro origen no puede llamar a esta API hasta que se añada una (ver checklist de integración).
- `app.UseHttpsRedirection()` está activo; `dotnet run` usa el perfil de `Properties/launchSettings.json` → `https://localhost:7276` / `http://localhost:5082`, Swagger en `/swagger`.

### Feature `Cliente`
- `Models/Cliente.cs` (Id, Nombres, Apellidos, Direccion, Telefono; `[Key]`, `[Required, MaxLength(100/100/200/20)]`, `= default!`) más `FechaCreacion` (no null, default de BD `SYSUTCDATETIME()`) y `FechaActualizacion` (nullable); registrado como `DbSet<Cliente> Clientes` en `Data/ApplicationDbContext.cs:15`.
- `Dtos/ClienteCreateDto.cs` **incluye `Id`** (decisión: un solo DTO para crear/actualizar) con `[Required, MaxLength]`; `POST` exige `Id == 0` y `PUT` exige `{id:int}` > 0 e igual al `Id` del body. `Dtos/ClienteReadDto.cs` expone `NombreCompleto` (concatenado en `Profiles/MappingProfile.cs`) y **no** expone las fechas.
- `Dtos/ResponseDto.cs` es el sobre único de respuesta (`IsSuccess`, `Result`, `DisplayMessage`, `ErrorMessages`): lo usan el controlador, la validación automática (`Program.cs:37-55`) y el middleware.
- `Repositorio/IClienteRepositorio.cs` + `Repositorio/ClienteRepositorio.cs`: implementados (lecturas con `AsNoTracking().ProjectTo<>`, CRUD, fechas UTC) y registrados con `AddScoped<IClienteRepositorio, ClienteRepositorio>` (`Program.cs:17`). `CreateUpdateCliente` mezcla alta y edición: `Id == 0` crea; si no, actualiza y devuelve `null` cuando no existe.
- `Controllers/ClientesController.cs` → ruta `api/clientes`. GET (lista/por id), POST, PUT y DELETE con `{id:int}`, `ProducesResponseType`, `ResponseDto`, `IClienteRepositorio` y sin `try/catch`. Deuda: sin `CancellationToken`, `CreateUpdateCliente` mezcla create/update, `GetClientes` sin paginación y `ResponseDto` sin `traceId`.
- **Fechas en UTC** (decisión): el repositorio asigna `DateTime.UtcNow` y el default es `SYSUTCDATETIME()`. La conversión a hora local se hace al presentar (`AT TIME ZONE` en SQL o en el frontend); no usar `DateTime.Now`/`GETDATE()`. Esta máquina está en `SA Pacific Standard Time` (UTC-5): `06:05` en BD = `01:05` local.
- Migraciones de `Cliente`: `20261101053603_AgregarCliente` (tabla `Clientes`), `20261105060429_UpdCliente-FechaCreacionAct` (añade `FechaActualizacion`), `20261105065326_ClienteFechaCreacionDefault` (default `SYSUTCDATETIME()`) y `20261105220604_ClienteMaxLength` (longitudes `nvarchar`).

### Feature `Usuario` (autenticación JWT)
- `Models/Usuario.cs`: `Id`, `UserName` (`[Required, MaxLength(50)]`), `PasswordHash`/`PasswordSalt` (`byte[]`); `[Index(nameof(UserName), IsUnique = true)]`. Registrado como `DbSet<Usuario> Usuarios` en `Data/ApplicationDbContext.cs:16`.
- `Dtos/UsuarioDto.cs` (entrada de Login/Register: `UserName` 4-50 y `Password` 6-50), `Dtos/UsuarioReadDto.cs` (`Id`, `UserName`, nunca hash) y `Dtos/UsuarioLoginReadDto.cs` (`Id`, `UserName`, `Token`; solo lo devuelve `Login`).
- `Repositorio/UsuarioRepositorio.cs` (`Program.cs:18`): `Existe`/`GetUsuarioById` con `AsNoTracking()` (+`ProjectTo`), `Register` hashea con HMAC-SHA512 (salt aleatorio) y devuelve `-1` si el usuario ya existe, `Login` devuelve `null` uniforme ante usuario inexistente **o** contraseña mala (anti-enumeración), `VerificarPasswordHash` es privado y usa `CryptographicOperations.FixedTimeEquals`, `CrearToken` firma HS512 con claims `ClaimTypes.NameIdentifier`/`ClaimTypes.Name` y `Expires = UtcNow.AddDays(1)`.
- `Controllers/UsuariosController.cs` → `api/usuarios`: `POST Login` (200 con token / 401 idéntico), `POST Register` (201 + `Location` a `GET {id}`, 400, 409) y `GET {id:int}` (200, 404). **Sin `[Authorize]`**: login y registro deben ser anónimos; `GET {id}` es deuda (expone `userName` sin autenticar).
- `BaezStone.Fullstack.Api.csproj`: `Microsoft.AspNetCore.Authentication.JwtBearer` **6.0.36** (10.x/8.x incompatibles con net6 → `NU1202`). La clave vive en `appsettings.json` → `AppSettings:Token` (128 caracteres) y **está commiteada**: deuda de seguridad, mover a User Secrets.
- `Program.cs:20-34`: `ValidateIssuer`/`ValidateAudience` en `false` y `ValidIssuer`/`ValidAudience` leen `Jwt:Issuer`/`Jwt:Audience`, claves que **no existen** en configuración (inocuo mientras no se validen).
- Migración: `20261107051358_AgregarUsuario` (tabla `Usuarios` + índice único `IX_Usuarios_UserName`). Evidencia funcional: jwt.io verifica la firma y `exp − iat = 86400`.
- Documentación detallada: `docs/RF-02-usuarios-jwt.md`.

### EF Core y base de datos
- Hay **dos** cadenas de conexión en `appsettings.json`:
  - `DefaultConnection` → LocalDB `PeliculasDB` (`Server=(localdb)\mssqllocaldb;Trusted_Connection=True`).
  - `ClienteConnection` → `Server=localhost;Database=ClienteDB` (SQL Server local, **no** LocalDB).
- `Program.cs:11` registra el `DbContext` con **`ClienteConnection`**, no con `DefaultConnection`; la API actual apunta a `ClienteDB`.
- Las migraciones viven en `BaezStone.Fullstack.Api/Migrations/` (nombres en kebab-case salvo `AgregarCliente`, p. ej. `20260610052233_add-movie.cs`). Hay **10 migraciones**; la más reciente es `20261107051358_AgregarUsuario` y están todas aplicadas en `ClienteDB` y commiteadas.
- **La herramienta global `dotnet-ef` instalada en esta máquina es la v10.0.9 y revienta contra este proyecto net6.0/EF6** (`FileLoadException: Could not load file or assembly 'System.Runtime, Version=8.0.0.0'`). Usa una herramienta 6.x:
  ```bash
  dotnet tool install --global dotnet-ef --version 6.0.36
  ```
- Ejecuta los comandos de EF desde `BaezStone.Fullstack.Api/` (es el proyecto de arranque) o pasa `--project`:
  ```bash
  dotnet ef migrations add <Name>       # desde BaezStone.Fullstack.Api/
  dotnet ef database update             # desde BaezStone.Fullstack.Api/
  ```

### Comandos
```bash
dotnet build BaezStone.Fullstack.Api
dotnet run --project BaezStone.Fullstack.Api
```
`dotnet build` siempre muestra `NETSDK1138` (net6.0 está fuera de soporte) — ese warning es la línea base, no una breakage. El SDK por defecto es el 10.0.402; también están instalados los SDK 6.0.428/8.0.421 y el runtime 6.0.36, así que la app arranca.

## Frontend — `BaezStone.Fullstack.WebUI/`

Hay **dos** apps Angular independientes (ninguna está en la solución ni consume la API todavía):

| App | Angular | TypeScript | Extras | Estado |
|---|---|---|---|---|
| `AppWebWithAng11/` | 11.2.19 | 4.1.6 | Material 11.2.13, TSLint | UI de películas con datos mock |
| `AppWebWithAng13/` | 13.1.x | 4.5.2 | SCSS, sin linter | Scaffold con routing vacío |

Ambas fijan Node **14.21.3** con pnpm. Documentación detallada: `docs/RF-03-frontend.md`.

### `AppWebWithAng11/`

### Peculiaridades del toolchain
- **Node 14.21.3 está fijado** tanto en `.npmrc` (`use-node-version=14.21.3`) como en `package.json` (`engines`). La máquina usa nvm4w; no cambies de versión de Node.
- El gestor de paquetes es **pnpm** (`angular.json` → `cli.packageManager: "pnpm"`, `pnpm-lock.yaml`). En el terminal integrado de VS Code `pnpm` funciona directamente (11.22.0).
- **Cuidado con `pnpm` en shells nuevas**: nvm4w dejó en `C:\nvm4w\nodejs\pnpm*` unos shims que apuntan a `global\v11\20c0-19ef0266896`, una instalación de pnpm que ya no existe. En cmd.exe —o en la shell de un agente— el PATH del sistema coloca `C:\nvm4w\nodejs` **antes** que `%PNPM_HOME%\bin`, así que `pnpm` resuelve al shim roto y falla con *"El sistema no puede encontrar la ruta especificada"*. Ya se renombraron a `*.bak` (2026-10-20), pero nvm4w puede recrearlos en el próximo `nvm use`: si reaparecen, usa la ruta absoluta `C:\Users\djsil\AppData\Local\pnpm\bin\pnpm.cmd` (pnpm 11.22.0).
- `pnpm-workspace.yaml` declara `allowBuilds` para `@angular/cli` y `core-js` — esto sustituye al paso manual `pnpm approve-builds` que aparece en `BaezStone.Fullstack.WebUI/notas-angular.txt`. **Lee ese archivo** — es el log de instalación de esta app.
- `node_modules/` ya está instalado. Los componentes nuevos se generan con `pnpm exec ng g c <carpeta>/<nombre> --skip-tests=true` (no se crea archivo spec).

### Comandos verificados
```bash
pnpm run build                                              # funciona; genera dist/AppWebWithAng11
pnpm run lint                                               # TSLint (no ESLint) — falla en la línea base, ver abajo
pnpm exec ng test --watch=false --browsers=ChromeHeadless   # usa esto, no `pnpm test` a secas
pnpm exec ng serve                                          # dev server en http://localhost:4200
```
- `pnpm start` es alias de `ng serve`. El primer arranque tarda ~30 s porque no existe caché `.angular/`; después recompila incrementalmente.
- **No ejecutes `pnpm test` a secas** — `karma.conf.js` fija `browsers: ['Chrome']`, `autoWatch: true`, `singleRun: false`, así que abre un Chrome interactivo y se queda vigilando para siempre.
- **Fallos de línea base (preexistentes, no son tu regresión) — no los "arregles" salvo que la tarea sea exactamente eso:**
  - `pnpm lint` → 5 errores: espacio en blanco al final en `src/app/app.component.ts:11`; comillas dobles en `src/app/peliculas/lista-peliculas/lista-peliculas.component.ts:21,27,33`; falta el punto y coma en `:36`.
  - `ng test` → **fallan 2 de 3 specs**. `src/app/app.component.spec.ts` sigue|Asertando los valores por defecto del scaffold (`title === 'AppWebWithAng11'`, `.content span` en la plantilla) pero el componente se cambió a `title = 'Introduccion a Angular'`; además el spec solo declara `AppComponent`, así que la plantilla registra `NG0304: 'app-lista-peliculas' is not a known element`.

### Convenciones
- SCSS, prefijo de componente `app-`, nombres de archivo en kebab-case, carpeta por funcionalidad.
- `AppModule` declara `AppComponent`, `ListaPeliculasComponent` y `MenuComponent`, e importa `BrowserAnimationsModule` y `MaterialModule` (`src/app/material/material.module.ts`, Angular Material 11.2.13 + CDK).
- `app.component.html` renderiza `<app-menu>` y `<app-lista-peliculas [peliculas]="peliculasEpoca80">`; ningún componente está enrutado.
- TSLint exige: **comillas simples**, punto y coma obligatorio, línea de 140 caracteres como máximo, sangría de 2 espacios, sin espacios al final.
- `src/app/app-routing.module.ts` tiene un **array `routes` vacío**; no hay navegación entre componentes.
- `ListaPeliculasComponent` sirve **datos mock hardcodeados** desde un `setTimeout(..., 2500)` — no está conectada a la API.

### Checklist de integración (frontend ↔ API)
Todavía no hay nada conectado. Para llamar a la API desde el navegador necesitas todo esto:
1. Una política de CORS en `Program.cs` (`builder.Services.AddCors(...)` + `app.UseCors(...)` con el origen de desarrollo de Angular, p. ej. `http://localhost:4200`) — actualmente ausente.
2. `HttpClientModule` importado en `src/app/app.module.ts` — actualmente ausente.
3. Una URL base en `src/environments/environment.ts` / `environment.prod.ts` (ambos contienen solo `{ production: bool }`).
4. En la API está activo `UseHttpsRedirection()`: llama a la URL **https**, o enruta el dev server a través de un `proxy.conf.json` de Angular y asume que la redirección romperá el proxy — decide esto a propósito.
5. `Environment` todavía no tiene la clave `apiUrl`; añádela en los dos archivos de environment y mantenlos sincronizados mediante la entrada `fileReplacements` de `angular.json`.

### `AppWebWithAng13/`
- Scaffold de Angular **13.1.x** (CLI 13.1.1, TypeScript 4.5.2, RxJS 7.4, Karma 6.3) con SCSS y `AppRoutingModule` (`routes` vacío). **Sin** Angular Material, sin `HttpClientModule`, sin `FormsModule`/`ReactiveFormsModule` y sin linter.
- Mismo toolchain que Ang11: `.npmrc` con `use-node-version=14.21.3`, `"packageManager": "pnpm"` en `angular.json`, `pnpm-lock.yaml` y `pnpm-workspace.yaml` propios. `node_modules/` y `dist/` existen localmente pero están ignorados.
- Comandos: `pnpm start` / `pnpm run build` / `pnpm exec ng test --watch=false --browsers=ChromeHeadless` (no uses `pnpm test` a secas: mismo `karma.conf.js` interactivo).
- **Decisión pendiente**: no está definido si Ang13 reemplaza a Ang11 o conviven. No empieces la integración con la API sin cerrar esa decisión.

## Git
- El árbol `BaezStone.Fullstack.WebUI/` **sí está versionado** (41 archivos en `AppWebWithAng11` + 31 en `AppWebWithAng13`). El `.gitignore` anidado de Angular (`AppWebWithAng11/.gitignore:5,7`) ignora `/dist` y `/out-tsc`, y el `.gitignore` raíz (`:294`) ignora `node_modules/`; los tres quedan fuera de git. La antigua "trampa" de reglas ancladas a la raíz ya no aplica.
- El árbol puede tener trabajo en curso; confirma con `git status` antes de tocar. Último commit conocido: `c1a412e` (`Añade JWT y base Angular 13`). En la última revisión había cambios sin commitear en `Program.cs` (`app.UseAuthentication()`) y `Controllers/ClientesController.cs` (`[Authorize]`).
- Documentos de requerimientos/estado: `docs/RF-01-clientes.md`, `docs/RF-02-usuarios-jwt.md`, `docs/RF-03-frontend.md`.
