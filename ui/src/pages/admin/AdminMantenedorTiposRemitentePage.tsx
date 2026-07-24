import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import { SeremTipoSection } from './AdminRemitentesPage';

export default function AdminMantenedorTiposRemitentePage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Tipo remitente</h2>
        <p className="mt-1 text-sm text-gray-500">Administración de SEREMTIP.</p>
      </div>
      <SeremTipoSection canEdit={canEdit} />
    </div>
  );
}
