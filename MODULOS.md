# Recorte de módulos — Seguridad, Administración y Órdenes de Compra

Este repositorio contiene **solo los archivos de tres módulos** de DocFlow-Infinity,
extraídos para integrarse sobre una base existente. NO es un proyecto compilable por
sí solo: faltan a propósito los archivos compartidos (Program.cs, DependencyInjection,
DbContext, csproj, componentes atómicos del front, etc.), que ya están en la base.

## Qué incluye
- **Órdenes de Compra**: dominio, aplicación (CQRS), infraestructura (servicios,
  repositorio, EF configs), controller, migraciones y tests. Front: página, organisms,
  api client, hooks, tipos y tests.
- **Seguridad**: Auth (login, MFA, password policy), catálogo de permisos + drift guards,
  CsrfProtectionMiddleware, MfaSecretProtector. Front: permissionCatalog, http (CSRF),
  usePermissions, LoginPage.
- **Administración**: mantenedores de catálogos, numeración (contadores/plantillas),
  integraciones, controllers Admin*, roles/usuarios. Front: pages/admin, api/admin.
- **Firma de usuario** (solo config, no estampado): configuración de la firma gráfica por
  usuario (imagen + clave cifrada en reposo + sigla), una por usuario. Backend: entidad
  `FirmaUsuario`, cifrado dedicado (`IFirmaClaveProtector`/`FirmaClaveProtector`), CRUD con
  upsert que preserva la clave si se omite, controllers `AdminUsuariosFirmaController` (admin)
  y `PerfilFirmaController` (autoservicio). Front: `FirmaUsuarioModal` + api clients
  (`firmaUsuarioApi`, `perfilFirmaApi`). El **estampado** de la firma lo consume otro módulo.
- **Flujo de plantillas** (solo config y exposición, no ejecución): definición del flujo
  obligatorio de cada plantilla como pasos ordenados (acción Autorizar/Firmar/Revisar/Visar +
  responsable Usuario/Rol/Departamento). Backend: entidad `PlantillaFlujoPaso`, enums,
  repositorio, resolver de nombre de responsable, endpoints GET/PUT en
  `AdminCatalogosPlantillasController`. Front: `PlantillaFlujoEditor` (selector de responsable
  con buscador, `molecules/SearchableSelect`, incluido) + `plantillaFlujoApi`. La **ejecución**
  del flujo la consume otro módulo.

## Al integrar sobre la base, hay que enganchar el wiring compartido:

> **Nota:** `DependencyInjection.cs`, `DocFlowDbContext.cs`, `ui/package.json`,
> `ui/package-lock.json` y `ui/src/components/atoms/Icon.tsx` **ya vienen incluidos**
> en este recorte para que lo nuestro funcione al integrarlo. Son archivos **de toda la
> app** (no solo de estos módulos): aplicalos sobre la MISMA base DocFlow-Infinity. Si tu
> base difiere, usalos como referencia y aplicá solo el delta descrito abajo.

1. **DI** (`DocFlow.Infrastructure/DependencyInjection.cs`, incluido): además de los
   servicios de OC (IMercadoPublicoService/HttpClient tipado, IOrdenCompraRepository,
   PDF/numeración), IMfaSecretProtector e IIntegracionConfigService, registra los módulos
   nuevos: `IFirmaClaveProtector`→`FirmaClaveProtector`, `IFirmaUsuarioRepository`,
   `IPlantillaFlujoRepository` e `IResponsableFlujoNombreResolver`.
2. **DbContext** (`DocFlowDbContext`, incluido): DbSets de OrdenCompra/Items/Adjuntos +
   DbSets de `FirmaUsuario` y `PlantillaFlujoPaso`, y aplica sus EF configs
   (`FirmaUsuarioConfiguration`, `PlantillaFlujoPasoConfiguration`).
3. **Base de datos** (`db/DocFlow-schema.sql`): script SQL **idempotente del modelo
   completo** (PostgreSQL), generado con `dotnet ef migrations script --idempotent`.
   Crea todo el esquema (`CREATE TABLE IF NOT EXISTS ...`) y guarda cada paso en
   `__EFMigrationsHistory`. Aplicalo sobre una base limpia para levantar el esquema
   completo. Si lo corrés sobre una base que YA tiene tablas con otra historia de
   migraciones, revisá primero: las guardas son por ID de migración, no por existencia
   de tabla. (No se incluyen las migraciones .cs ni el ModelSnapshot: este script es la
   fuente única del esquema.)
4. **Rutas front** (`ui/src/routes`): registrar `/ordenes-compra` y las de admin.
5. **Catálogo de permisos backend** (`PermissionCatalog` + seeder + PermisosDto): los 4
   permisos `ordenescompra.*` deben existir en ambos lados (hay drift guards que lo validan).
6. **appsettings** (NO incluido — secretos, repo público): sección `MercadoPublico`
   (Ticket, BaseUrl, CodigoOrganismo) se configura en runtime desde Admin → Integraciones.
   Para firma de usuario hay que agregar la clave `Security:FirmaEncryptionKey` (AES, mín.
   32 chars) usada para cifrar en reposo la clave de firma. Ejemplo en `appsettings.Development.json`:
   `"Security": { "MfaEncryptionKey": "<32+ chars>", "FirmaEncryptionKey": "<32+ chars>" }`.
7. **Dependencia front nueva**: `pdfjs-dist` (ya está en `package.json`/`package-lock.json`
   incluidos) — la usa `PlantillaMedidasEditor` para la preview del PDF. Correr `npm install`.
8. **Base compartida (front y backend) — no incluida salvo lo que agregamos.** Como todo
   el recorte, nuestro código se apoya en la base común de DocFlow-Infinity que YA existe
   del lado de Pedro. No es que falte "por accidente": son las mismas piezas que el resto
   del recorte ya usa. En concreto, nuestro código nuevo consume de la base:
   - **Front UI**: `components/atoms` (Button, Input, Badge, Spinner, Divider, IconButton,
     Toggle, Tooltip), `molecules/FormField`, `molecules/Pagination`,
     `organisms/ModalDialog`, `organisms/ConfirmDialog`, `organisms/PlantillaEditor`.
   - **Front infra**: `contexts/AuthContext`, `contexts/ToastContext`,
     `hooks/usePasswordPolicy`, `lib/api/auth`, `lib/api/catalogos`, `lib/validations/auth`.
   - **Backend**: `ICurrentUser`, base MediatR (`ISender`) y `ControllerBase` de ASP.NET
     (los controllers no dependen de una clase base propia).

   **Incluido a propósito** para que lo nuestro funcione: `atoms/Icon` (íconos
   `Signature`/`Workflow`), `molecules/SearchableSelect` (buscador del editor de flujo),
   `pages/ProfilePage.tsx` (+test, enganche de firma en Mi Perfil), `DocFlowDbContext.cs`,
   `DependencyInjection.cs` y `package.json`/`package-lock.json`.

   Verificado: ninguna referencia de nuestro código nuevo queda colgando fuera de esta
   base compartida; todo lo propio (apis, repos, interfaces, entidades) está en el recorte.
