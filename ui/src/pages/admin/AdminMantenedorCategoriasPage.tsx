import { useEffect, useMemo, useState } from 'react';
import { useFieldArray, useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import Button from '../../components/atoms/Button';
import IconButton from '../../components/atoms/IconButton';
import SearchBar from '../../components/molecules/SearchBar';
import FormField from '../../components/molecules/FormField';
import Pagination from '../../components/molecules/Pagination';
import ModalDialog from '../../components/organisms/ModalDialog';
import Spinner from '../../components/atoms/Spinner';
import { useToast } from '../../contexts/ToastContext';
import {
  createCatalogoCategoria,
  createCatalogoSubcategoria,
  deleteCatalogoCategoria,
  deleteCatalogoSubcategoria,
  listCatalogoCategorias,
  listCatalogoSubcategorias,
  updateCatalogoCategoria,
  updateCatalogoSubcategoria,
  type CatalogoCategoriaDto,
  type CatalogoSubcategoriaDto,
} from '../../lib/api/admin/adminCatalogosApi';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

function matchesSearch(search: string, values: Array<string | number | null | undefined>) {
  const q = search.trim().toLowerCase();
  if (!q) return true;
  return values.some((value) => String(value ?? '').toLowerCase().includes(q));
}

const initialSubcategorySchema = z.object({
  subcatNombre: z.string().min(1, 'El nombre es obligatorio').max(200, 'Máximo 200 caracteres'),
});

const categorySchema = z.object({
  catDesc: z.string().min(1, 'La descripción es obligatoria').max(60, 'Máximo 60 caracteres'),
  initialSubcategories: z.array(initialSubcategorySchema).default([]),
});

const categoryEditSchema = z.object({
  catCod: z.coerce.number().int().positive('El código debe ser mayor a 0'),
  catDesc: z.string().min(1, 'La descripción es obligatoria').max(60, 'Máximo 60 caracteres'),
});

const subcategorySchema = z.object({
  subcatNombre: z.string().min(1, 'El nombre es obligatorio').max(200, 'Máximo 200 caracteres'),
});

type CategoryFormData = z.infer<typeof categorySchema>;
type CategoryEditFormData = z.infer<typeof categoryEditSchema>;
type SubcategoryFormData = z.infer<typeof subcategorySchema>;

export default function AdminMantenedorCategoriasPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);
  const qc = useQueryClient();
  const toast = useToast();
  const [selectedCategoryCod, setSelectedCategoryCod] = useState<number | null>(null);
  const [categoryModal, setCategoryModal] = useState<'crear' | 'editar' | null>(null);
  const [subcategoryModal, setSubcategoryModal] = useState<'crear' | 'editar' | null>(null);
  const [editingCategory, setEditingCategory] = useState<CatalogoCategoriaDto | null>(null);
  const [editingSubcategory, setEditingSubcategory] = useState<CatalogoSubcategoriaDto | null>(null);
  const [deletingCategory, setDeletingCategory] = useState<CatalogoCategoriaDto | null>(null);
  const [deletingSubcategory, setDeletingSubcategory] = useState<CatalogoSubcategoriaDto | null>(null);
  const [search, setSearch] = useState('');
  const [searchSub, setSearchSub] = useState('');

  const categoryForm = useForm<CategoryFormData>({
    resolver: zodResolver(categorySchema) as Resolver<CategoryFormData>,
    defaultValues: { catDesc: '', initialSubcategories: [] },
  });
  const categoryEditForm = useForm<CategoryEditFormData>({
    resolver: zodResolver(categoryEditSchema) as Resolver<CategoryEditFormData>,
    defaultValues: { catCod: 0, catDesc: '' },
  });
  const subcategoryForm = useForm<SubcategoryFormData>({
    resolver: zodResolver(subcategorySchema) as Resolver<SubcategoryFormData>,
    defaultValues: { subcatNombre: '' },
  });
  const initialSubcategories = useFieldArray({
    control: categoryForm.control,
    name: 'initialSubcategories',
  });

  const { data: categories = [], isLoading: categoriesLoading, isError: categoriesError } = useQuery({
    queryKey: ['admin-catalogos', 'categorias'],
    queryFn: listCatalogoCategorias,
  });
  const filteredCategories = useMemo(
    () => categories.filter((item) => matchesSearch(search, [item.catCod, item.catDesc])),
    [categories, search],
  );

  const [paginaCat, setPaginaCat] = useState(1);
  const [tamanoPaginaCat, setTamanoPaginaCat] = useState(20);
  const totalCategorias = filteredCategories.length;
  const totalPaginasCat = Math.max(1, Math.ceil(totalCategorias / tamanoPaginaCat));

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(totalCategorias / tamanoPaginaCat));
    setPaginaCat((current) => Math.min(current, maxPagina));
  }, [totalCategorias, tamanoPaginaCat]);

  const pagedCategories = filteredCategories.slice((paginaCat - 1) * tamanoPaginaCat, paginaCat * tamanoPaginaCat);

  const selectedCategory = categories.find((item) => item.catCod === selectedCategoryCod) ?? categories[0] ?? null;
  const activeCategoryCod = selectedCategory?.catCod;
  const { data: subcategories = [], isLoading: subcategoriesLoading, isError: subcategoriesError } = useQuery({
    queryKey: ['admin-catalogos', 'subcategorias', activeCategoryCod ?? 'none'],
    queryFn: () => listCatalogoSubcategorias(activeCategoryCod ?? undefined),
    enabled: activeCategoryCod !== undefined,
  });

  const subQuery = searchSub.trim().toLowerCase();
  const filteredSubcategories = subcategories.filter(
    (s) => !subQuery
      || s.subcatNombre.toLowerCase().includes(subQuery)
      || String(s.idSubcategoria).includes(subQuery),
  );

  const [paginaSub, setPaginaSub] = useState(1);
  const [tamanoPaginaSub, setTamanoPaginaSub] = useState(20);
  const totalSubcategorias = filteredSubcategories.length;
  const totalPaginasSub = Math.max(1, Math.ceil(totalSubcategorias / tamanoPaginaSub));

  // Reset the subcategory page when the selected category changes.
  useEffect(() => {
    setPaginaSub(1);
  }, [activeCategoryCod]);

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(totalSubcategorias / tamanoPaginaSub));
    setPaginaSub((current) => Math.min(current, maxPagina));
  }, [totalSubcategorias, tamanoPaginaSub]);

  const pagedSubcategories = filteredSubcategories.slice((paginaSub - 1) * tamanoPaginaSub, paginaSub * tamanoPaginaSub);

  const createCategoryMut = useMutation({
    mutationFn: async (values: CategoryFormData) => {
      const created = await createCatalogoCategoria({ catDesc: values.catDesc });
      for (const subcat of values.initialSubcategories) {
        await createCatalogoSubcategoria({
          catCod: created.catCod,
          subcatNombre: subcat.subcatNombre,
          subcatDescripcion: null,
        });
      }
      return created.catCod;
    },
    onSuccess: async (catCod) => {
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] });
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] });
      setCategoryModal(null);
      setSelectedCategoryCod(catCod);
      toast.success('Categoría creada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la categoría.')),
  });

  const updateCategoryMut = useMutation({
    mutationFn: ({ catCod, catDesc }: { catCod: number; catDesc: string }) => updateCatalogoCategoria(catCod, { catDesc }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] });
      setCategoryModal(null);
      toast.success('Categoría actualizada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la categoría.')),
  });

  const deleteCategoryMut = useMutation({
    mutationFn: (catCod: number) => deleteCatalogoCategoria(catCod),
    onSuccess: async (_data, catCod) => {
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] });
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] });
      setDeletingCategory(null);
      setSelectedCategoryCod((current) => (current === catCod ? (categories.find((item) => item.catCod !== catCod)?.catCod ?? null) : current));
      toast.success('Categoría eliminada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la categoría.')),
  });

  const createSubcategoryMut = useMutation({
    mutationFn: (values: { catCod: number; subcatNombre: string; subcatDescripcion?: string | null }) =>
      createCatalogoSubcategoria(values),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] });
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] });
      setSubcategoryModal(null);
      toast.success('Subcategoría creada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la subcategoría.')),
  });

  const updateSubcategoryMut = useMutation({
    mutationFn: ({ catCod, idSubcategoria, subcatNombre, subcatDescripcion }: { catCod: number; idSubcategoria: number; subcatNombre: string; subcatDescripcion?: string | null }) =>
      updateCatalogoSubcategoria(catCod, idSubcategoria, { subcatNombre, subcatDescripcion }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] });
      setSubcategoryModal(null);
      toast.success('Subcategoría actualizada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la subcategoría.')),
  });

  const deleteSubcategoryMut = useMutation({
    mutationFn: ({ catCod, idSubcategoria }: { catCod: number; idSubcategoria: number }) => deleteCatalogoSubcategoria(catCod, idSubcategoria),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] });
      await qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] });
      setDeletingSubcategory(null);
      toast.success('Subcategoría eliminada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la subcategoría.')),
  });

  const selectedSubcategoryLabel = useMemo(() => selectedCategory ? `${selectedCategory.catCod} — ${selectedCategory.catDesc}` : 'Seleccioná una categoría', [selectedCategory]);

  function openCreateCategory() {
    categoryForm.reset({ catDesc: '', initialSubcategories: [] });
    initialSubcategories.replace([]);
    setEditingCategory(null);
    setCategoryModal('crear');
  }

  function openEditCategory(item: CatalogoCategoriaDto) {
    categoryEditForm.reset({ catCod: item.catCod, catDesc: item.catDesc });
    setEditingCategory(item);
    setCategoryModal('editar');
  }

  function openCreateSubcategory() {
    if (!selectedCategory) return;
    subcategoryForm.reset({ subcatNombre: '' });
    setEditingSubcategory(null);
    setSubcategoryModal('crear');
  }

  function openEditSubcategory(item: CatalogoSubcategoriaDto) {
    subcategoryForm.reset({ subcatNombre: item.subcatNombre });
    setEditingSubcategory(item);
    setSubcategoryModal('editar');
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Categorías</h2>
        <p className="mt-1 text-sm text-gray-500">SECATALO y SESUBCATEGORIAS en un solo flujo maestro-detalle.</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
          <div className="mb-4 flex items-center justify-between gap-3">
            <div>
              <h3 className="text-base font-semibold text-gray-800">Categorías</h3>
              <p className="text-sm text-gray-500">Seleccioná una categoría para ver sus subcategorías.</p>
            </div>
            {canEdit && <Button onClick={openCreateCategory}>+ Crear categoría</Button>}
          </div>

          {categoriesLoading ? (
            <div className="flex justify-center py-10"><Spinner size="md" /></div>
          ) : categoriesError ? (
            <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">No se pudieron cargar las categorías.</p>
          ) : (
            <>
              <div className="mb-3 max-w-sm">
                <SearchBar value={search} onChange={setSearch} placeholder="Buscar categorías..." />
              </div>
              <div className="overflow-x-auto rounded border border-gray-200">
                <table className="min-w-full text-sm">
                  <thead className="bg-gray-50 text-xs uppercase text-gray-500">
                    <tr>
                      <th className="px-4 py-2 text-left">Código</th>
                      <th className="px-4 py-2 text-left">Descripción</th>
                      <th className="px-4 py-2 text-left">Subcategorías</th>
                      <th className="px-4 py-2 text-left">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {pagedCategories.map((item) => (
                      <tr key={item.catCod} className={selectedCategory?.catCod === item.catCod ? 'bg-blue-50' : 'hover:bg-gray-50'} onClick={() => setSelectedCategoryCod(item.catCod)}>
                        <td className="px-4 py-2">{item.catCod}</td>
                        <td className="px-4 py-2">{item.catDesc}</td>
                        <td className="px-4 py-2">{item.totalSubcategorias}</td>
                        <td className="px-4 py-2">
                          {canEdit && (
                            <div className="flex gap-1">
                              <IconButton name="edit" tooltip="Editar" appearance="admin" onClick={(e) => { e.stopPropagation(); openEditCategory(item); }} />
                              <IconButton name="trash" tooltip="Eliminar" variant="danger" appearance="admin" onClick={(e) => { e.stopPropagation(); setDeletingCategory(item); }} />
                            </div>
                          )}
                        </td>
                      </tr>
                    ))}
                  {filteredCategories.length === 0 && (
                    <tr><td colSpan={4} className="px-4 py-8 text-center text-sm text-gray-500">{search.trim() ? 'No se encontraron categorías.' : 'No hay categorías cargadas.'}</td></tr>
                  )}
                  </tbody>
                </table>
                {totalCategorias > 0 && (
                  <Pagination
                    pagina={paginaCat}
                    totalPaginas={totalPaginasCat}
                    totalItems={totalCategorias}
                    tamanoPagina={tamanoPaginaCat}
                    onChange={setPaginaCat}
                    onTamanoPaginaChange={(tamano) => { setTamanoPaginaCat(tamano); setPaginaCat(1); }}
                  />
                )}
              </div>
            </>
          )}
        </section>

        <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
          <div className="mb-4 flex items-center justify-between gap-3">
            <div>
              <h3 className="text-base font-semibold text-gray-800">Subcategorías</h3>
              <p className="text-sm text-gray-500">{selectedSubcategoryLabel}</p>
            </div>
            {canEdit && <Button onClick={openCreateSubcategory} disabled={!selectedCategory}>+ Crear subcategoría</Button>}
          </div>

          {subcategoriesLoading ? (
            <div className="flex justify-center py-10"><Spinner size="md" /></div>
          ) : subcategoriesError ? (
            <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">No se pudieron cargar las subcategorías.</p>
          ) : !selectedCategory ? (
            <p className="rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">Elegí una categoría para ver su detalle.</p>
          ) : (
            <>
              <div className="mb-3">
                <input
                  type="text"
                  value={searchSub}
                  onChange={(e) => setSearchSub(e.target.value)}
                  placeholder="Buscar subcategoría por nombre"
                  className="w-full max-w-sm rounded border border-gray-300 px-3 py-2 text-sm"
                />
              </div>
              <div className="overflow-x-auto rounded border border-gray-200">
                <table className="min-w-full text-sm">
                  <thead className="bg-gray-50 text-xs uppercase text-gray-500">
                    <tr>
                      <th className="px-4 py-2 text-left">ID</th>
                      <th className="px-4 py-2 text-left">Nombre</th>
                      <th className="px-4 py-2 text-left">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {pagedSubcategories.map((item) => (
                      <tr key={`${item.catCod}-${item.idSubcategoria}`}>
                        <td className="px-4 py-2">{item.idSubcategoria}</td>
                        <td className="px-4 py-2">{item.subcatNombre}</td>
                        <td className="px-4 py-2">
                          {canEdit && (
                            <div className="flex gap-1">
                              <IconButton name="edit" tooltip="Editar" appearance="admin" onClick={() => openEditSubcategory(item)} />
                              <IconButton name="trash" tooltip="Eliminar" variant="danger" appearance="admin" onClick={() => setDeletingSubcategory(item)} />
                            </div>
                          )}
                        </td>
                      </tr>
                    ))}
                    {filteredSubcategories.length === 0 && (
                      <tr><td colSpan={3} className="px-4 py-8 text-center text-sm text-gray-500">
                        {subQuery ? 'No se encontraron subcategorías para la búsqueda.' : 'No hay subcategorías cargadas.'}
                      </td></tr>
                    )}
                  </tbody>
                </table>
                {totalSubcategorias > 0 && (
                  <Pagination
                    pagina={paginaSub}
                    totalPaginas={totalPaginasSub}
                    totalItems={totalSubcategorias}
                    tamanoPagina={tamanoPaginaSub}
                    onChange={setPaginaSub}
                    onTamanoPaginaChange={(tamano) => { setTamanoPaginaSub(tamano); setPaginaSub(1); }}
                  />
                )}
              </div>
            </>
          )}
        </section>
      </div>

      {categoryModal === 'crear' && (
        <ModalDialog
          open
          title="Crear categoría"
          size="lg"
          onClose={() => setCategoryModal(null)}
          footer={<><Button variant="secondary" onClick={() => setCategoryModal(null)}>Cancelar</Button><Button type="submit" form="category-form" loading={createCategoryMut.isPending}>Guardar</Button></>}
        >
          <form id="category-form" onSubmit={categoryForm.handleSubmit((values: CategoryFormData) => createCategoryMut.mutate(values))} className="space-y-4">
            <FormField label="Descripción" error={categoryForm.formState.errors.catDesc?.message}><input {...categoryForm.register('catDesc')} maxLength={60} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>

            <div className="rounded border border-gray-200 bg-gray-50 p-3">
              <div className="mb-3 flex items-center justify-between gap-3">
                <div>
                  <h4 className="text-sm font-semibold text-gray-800">Subcategorías iniciales</h4>
                  <p className="text-xs text-gray-500">Opcional: agregá subcategorías en el mismo alta.</p>
                </div>
                <Button type="button" variant="secondary" onClick={() => initialSubcategories.append({ subcatNombre: '' })}>+ Agregar fila</Button>
              </div>
              <div className="space-y-3">
                {initialSubcategories.fields.map((field, index) => (
                  <div key={field.id} className="grid gap-3 rounded border border-gray-200 bg-white p-3 md:grid-cols-2">
                    <FormField label="Nombre" error={categoryForm.formState.errors.initialSubcategories?.[index]?.subcatNombre?.message}><input {...categoryForm.register(`initialSubcategories.${index}.subcatNombre` as const)} maxLength={200} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
                      <div className="md:col-span-2 flex justify-end">
                        <Button type="button" variant="danger" onClick={() => initialSubcategories.remove(index)}>Quitar</Button>
                      </div>
                  </div>
                ))}
                {initialSubcategories.fields.length === 0 && <p className="text-xs text-gray-500">Sin subcategorías iniciales.</p>}
              </div>
            </div>
          </form>
        </ModalDialog>
      )}

      {categoryModal === 'editar' && editingCategory && (
        <ModalDialog
          open
          title="Editar categoría"
          onClose={() => setCategoryModal(null)}
          footer={<><Button variant="secondary" onClick={() => setCategoryModal(null)}>Cancelar</Button><Button type="submit" form="category-edit-form" loading={updateCategoryMut.isPending}>Guardar</Button></>}
        >
          <form id="category-edit-form" onSubmit={categoryEditForm.handleSubmit((values: CategoryEditFormData) => updateCategoryMut.mutate(values))} className="space-y-3">
            <FormField label="Código"><input {...categoryEditForm.register('catCod', { valueAsNumber: true })} disabled className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100" /></FormField>
            <FormField label="Descripción" error={categoryEditForm.formState.errors.catDesc?.message}><input {...categoryEditForm.register('catDesc')} maxLength={60} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
          </form>
        </ModalDialog>
      )}

      {subcategoryModal === 'crear' && selectedCategory && (
        <ModalDialog
          open
          title={`Crear subcategoría en ${selectedCategory.catDesc}`}
          onClose={() => setSubcategoryModal(null)}
          footer={<><Button variant="secondary" onClick={() => setSubcategoryModal(null)}>Cancelar</Button><Button type="submit" form="subcategory-form" loading={createSubcategoryMut.isPending}>Guardar</Button></>}
        >
          <form id="subcategory-form" onSubmit={subcategoryForm.handleSubmit((values: SubcategoryFormData) => createSubcategoryMut.mutate({ catCod: selectedCategory.catCod, subcatNombre: values.subcatNombre, subcatDescripcion: null }))} className="space-y-3">
            <FormField label="Nombre" error={subcategoryForm.formState.errors.subcatNombre?.message}><input {...subcategoryForm.register('subcatNombre')} maxLength={200} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
          </form>
        </ModalDialog>
      )}

      {subcategoryModal === 'editar' && editingSubcategory && (
        <ModalDialog
          open
          title="Editar subcategoría"
          onClose={() => setSubcategoryModal(null)}
          footer={<><Button variant="secondary" onClick={() => setSubcategoryModal(null)}>Cancelar</Button><Button type="submit" form="subcategory-edit-form" loading={updateSubcategoryMut.isPending}>Guardar</Button></>}
        >
          <form id="subcategory-edit-form" onSubmit={subcategoryForm.handleSubmit((values: SubcategoryFormData) => updateSubcategoryMut.mutate({ catCod: editingSubcategory.catCod, idSubcategoria: editingSubcategory.idSubcategoria, subcatNombre: values.subcatNombre, subcatDescripcion: null }))} className="space-y-3">
            <FormField label="ID"><input value={editingSubcategory.idSubcategoria} disabled className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100" /></FormField>
            <FormField label="Nombre" error={subcategoryForm.formState.errors.subcatNombre?.message}><input {...subcategoryForm.register('subcatNombre')} maxLength={200} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
          </form>
        </ModalDialog>
      )}

      <ModalDialog
        open={deletingCategory !== null}
        title="Eliminar categoría"
        onClose={() => setDeletingCategory(null)}
        footer={<><Button variant="secondary" onClick={() => setDeletingCategory(null)}>Cancelar</Button><Button variant="danger" loading={deleteCategoryMut.isPending} onClick={() => deletingCategory && deleteCategoryMut.mutate(deletingCategory.catCod)}>Eliminar</Button></>}
      >
          <p className="text-sm text-gray-600">Está por eliminarse la categoría <strong>{deletingCategory?.catDesc}</strong>.</p>
      </ModalDialog>

      <ModalDialog
        open={deletingSubcategory !== null}
        title="Eliminar subcategoría"
        onClose={() => setDeletingSubcategory(null)}
        footer={<><Button variant="secondary" onClick={() => setDeletingSubcategory(null)}>Cancelar</Button><Button variant="danger" loading={deleteSubcategoryMut.isPending} onClick={() => deletingSubcategory && deleteSubcategoryMut.mutate({ catCod: deletingSubcategory.catCod, idSubcategoria: deletingSubcategory.idSubcategoria })}>Eliminar</Button></>}
      >
          <p className="text-sm text-gray-600">Está por eliminarse la subcategoría <strong>{deletingSubcategory?.subcatNombre}</strong>.</p>
      </ModalDialog>
    </div>
  );
}
