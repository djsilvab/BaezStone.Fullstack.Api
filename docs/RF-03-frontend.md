# RF-03 — Frontend Angular y su integración con la API

| Campo | Valor |
|---|---|
| Etapa | 3 — Cliente web |
| Alcance | `BaezStone.Fullstack.WebUI`: `AppWebWithAng11` (existente) y `AppWebWithAng13` (base nueva), estado actual e integración pendiente con la API |
| Estado | Solo estructura; **ningún** frontend consume la API todavía (CORS ya resuelto en la API) |
| Fecha del documento | 2026-11-11 |
| Commits | `44055e4` (Ang11), `c1a412e` (Ang13 + `Añade JWT y base Angular 13`), `9739286` (CORS en la API) |

## 1. Alcance

Documentar la convivencia de dos aplicaciones Angular en el repo, su toolchain fijado y todo lo que falta para que el navegador hable con `BaezStone.Fullstack.Api` (CORS, `HttpClient`, URL base, HTTPS y flujo de login con token).

## 2. Estado actual

### `AppWebWithAng11` (app existente, 41 archivos versionados)
- Angular **11.2.19**, TypeScript **4.1.6**, Angular Material **11.2.13** + CDK, **TSLint**, pnpm.
- Node **14.21.3** fijado en `.npmrc` (`use-node-version`) y `package.json` (`engines`).
- `AppModule` declara `AppComponent`, `ListaPeliculasComponent` y `MenuComponent`; importa `BrowserAnimationsModule` y `MaterialModule`.
- `app-routing.module.ts` con `routes` **vacío**; nada está enrutado.
- `ListaPeliculasComponent` sirve **datos mock hardcodeados** con `setTimeout(..., 2500)`; no hay `HttpClientModule`.
- Línea base roja: `pnpm run lint` = 5 errores (espacios finales, comillas dobles, falta `;`); `ng test` falla 2 de 3 specs (specs del scaffold desactualizadas, `NG0304` por falta de declaración).

### `AppWebWithAng13` (base nueva, 31 archivos versionados)
- Angular **13.1.x**, Angular CLI **13.1.1**, TypeScript **4.5.2**, RxJS **7.4**, Zone.js **0.11.4**, Karma 6.3.
- Scaffold recién generado: `AppComponent` con `title = 'AppWebWithAng13'`, `AppRoutingModule` con `RouterModule.forRoot(routes)` y `routes` **vacío**, estilos **SCSS**.
- Sin Angular Material, sin `HttpClientModule`, sin formularios (`FormsModule`/`ReactiveFormsModule`), sin TSLint/ESLint configurado.
- pnpm con `"packageManager": "pnpm"` en `angular.json`, `.npmrc` con `use-node-version=14.21.3` y su propio `pnpm-workspace.yaml`; `node_modules/` y `dist/` existen localmente pero están ignorados por git.
- No está incluida en `BaezStone.Fullstack.slnx` (la solución solo tiene el proyecto de la API, igual que Ang11).

## 3. Decisiones técnicas heredadas

1. **Node 14.21.3 fijado** en ambos proyectos; no se cambia de versión (nvm4w).
2. **pnpm** como gestor único (`pnpm-lock.yaml`), instalado vía `pnpm-workspace.yaml` (`allowBuilds` para `@angular/cli` y `core-js`).
3. **SCSS**, prefijo `app-`, kebab-case y carpeta por funcionalidad.
4. **Sin rutas reales** en ninguno de los dos: la navegación aún no existe, aunque Ang13 ya importa `RouterModule`.

## 4. Decisión abierta

**¿Ang13 reemplaza a Ang11 o conviven?** No hay decisión registrada. Consecuencias:

| Opción | Efecto |
|---|---|
| Ang13 como app viva | Se migra la UI (menú + lista de películas/cliente) y Ang11 queda congelada o se elimina |
| Ang11 como app viva | Ang13 queda como laboratorio; se mantiene TSLint y Material 11 |
| Convivir | Doble mantenimiento, doble `node_modules` y dos orígenes de CORS |

Recomendación: elegir Ang13 como app viva (Angular 11 está fuera de soporte desde noviembre de 2022) y portar solo lo necesario.

## 5. Checklist de integración (frontend ↔ API)

Ninguno de los dos proyectos está conectado. Para el primer consumo real (`POST /api/usuarios/Login`, luego `GET /api/clientes` con token):

1. ✅ **CORS en la API** — hecho (commit `9739286`): política `"Angular"` para `http://localhost:4200` y `https://localhost:4200`; el preflight permite `authorization`. Ojo: si Ang11 y Ang13 corren a la vez, hay que añadir el segundo puerto.
2. **`HttpClientModule`** en el `AppModule` del frontend elegido — ausente en ambos.
3. **URL base**: añadir `apiUrl` en `src/environments/environment.ts` y `environment.prod.ts` (hoy solo `{ production }`) y mantenerlos sincronizados vía `fileReplacements` de `angular.json`.
4. **HTTPS**: `UseHttpsRedirection()` está activo en la API. Llamar a `https://localhost:7276` (con certificado de desarrollo de confianza) o usar `proxy.conf.json` asumiendo que la redirección puede romper el proxy.
5. **Flujo de login**: servicio de autenticación que haga `POST /api/usuarios/Login`, guarde el `token`, un `HttpInterceptor` que añada `Authorization: Bearer`, y un `CanActivate` para las rutas protegidas.
6. **Manejo del 401 uniforme**: mostrar el mismo mensaje para credenciales inválidas (el backend no distingue usuario inexistente de contraseña mala).
7. **Adaptador de `ResponseDto`**: todos los endpoints devuelven `{ isSuccess, result, displayMessage, errorMessages }`; el frontend debe desenvolver `result` y mostrar `displayMessage`/`errorMessages`.

## 6. Pendientes y deudas

| Prioridad | Ítem |
|---|---|
| Resuelto | CORS en la API (política `"Angular"`, verificada con preflight y origen denegado) |
| Crítica | Decidir el frontend vivo (Ang11 vs. Ang13) antes de escribir código de integración |
| Alta | `HttpClientModule`, `environment.apiUrl`, servicio de auth + interceptor + guard |
| Alta | Sustituir los mocks de `ListaPeliculasComponent` (Ang11) por datos reales, o portar la vista a Ang13 |
| Media | Unificar toolchain: Ang13 sin linter configurado (Ang11 usa TSLint) |
| Media | Rutas reales (`routes` vacío en ambos) y layout/menú en Ang13 |
| Baja | Reparar la línea base de `lint`/`test` de Ang11 si ese proyecto sigue vivo |
| Baja | No hay CI (`.github/workflows/` vacío) para build/lint de ninguno de los dos |

## 7. Próximos pasos

1. Cerrar la decisión Ang11 vs. Ang13.
2. Crear en el frontend elegido: `environment.apiUrl`, `HttpClientModule`, `AuthService`, `AuthInterceptor` y `AuthGuard`.
3. Implementar la pantalla de login y probar el flujo completo contra `RF-02`.
4. Conectar la vista de clientes a `/api/clientes` con el token (`RF-01`).
