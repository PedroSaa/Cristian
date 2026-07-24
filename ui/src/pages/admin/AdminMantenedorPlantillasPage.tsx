import AdminPlantillasSection from './AdminPlantillasSection';

export default function AdminMantenedorPlantillasPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Plantillas</h2>
        <p className="mt-1 text-sm text-gray-500">Administración de SEFORPLA.</p>
      </div>
      <AdminPlantillasSection />
    </div>
  );
}
