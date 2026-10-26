// Arquivos do paciente (exames, laudos, fotos...). Mesma API que a versao classica usa.
import { api, lerSessao } from './api';

export interface ArquivoPaciente {
  id: string;
  nome: string;
  contentType: string;
  tamanhoBytes: number;
  categoria: string;
  descricao: string | null;
  tags: string[];
  dataUtc: string;
  origem: 'Paciente' | 'Profissional' | string;
  downloadUrl: string;
}

export const CATEGORIAS = ['Exame', 'Laudo', 'Foto', 'Receita', 'Documento', 'Outro'] as const;
export const ACEITOS = 'application/pdf,image/jpeg,image/png,image/webp,.doc,.docx';
export const LIMITE_BYTES = 15 * 1024 * 1024;

export const tamanho = (b: number) => (b < 1024 ? `${b} B` : b < 1024 * 1024 ? `${Math.round(b / 1024)} KB` : `${(b / 1024 / 1024).toFixed(1).replace('.', ',')} MB`);

/** Rota da API para o arquivo: paciente usa /api/arquivos/me, equipe usa a do paciente. */
const base = (pacienteId?: string) => (pacienteId ? `/api/pacientes/${pacienteId}/arquivos` : '/api/arquivos/me');

export const listarArquivos = (pacienteId?: string) => api<ArquivoPaciente[]>(base(pacienteId));

export async function enviarArquivo(file: File, opts: { categoria: string; descricao?: string; viaChat?: boolean; pacienteId?: string }) {
  if (file.size > LIMITE_BYTES) throw new Error('O arquivo deve ter no máximo 15 MB.');
  const fd = new FormData();
  fd.append('file', file);
  fd.append('categoria', opts.categoria);
  if (opts.descricao) fd.append('descricao', opts.descricao);
  fd.append('viaChat', String(!!opts.viaChat));
  return api<ArquivoPaciente>(`${base(opts.pacienteId)}/upload`, { method: 'POST', body: fd });
}

export const removerArquivo = (id: string, pacienteId?: string) => api(`${base(pacienteId)}/${id}`, { method: 'DELETE' });

/** O download pede o token, entao baixa como blob e abre numa aba (ou salva, se o navegador bloquear). */
export async function abrirArquivo(a: Pick<ArquivoPaciente, 'id' | 'nome'>, pacienteId?: string) {
  const aba = window.open('', '_blank');
  const s = lerSessao();
  const r = await fetch(`${base(pacienteId)}/${a.id}/download`, { headers: s ? { Authorization: `Bearer ${s.token}` } : {} });
  if (!r.ok) {
    aba?.close();
    throw new Error('Não foi possível abrir o arquivo.');
  }
  const url = URL.createObjectURL(await r.blob());
  if (aba) aba.location.href = url;
  else {
    const link = document.createElement('a');
    link.href = url;
    link.download = a.nome;
    link.click();
  }
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
