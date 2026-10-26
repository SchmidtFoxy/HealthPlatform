const nf0 = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 });
const nf1 = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 });

export const n0 = (v: number | null | undefined) => (v == null || Number.isNaN(v) ? '—' : nf0.format(v));
export const n1 = (v: number | null | undefined) => (v == null || Number.isNaN(v) ? '—' : nf1.format(v));

export const primeiroNome = (nome?: string | null) => (nome ?? '').trim().split(/\s+/)[0] ?? '';

export const iniciais = (nome?: string | null) =>
  (nome ?? '')
    .trim()
    .split(/\s+/)
    .filter((p) => p.length > 2 || /^[A-Z]/.test(p))
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('');

export const hora = (h?: string | null) => (h ? h.slice(0, 5) : '');

export function saudacao(d = new Date()) {
  const h = d.getHours();
  if (h < 5) return 'Boa madrugada';
  if (h < 12) return 'Bom dia';
  if (h < 18) return 'Boa tarde';
  return 'Boa noite';
}

export const dataLonga = (d = new Date()) => {
  const t = d.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' });
  return t.charAt(0).toUpperCase() + t.slice(1);
};

export const dataCurta = (iso: string) => {
  const d = new Date(iso.length === 10 ? iso + 'T12:00:00' : iso);
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }).replace('.', '');
};

export const diaSemanaCurto = (iso: string) => {
  const d = new Date(iso.length === 10 ? iso + 'T12:00:00' : iso);
  return d.toLocaleDateString('pt-BR', { weekday: 'narrow' });
};

export function idade(nascimento?: string | null) {
  if (!nascimento) return null;
  const n = new Date(nascimento + 'T12:00:00');
  const h = new Date();
  let a = h.getFullYear() - n.getFullYear();
  if (h.getMonth() < n.getMonth() || (h.getMonth() === n.getMonth() && h.getDate() < n.getDate())) a--;
  return a;
}

export function tempoRelativo(iso?: string | null) {
  if (!iso) return 'nunca';
  const diff = Date.now() - new Date(iso).getTime();
  const dias = Math.floor(diff / 86400000);
  if (dias <= 0) return 'hoje';
  if (dias === 1) return 'ontem';
  if (dias < 7) return `há ${dias} dias`;
  if (dias < 30) return `há ${Math.floor(dias / 7)} sem`;
  return `há ${Math.floor(dias / 30)} meses`;
}

export const mmss = (s: number) => `${Math.floor(s / 60)}:${String(Math.max(0, s % 60)).padStart(2, '0')}`;

const TOKENS_DIA: Record<number, string[]> = {
  0: ['domingo', 'dom'],
  1: ['segunda', 'seg'],
  2: ['terça', 'terca', 'ter'],
  3: ['quarta', 'qua'],
  4: ['quinta', 'qui'],
  5: ['sexta', 'sex'],
  6: ['sábado', 'sabado', 'sáb', 'sab'],
};

/** Mesma regra do backend (EstrategiaDiariaService): sessao sem dias definidos vale para qualquer dia. */
export function sessaoEhDoDia(diasSemana: string | null | undefined, d = new Date()) {
  if (!diasSemana?.trim()) return true;
  const alvo = diasSemana.toLowerCase();
  return TOKENS_DIA[d.getDay()].some((t) => alvo.includes(t));
}

export const hojeIso = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

/** Primeiro numero de uma prescricao como "8-10", "12" ou "40s". */
export const repsAlvo = (r: string) => {
  const m = r.match(/\d+/);
  return m ? Number(m[0]) : null;
};
