import http from '../http';
import type { DepartamentoResumen, UsuarioResumen } from '../../types/catalogo';

export async function getUsuariosCatalogo(): Promise<UsuarioResumen[]> {
  const { data } = await http.get<UsuarioResumen[]>('/catalogos/usuarios');
  return data;
}

export async function getDepartamentosCatalogo(): Promise<DepartamentoResumen[]> {
  const { data } = await http.get<DepartamentoResumen[]>('/catalogos/departamentos');
  return data;
}
