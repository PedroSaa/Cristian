import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import { TiptarSection } from './AdminCatalogosPage';

export default function AdminMantenedorTareasPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Tareas</h2>
        <p className="mt-1 text-sm text-gray-500">Administración de SETIPTAR.</p>
      </div>
      <TiptarSection canEdit={canEdit} />
    </div>
  );
}
