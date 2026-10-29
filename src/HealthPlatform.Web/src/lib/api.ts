// Cliente HTTP do novo front. Usa as mesmas chaves de sessao do front classico
// (hp_token / hp_user / hp_expires_at), entao alternar entre os dois nao pede novo login.

export type TipoUsuario = 'Admin' | 'Medico' | 'Nutricionista' | 'Personal' | 'Secretaria' | 'Paciente';

export interface Sessao {
  token: string;
  nome: string;
  tipoUsuario: TipoUsuario;
  expiresAtUtc: string | null;
  /** Admin vendo o app como um profissional, somente leitura. */
  impersonating?: boolean;
  impersonatorNome?: string | null;
}

interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  nome: string;
  tipoUsuario: TipoUsuario;
  impersonating?: boolean;
  impersonatorId?: string | null;
  impersonatorNome?: string | null;
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

const listeners = new Set<(s: Sessao | null) => void>();

export function lerSessao(): Sessao | null {
  try {
    const token = localStorage.getItem('hp_token');
    const user = JSON.parse(localStorage.getItem('hp_user') || 'null');
    if (!token || !user?.tipoUsuario) return null;
    return { token, nome: user.nome, tipoUsuario: user.tipoUsuario, expiresAtUtc: localStorage.getItem('hp_expires_at'), impersonating: !!user.impersonating, impersonatorNome: user.impersonatorNome ?? null };
  } catch {
    return null;
  }
}

function salvarSessao(r: LoginResponse | null) {
  try {
    if (!r) {
      localStorage.removeItem('hp_token');
      localStorage.removeItem('hp_user');
      localStorage.removeItem('hp_expires_at');
    } else {
      localStorage.setItem('hp_token', r.accessToken);
      // Mesmo formato do front classico, para a simulacao valer nos dois.
      localStorage.setItem(
        'hp_user',
        JSON.stringify({ nome: r.nome, tipoUsuario: r.tipoUsuario, impersonating: !!r.impersonating, impersonatorId: r.impersonatorId ?? null, impersonatorNome: r.impersonatorNome ?? null }),
      );
      localStorage.setItem('hp_expires_at', r.expiresAtUtc || '');
    }
  } catch {
    /* armazenamento indisponivel: a sessao vale so para esta aba */
  }
  const s = lerSessao();
  listeners.forEach((l) => l(s));
}

/** Atualiza o nome guardado na sessao (depois de editar a conta). */
export function renomearSessao(nome: string) {
  try {
    const u = JSON.parse(localStorage.getItem('hp_user') || 'null');
    if (u) localStorage.setItem('hp_user', JSON.stringify({ ...u, nome }));
  } catch {
    /* sem armazenamento */
  }
  const s = lerSessao();
  listeners.forEach((l) => l(s));
}

export function aoMudarSessao(fn: (s: Sessao | null) => void) {
  listeners.add(fn);
  return () => listeners.delete(fn);
}

let renovando: Promise<boolean> | null = null;

async function renovar(): Promise<boolean> {
  const atual = lerSessao();
  // A simulacao e somente leitura: o servidor recusa renovar, entao ela termina quando o token vence.
  if (!atual || atual.impersonating) return false;
  renovando ??= fetch('/api/auth/renovar', { method: 'POST', headers: { Authorization: `Bearer ${atual.token}` } })
    .then(async (r) => {
      if (!r.ok) return false;
      salvarSessao(await r.json());
      return true;
    })
    .catch(() => false)
    .finally(() => {
      renovando = null;
    });
  return renovando;
}

export async function api<T>(path: string, init: RequestInit & { json?: unknown } = {}, tentativa = 0): Promise<T> {
  const sessao = lerSessao();
  const headers = new Headers(init.headers);
  if (sessao) headers.set('Authorization', `Bearer ${sessao.token}`);
  let body = init.body;
  if (init.json !== undefined) {
    headers.set('Content-Type', 'application/json');
    body = JSON.stringify(init.json);
  }
  const res = await fetch(path, { ...init, headers, body });
  if (res.status === 401 && path !== '/api/auth/login') {
    if (tentativa === 0 && (await renovar())) return api<T>(path, init, 1);
    salvarSessao(null);
    throw new ApiError(401, 'Sua sessão expirou. Entre novamente.');
  }
  const text = await res.text();
  let data: unknown = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  if (!res.ok) {
    const corpo = (data && typeof data === 'object' ? data : {}) as { message?: unknown; title?: unknown };
    const msg = corpo.message ?? corpo.title ?? 'Não foi possível concluir. Tente de novo.';
    throw new ApiError(res.status, String(msg));
  }
  return data as T;
}

export async function entrar(email: string, senha: string) {
  const r = await api<LoginResponse>('/api/auth/login', { method: 'POST', json: { email, senha } });
  salvarSessao(r);
  lembrarConta({ email, nome: r.nome, tipoUsuario: r.tipoUsuario });
  return lerSessao()!;
}

export async function sair() {
  // Na simulacao, o logout de verdade e o da conta do admin.
  if (lerSessao()?.impersonating) await encerrarSimulacao();
  // Este aparelho para de receber as notificacoes da conta que saiu (com mensagens de saude no texto).
  // A inscricao do navegador fica: a proxima conta que entrar com a permissao dada passa a usa-la.
  try {
    const sub = await (await navigator.serviceWorker?.getRegistration('/'))?.pushManager?.getSubscription();
    if (sub) await api('/api/push/subscriptions', { method: 'DELETE', json: { endpoint: sub.endpoint } });
  } catch {
    /* sem service worker ou sem rede: segue o logout */
  }
  try {
    await api('/api/auth/logout', { method: 'POST' });
  } catch {
    /* sair localmente mesmo se a API nao responder */
  }
  salvarSessao(null);
}

/** Admin passa a ver o app como o profissional escolhido, somente leitura. */
export async function iniciarSimulacao(usuarioId: string) {
  const r = await api<LoginResponse>(`/api/impersonacao/${usuarioId}/iniciar`, { method: 'POST' });
  salvarSessao(r);
}

export async function encerrarSimulacao() {
  try {
    salvarSessao(await api<LoginResponse>('/api/impersonacao/finalizar', { method: 'POST' }));
  } catch {
    salvarSessao(null);
  }
}

export const ehPaciente = (s: Sessao | null) => s?.tipoUsuario === 'Paciente';

export async function pedirNovaSenha(email: string) {
  const r = await api<{ message?: string }>('/api/auth/password/esqueci', { method: 'POST', json: { email } });
  return r?.message ?? 'Se existir uma conta ativa para este e-mail, você receberá as instruções para redefinir sua senha.';
}

// Contas usadas neste aparelho, para trocar de perfil sem digitar tudo de novo. Guarda so e-mail, nome e papel.
export interface ContaLembrada {
  email: string;
  nome: string;
  tipoUsuario: TipoUsuario;
}

const CHAVE_CONTAS = 'aesyn_contas';

export function contasLembradas(): ContaLembrada[] {
  try {
    const v = JSON.parse(localStorage.getItem(CHAVE_CONTAS) || '[]');
    return Array.isArray(v) ? v.filter((c) => c?.email) : [];
  } catch {
    return [];
  }
}

export function lembrarConta(c: ContaLembrada) {
  try {
    const resto = contasLembradas().filter((x) => x.email.toLowerCase() !== c.email.toLowerCase());
    localStorage.setItem(CHAVE_CONTAS, JSON.stringify([c, ...resto].slice(0, 5)));
  } catch {
    /* sem armazenamento: so nao lembra */
  }
}

export function esquecerConta(email: string) {
  try {
    localStorage.setItem(CHAVE_CONTAS, JSON.stringify(contasLembradas().filter((x) => x.email.toLowerCase() !== email.toLowerCase())));
  } catch {
    /* sem armazenamento */
  }
}
