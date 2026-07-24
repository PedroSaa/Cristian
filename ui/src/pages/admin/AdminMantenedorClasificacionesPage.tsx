import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import { ClasegSection } from './AdminCatalogosPage';

export default function AdminMantenedorClasificacionesPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Clasificaciones</h2>
        <p className="mt-1 text-sm text-gray-500">Administración de SECLASEG.</p>
      </div>
      <ClasegSection canEdit={canEdit} />
    </div>
  );
}
