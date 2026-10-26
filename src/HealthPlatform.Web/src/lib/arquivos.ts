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
export const ACEITOS = 'image/*,application/pdf,.doc,.docx';
export const ACEITOS_DOCUMENTO = 'application/pdf,.doc,.docx,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document';
export const LIMITE_BYTES = 15 * 1024 * 1024;

export const tamanho = (b: number) => (b < 1024 ? `${b} B` : b < 1024 * 1024 ? `${Math.round(b / 1024)} KB` : `${(b / 1024 / 1024).toFixed(1).replace('.', ',')} MB`);

/** Rota da API para o arquivo: paciente usa /api/arquivos/me, equipe usa a do paciente. */
const base = (pacienteId?: string) => (pacienteId ? `/api/pacientes/${pacienteId}/arquivos` : '/api/arquivos/me');

export const listarArquivos = (pacienteId?: string) => api<ArquivoPaciente[]>(base(pacienteId));

const ehImagemArquivo = (f: File) => f.type.startsWith('image/') || /\.(heic|heif|jpe?g|png|webp|gif)$/i.test(f.name);

/**
 * Foto do celular (HEIC do iPhone, foto grande da camera...) vira JPEG de ate 2560 px antes de subir:
 * o servidor so aceita JPEG, PNG e WebP, e o envio fica bem mais leve.
 */
export async function prepararImagem(file: File): Promise<File> {
  if (!ehImagemArquivo(file)) return file;
  const aceita = ['image/jpeg', 'image/png', 'image/webp'].includes(file.type);
  if (aceita && file.size <= 3 * 1024 * 1024) return file;
  const url = URL.createObjectURL(file);
  try {
    const img = new Image();
    img.src = url;
    await img.decode();
    const escala = Math.min(1, 2560 / Math.max(img.naturalWidth, img.naturalHeight));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(img.naturalWidth * escala);
    canvas.height = Math.round(img.naturalHeight * escala);
    canvas.getContext('2d')!.drawImage(img, 0, 0, canvas.width, canvas.height);
    const blob = await new Promise<Blob | null>((ok) => canvas.toBlob(ok, 'image/jpeg', 0.85));
    if (!blob) return file;
    const nome = file.name.replace(/\.[^.]+$/, '') || 'foto';
    return new File([blob], `${nome}.jpg`, { type: 'image/jpeg', lastModified: file.lastModified });
  } catch {
    if (!aceita) throw new Error('Não deu para ler essa foto. Tente tirar um print dela ou escolher outra.');
    return file;
  } finally {
    URL.revokeObjectURL(url);
  }
}

export async function enviarArquivo(arquivo: File, opts: { categoria: string; descricao?: string; viaChat?: boolean; pacienteId?: string }) {
  const file = await prepararImagem(arquivo);
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

/** Baixa o arquivo com o token e devolve uma URL local (blob) para exibir, por exemplo, uma foto. */
export async function urlDoArquivo(id: string, pacienteId?: string) {
  const s = lerSessao();
  const r = await fetch(`${base(pacienteId)}/${id}/download`, { headers: s ? { Authorization: `Bearer ${s.token}` } : {} });
  if (!r.ok) throw new Error('Não foi possível carregar o arquivo.');
  return URL.createObjectURL(await r.blob());
}
