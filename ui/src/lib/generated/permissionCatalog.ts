export const ADMIN_PERMISSION_PREFIX = 'admin.' as const;

export const PERMISSIONS = {
  BANDEJA_VER: 'bandeja.ver',
  DOCUMENTOS_CREAR: 'documentos.crear',
  DOCUMENTOS_VER: 'documentos.ver',
  DESPACHO_VER: 'despacho.ver',
  EXPEDIENTES_VER: 'expedientes.ver',
  ARCHIVADORES_VER: 'archivadores.ver',
  REPORTES_GENERAR: 'reportes.generar',
  PROVEEDORES_VER: 'proveedores.ver',
  FACTURAS_VER: 'facturas.ver',
  ORDENES_COMPRA_VER: 'ordenescompra.ver',
  ORDENES_COMPRA_CREAR: 'ordenescompra.crear',
  ORDENES_COMPRA_APROBAR: 'ordenescompra.aprobar',
  ORDENES_COMPRA_ANULAR: 'ordenescompra.anular',
  OIRS_VER: 'oirs.ver',
  FIRMAS_VER: 'firmas.ver',
  RRHH_GESTIONAR: 'rrhh.gestionar',
  ADMIN_USUARIOS_VER: 'admin.usuarios.ver',
  ADMIN_USUARIOS_CREAR: 'admin.usuarios.crear',
  ADMIN_USUARIOS_EDITAR: 'admin.usuarios.editar',
  ADMIN_USUARIOS_ACTIVAR: 'admin.usuarios.activar',
  ADMIN_USUARIOS_DESACTIVAR: 'admin.usuarios.desactivar',
  ADMIN_USUARIOS_RESET_PASSWORD: 'admin.usuarios.reset-password',
  ADMIN_USUARIOS_BLOQUEAR: 'admin.usuarios.bloquear',
  ADMIN_DEPARTAMENTOS_VER: 'admin.departamentos.ver',
  ADMIN_DEPARTAMENTOS_EDITAR: 'admin.departamentos.editar',
  ADMIN_ROLES_VER: 'admin.roles.ver',
  ADMIN_ROLES_CREAR: 'admin.roles.crear',
  ADMIN_ROLES_EDITAR: 'admin.roles.editar',
  ADMIN_ROLES_ELIMINAR: 'admin.roles.eliminar',
  ADMIN_ROLES_PERMISOS: 'admin.roles.permisos',
  ADMIN_CATALOGOS_VER: 'admin.catalogos.ver',
  ADMIN_CATALOGOS_EDITAR: 'admin.catalogos.editar',
  ADMIN_NUMERACION_VER: 'admin.numeracion.ver',
  ADMIN_NUMERACION_EDITAR: 'admin.numeracion.editar',
  ADMIN_PLANTILLAS_NUMERACION_VER: 'admin.plantillasNumeracion.ver',
  ADMIN_PLANTILLAS_NUMERACION_EDITAR: 'admin.plantillasNumeracion.editar',
  ADMIN_CONFIG_VER: 'admin.config.ver',
  ADMIN_CONFIG_EDITAR: 'admin.config.editar',
  ADMIN_INTEGRACIONES_VER: 'admin.integraciones.ver',
  ADMIN_INTEGRACIONES_EDITAR: 'admin.integraciones.editar',
  ADMIN_AUDITORIA_VER: 'admin.auditoria.ver',
  ADMIN_RESPALDOS_VER: 'admin.respaldos.ver',
  ADMIN_RESPALDOS_CREAR: 'admin.respaldos.crear',
  ADMIN_RESPALDOS_EDITAR: 'admin.respaldos.editar',
  ADMIN_RESPALDOS_DESCARGAR: 'admin.respaldos.descargar',
  ADMIN_RESPALDOS_RESTAURAR: 'admin.respaldos.restaurar',
  ADMIN_RESPALDOS_CONFIGURAR: 'admin.respaldos.configurar',
} as const;

export type PermissionKey = typeof PERMISSIONS[keyof typeof PERMISSIONS];

export const ADMIN_CATALOGOS_ACCESS_PERMISSIONS = [
  PERMISSIONS.ADMIN_CATALOGOS_VER,
  PERMISSIONS.ADMIN_CATALOGOS_EDITAR,
] as const;

export const ADMIN_NUMERACION_ACCESS_PERMISSIONS = [
  PERMISSIONS.ADMIN_NUMERACION_VER,
  PERMISSIONS.ADMIN_NUMERACION_EDITAR,
] as const;

export const ADMIN_PLANTILLAS_NUMERACION_ACCESS_PERMISSIONS = [
  PERMISSIONS.ADMIN_PLANTILLAS_NUMERACION_VER,
  PERMISSIONS.ADMIN_PLANTILLAS_NUMERACION_EDITAR,
] as const;

type NavItem = {
  to: string;
  label: string;
  requiredPermission: PermissionKey;
  accessPermissions?: readonly PermissionKey[];
};

type NavGroup = {
  label: string;
  items: readonly NavItem[];
};

export const APP_NAV_ITEMS: readonly NavItem[] = [
  { to: '/inbox', label: 'Bandeja de Entrada', requiredPermission: PERMISSIONS.BANDEJA_VER },
  { to: '/documentos/nuevo', label: 'Registro', requiredPermission: PERMISSIONS.DOCUMENTOS_CREAR },
  { to: '/despacho', label: 'Despacho', requiredPermission: PERMISSIONS.DESPACHO_VER },
  { to: '/documentos', label: 'Consulta', requiredPermission: PERMISSIONS.DOCUMENTOS_VER },
  { to: '/expedientes', label: 'Expedientes', requiredPermission: PERMISSIONS.EXPEDIENTES_VER },
  { to: '/archivadores', label: 'Archivadores', requiredPermission: PERMISSIONS.ARCHIVADORES_VER },
  { to: '/reportes', label: 'Reportes', requiredPermission: PERMISSIONS.REPORTES_GENERAR },
  { to: '/proveedores', label: 'Proveedores', requiredPermission: PERMISSIONS.PROVEEDORES_VER },
  { to: '/facturas', label: 'Facturas DTE', requiredPermission: PERMISSIONS.FACTURAS_VER },
  { to: '/ordenes-compra', label: 'Órdenes de Compra', requiredPermission: PERMISSIONS.ORDENES_COMPRA_VER },
  { to: '/oirs', label: 'OIRS', requiredPermission: PERMISSIONS.OIRS_VER },
  { to: '/rrhh', label: 'RRHH', requiredPermission: PERMISSIONS.RRHH_GESTIONAR },
  { to: '/firmas/historial', label: 'Firma Electrónica', requiredPermission: PERMISSIONS.FIRMAS_VER },
] as const;

export const ADMIN_NAV_ITEMS: readonly (NavItem | NavGroup)[] = [
  { to: '/admin/usuarios', label: 'Usuarios', requiredPermission: PERMISSIONS.ADMIN_USUARIOS_VER },
  { to: '/admin/departamentos', label: 'Departamentos', requiredPermission: PERMISSIONS.ADMIN_DEPARTAMENTOS_VER },
  { to: '/admin/roles', label: 'Roles', requiredPermission: PERMISSIONS.ADMIN_ROLES_VER },
  {
    label: 'Mantenedores',
    items: [
      { to: '/admin/mantenedores/categorias', label: 'Categorías', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/clasificaciones', label: 'Clasificaciones', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/formas-envio', label: 'Formas de envío', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/tareas', label: 'Tareas', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/formatos-documento', label: 'Formatos de documentos', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/plantillas', label: 'Plantillas', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/plantillas-numeracion', label: 'Plantillas Numeración', requiredPermission: PERMISSIONS.ADMIN_PLANTILLAS_NUMERACION_VER, accessPermissions: ADMIN_PLANTILLAS_NUMERACION_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/contadores-numeracion', label: 'Contadores Numeración', requiredPermission: PERMISSIONS.ADMIN_NUMERACION_VER, accessPermissions: ADMIN_NUMERACION_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/correlativos', label: 'Correlativos', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/tipos-remitente', label: 'Tipo remitente', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
      { to: '/admin/mantenedores/remitentes', label: 'Remitente', requiredPermission: PERMISSIONS.ADMIN_CATALOGOS_VER, accessPermissions: ADMIN_CATALOGOS_ACCESS_PERMISSIONS },
    ],
  },
  { to: '/admin/configuracion', label: 'Configuración', requiredPermission: PERMISSIONS.ADMIN_CONFIG_VER },
  { to: '/admin/integraciones', label: 'Integraciones', requiredPermission: PERMISSIONS.ADMIN_INTEGRACIONES_VER },
  { to: '/admin/auditoria', label: 'Auditoría', requiredPermission: PERMISSIONS.ADMIN_AUDITORIA_VER },
  { to: '/admin/respaldos', label: 'Respaldos', requiredPermission: PERMISSIONS.ADMIN_RESPALDOS_VER },
] as const;

export const PERMISSION_LABELS: Record<string, string> = {
  [PERMISSIONS.BANDEJA_VER]: 'Ver bandeja de entrada',
  [PERMISSIONS.DOCUMENTOS_CREAR]: 'Crear documentos',
  [PERMISSIONS.DOCUMENTOS_VER]: 'Ver documentos',
  [PERMISSIONS.DESPACHO_VER]: 'Ver despacho',
  [PERMISSIONS.EXPEDIENTES_VER]: 'Ver expedientes',
  [PERMISSIONS.ARCHIVADORES_VER]: 'Ver archivadores',
  [PERMISSIONS.REPORTES_GENERAR]: 'Generar reportes',
  [PERMISSIONS.PROVEEDORES_VER]: 'Ver proveedores',
  [PERMISSIONS.FACTURAS_VER]: 'Ver facturas',
  [PERMISSIONS.ORDENES_COMPRA_VER]: 'Ver órdenes de compra',
  [PERMISSIONS.ORDENES_COMPRA_CREAR]: 'Crear órdenes de compra',
  [PERMISSIONS.ORDENES_COMPRA_APROBAR]: 'Aprobar órdenes de compra',
  [PERMISSIONS.ORDENES_COMPRA_ANULAR]: 'Anular órdenes de compra',
  [PERMISSIONS.OIRS_VER]: 'Ver OIRS',
  [PERMISSIONS.FIRMAS_VER]: 'Ver firma electrónica',
  [PERMISSIONS.RRHH_GESTIONAR]: 'Acceso a módulo RRHH',
  [PERMISSIONS.ADMIN_USUARIOS_VER]: 'Ver lista de usuarios',
  [PERMISSIONS.ADMIN_USUARIOS_CREAR]: 'Crear usuarios',
  [PERMISSIONS.ADMIN_USUARIOS_EDITAR]: 'Editar usuarios',
  [PERMISSIONS.ADMIN_USUARIOS_ACTIVAR]: 'Activar usuarios',
  [PERMISSIONS.ADMIN_USUARIOS_DESACTIVAR]: 'Desactivar usuarios',
  [PERMISSIONS.ADMIN_USUARIOS_RESET_PASSWORD]: 'Restablecer contraseñas de usuarios',
  [PERMISSIONS.ADMIN_USUARIOS_BLOQUEAR]: 'Bloquear usuarios',
  [PERMISSIONS.ADMIN_DEPARTAMENTOS_VER]: 'Ver departamentos',
  [PERMISSIONS.ADMIN_DEPARTAMENTOS_EDITAR]: 'Editar departamentos',
  [PERMISSIONS.ADMIN_ROLES_VER]: 'Ver roles',
  [PERMISSIONS.ADMIN_ROLES_CREAR]: 'Crear roles',
  [PERMISSIONS.ADMIN_ROLES_EDITAR]: 'Editar roles',
  [PERMISSIONS.ADMIN_ROLES_ELIMINAR]: 'Eliminar roles',
  [PERMISSIONS.ADMIN_ROLES_PERMISOS]: 'Gestionar permisos de roles',
  [PERMISSIONS.ADMIN_CATALOGOS_VER]: 'Ver catálogos',
  [PERMISSIONS.ADMIN_CATALOGOS_EDITAR]: 'Editar catálogos',
  [PERMISSIONS.ADMIN_NUMERACION_VER]: 'Ver numeración',
  [PERMISSIONS.ADMIN_NUMERACION_EDITAR]: 'Editar numeración',
  [PERMISSIONS.ADMIN_PLANTILLAS_NUMERACION_VER]: 'Ver plantillas de numeración',
  [PERMISSIONS.ADMIN_PLANTILLAS_NUMERACION_EDITAR]: 'Editar plantillas de numeración',
  [PERMISSIONS.ADMIN_CONFIG_VER]: 'Ver configuración',
  [PERMISSIONS.ADMIN_CONFIG_EDITAR]: 'Editar configuración',
  [PERMISSIONS.ADMIN_INTEGRACIONES_VER]: 'Ver integraciones',
  [PERMISSIONS.ADMIN_INTEGRACIONES_EDITAR]: 'Editar integraciones',
  [PERMISSIONS.ADMIN_AUDITORIA_VER]: 'Ver auditoría',
  [PERMISSIONS.ADMIN_RESPALDOS_VER]: 'Ver respaldos',
  [PERMISSIONS.ADMIN_RESPALDOS_CREAR]: 'Crear respaldos',
  [PERMISSIONS.ADMIN_RESPALDOS_EDITAR]: 'Editar respaldos',
  [PERMISSIONS.ADMIN_RESPALDOS_DESCARGAR]: 'Descargar respaldos',
  [PERMISSIONS.ADMIN_RESPALDOS_RESTAURAR]: 'Restaurar respaldos',
  [PERMISSIONS.ADMIN_RESPALDOS_CONFIGURAR]: 'Configurar respaldos',
};
