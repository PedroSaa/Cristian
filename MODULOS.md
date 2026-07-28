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

## Al integrar sobre la base, hay que enganchar el wiring compartido:
1. **DI** (`DocFlow.Infrastructure/DependencyInjection.cs`): registrar servicios de OC
   (IMercadoPublicoService/HttpClient tipado, IOrdenCompraRepository, PDF/numeración),
   IMfaSecretProtector, IIntegracionConfigService.
2. **DbContext** (`DocFlowDbContext`): DbSets de OrdenCompra/Items/Adjuntos y aplicar
   las EF configs de OrdenesCompra.
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
6. **appsettings**: sección `MercadoPublico` (Ticket, BaseUrl, CodigoOrganismo) — se
   configura en runtime desde Admin → Integraciones.
