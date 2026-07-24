import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import { FordocSection } from './AdminCatalogosPage';

export default function AdminMantenedorFormatosDocumentoPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Formatos de documento</h2>
        <p className="mt-1 text-sm text-gray-500">Administración de SEFORDOC.</p>
      </div>
      <FordocSection canEdit={canEdit} />
    </div>
  );
}
