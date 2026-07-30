export interface UsuarioResumen {
  id: string;
  nombreCompleto: string;
  email: string;
  rol: string;
  departamentoId: string | null;
}

export interface DepartamentoResumen {
  id: string;
  nombre: string;
  codigo: string;
}
