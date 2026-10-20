# BaezStone.Fullstack — Guía para agentes

Dos apps independientes, un solo repo. Lee la sección de la app que vas a tocar.

| App | Ruta | Stack | ¿En `BaezStone.Fullstack.slnx`? |
|---|---|---|---|
| API | `BaezStone.Fullstack.Api/` | .NET 6 Web API, EF Core 6.0.36, AutoMapper 12, Swashbuckle | **Sí** (único proyecto) |
| Frontend | `BaezStone.Fullstack.WebUI/AppWebWithAng11/` | Angular 11.2.19, TypeScript 4.1.6, TSLint, pnpm | No |

- Todo el árbol `BaezStone.Fullstack.WebUI/` está **sin versionar en git** — confírmalo explícitamente, no asumas que está versionado.
- `.github/workflows/` está **vacío**: no hay CI ni puertas de calidad en PR. Toda la verificación es local.
- No existe proyecto de tests para la API (ni xUnit ni NUnit). No inventes uno por tu cuenta.

## API — `BaezStone.Fullstack.Api/`

### Estructura y convenciones
- `Models/` (entidades EF) → `Dtos/` (DTOs separados de creación `*CreateDto` / lectura `*ReadDto`) → `Profiles/MappingProfile.cs` (AutoMapper) → `Controllers/`.
- AutoMapper se registra una sola vez en `Program.cs` con `AddAutoMapper(typeof(Program).Assembly)`; **todos los mapas nuevos van en `Profiles/MappingProfile.cs`**.
- Las lecturas usan `AsNoTracking().ProjectTo<T>(_mapper.ConfigurationProvider)`. Los POSTs mapean entidad→DTO de lectura a mano tras `SaveChangesAsync`.
- `MovieReadDto.CategoryNombre` **no** se mapea explícitamente — la convención de aplanado (flattening) de AutoMapper lo obtiene de `Movie.Category.Nombre`. Un campo aplanado nuevo en el DTO debe provenir de una propiedad de navegación, no de un escalar en `Movie`.
- `Movie.Category` es `[JsonIgnore]` + `[ValidateNever]`; se carga a mano después del guardado (`Controllers/MovieController.cs:55`).
- `Category.Estado` existe en la entidad pero **no** está en `CategoryCreateDto`/`CategoryReadDto` — la API nunca lo expone.
- Las rutas son `api/[controller]` → `api/category`, `api/movie`. **Solo GET/POST** — no hay Update/Delete para ninguno de los dos recursos.
- `Middleware/ErrorHandlingMiddleware.cs` se registra primero en el pipeline y convierte cualquier excepción no controlada en un 500 con `application/problem+json`. En Development el campo `Detail` contiene el stack completo de la excepción. No añadas try/catch a nivel de controlador.
- Se llama a `app.UseAuthorization()` pero **no hay registrado ningún esquema de autenticación** — la API es totalmente anónima.
- **No hay ninguna política de CORS configurada.** Una app de navegador en otro origen no puede llamar a esta API hasta que se añada una (ver checklist de integración).
- `app.UseHttpsRedirection()` está activo; `dotnet run` usa el perfil de `Properties/launchSettings.json` → `https://localhost:7276` / `http://localhost:5082`, Swagger en `/swagger`.

### EF Core y base de datos
- SQL Server LocalDB, base de datos `PeliculasDB`, cadena de conexión `DefaultConnection` en `appsettings.json` (`Trusted_Connection=True`).
- Las migraciones viven en `BaezStone.Fullstack.Api/Migrations/` (nombres en kebab-case, p. ej. `20260610052233_add-movie.cs`). Las 5 migraciones existentes ya están aplicadas en la BD local.
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
`dotnet build` siempre muestra `NETSDK1138` (net6.0 está fuera de soporte) — ese warning es la línea base, no una breakage. El SDK por defecto es el 10.0.300; también están instalados los SDK 6.0.428/8.0.421 y el runtime 6.0.36, así que la app arranca.

## Frontend — `BaezStone.Fullstack.WebUI/AppWebWithAng11/`

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
- TSLint exige: **comillas simples**, punto y coma obligatorio, línea de 140 caracteres como máximo, sangría de 2 espacios, sin espacios al final.
- `src/app/app-routing.module.ts` tiene un **array `routes` vacío** — `ListaPeliculasComponent` se declara en `AppModule` y se usa directamente en `app.component.html`, no está enrutada.
- `ListaPeliculasComponent` sirve **datos mock hardcodeados** desde un `setTimeout(..., 2500)` — no está conectada a la API.

### Checklist de integración (frontend ↔ API)
Todavía no hay nada conectado. Para llamar a la API desde el navegador necesitas todo esto:
1. Una política de CORS en `Program.cs` (`builder.Services.AddCors(...)` + `app.UseCors(...)` con el origen de desarrollo de Angular, p. ej. `http://localhost:4200`) — actualmente ausente.
2. `HttpClientModule` importado en `src/app/app.module.ts` — actualmente ausente.
3. Una URL base en `src/environments/environment.ts` / `environment.prod.ts` (ambos contienen solo `{ production: bool }`).
4. En la API está activo `UseHttpsRedirection()`: llama a la URL **https**, o enruta el dev server a través de un `proxy.conf.json` de Angular y asume que la redirección romperá el proxy — decide esto a propósito.
5. `Environment` todavía no tiene la clave `apiUrl`; añádela en los dos archivos de environment y mantenlos sincronizados mediante la entrada `fileReplacements` de `angular.json`.

## Trampa de Git
Las reglas de Angular añadidas al `.gitignore` están **ancladas a la raíz** (`/dist`, `/out-tsc`), así que **no** ignoran `BaezStone.Fullstack.WebUI/AppWebWithAng11/dist/` ni `.../out-tsc/` — la salida de build está sin versionar pero visible, y se acabaría committeando. `node_modules/` sí lo ignora el `.gitignore` base (línea ~294).
