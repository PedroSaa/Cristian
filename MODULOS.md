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
1. **DI** (`DocFlow.Infrastructure/DependencyInjection.cs`): registrar servicios de OC
   (IMercadoPublicoService/HttpClient tipado, IOrdenCompraRepository, PDF/numeración),
   IMfaSecretProtector, IIntegracionConfigService. Para los módulos nuevos: registrar
   `IFirmaClaveProtector`→`FirmaClaveProtector`, `IFirmaUsuarioRepository`,
   `IPlantillaFlujoRepository` e `IResponsableFlujoNombreResolver`.
2. **DbContext** (`DocFlowDbContext`): DbSets de OrdenCompra/Items/Adjuntos y aplicar
   las EF configs de OrdenesCompra. Para los módulos nuevos: DbSets de `FirmaUsuario` y
   `PlantillaFlujoPaso` y aplicar `FirmaUsuarioConfiguration` y `PlantillaFlujoPasoConfiguration`.
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
   configura en runtime desde Admin → Integraciones. Para firma de usuario: clave
   `Security:FirmaEncryptionKey` (AES) para el cifrado en reposo de la clave de firma.
7. **Front compartido no incluido**: la firma desde **Mi Perfil** se engancha en
   `ui/src/pages/ProfilePage.tsx` y los botones de flujo/medidas usan `atoms/Icon`
   (íconos `Signature`/`Workflow`); esos archivos compartidos viven en la base. Sí se
   incluye `molecules/SearchableSelect` por ser dependencia directa del editor de flujo.
