// Notificacoes push. A inscricao fica no service worker da raiz (/sw.js), que e quem mostra as
// notificacoes e trata o clique (o codigo de push fica em /push-sw.js, importado pelo service worker do app).
import { api } from './api';

export interface PushStatus {
  habilitadoNoServidor: boolean;
  configurado: boolean;
  publicKey: string | null;
  dispositivos: number;
}

export const pushSuportado = () => 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window && window.isSecureContext;

const chave = (v: string) => {
  const b = (v + '='.repeat((4 - (v.length % 4)) % 4)).replace(/-/g, '+').replace(/_/g, '/');
  return Uint8Array.from(atob(b), (c) => c.charCodeAt(0));
};

async function registroRaiz() {
  const atual = await navigator.serviceWorker.getRegistration('/');
  const reg = atual && atual.scope === `${location.origin}/` ? atual : await navigator.serviceWorker.register('/sw.js', { scope: '/' });
  if (reg.active) return reg;
  const w = reg.installing ?? reg.waiting;
  await new Promise<void>((ok) => {
    if (!w) return ok();
    w.addEventListener('statechange', () => w.state === 'activated' && ok());
    setTimeout(ok, 8000);
  });
  return reg;
}

export const statusPush = () => api<PushStatus>('/api/push/status');

export async function inscricaoAtual() {
  if (!pushSuportado()) return null;
  const reg = await navigator.serviceWorker.getRegistration('/');
  if (!reg || reg.scope !== `${location.origin}/`) return null;
  return reg.pushManager.getSubscription();
}

export async function ativarPush() {
  if (!pushSuportado()) throw new Error('Este navegador não recebe notificações. No iPhone, instale o AESYN na tela de início primeiro.');
  const permissao = await Notification.requestPermission();
  if (permissao !== 'granted') throw new Error('Permissão de notificações negada. Libere nas configurações do navegador.');
  const st = await statusPush();
  if (!st.habilitadoNoServidor || !st.publicKey) throw new Error('Notificações ainda não estão ligadas no servidor.');
  const reg = await registroRaiz();
  const sub = (await reg.pushManager.getSubscription()) ?? (await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: chave(st.publicKey) }));
  const j = sub.toJSON();
  await api('/api/push/subscriptions', { method: 'POST', json: { endpoint: sub.endpoint, p256Dh: j.keys?.p256dh, auth: j.keys?.auth, userAgent: navigator.userAgent } });
  return sub;
}

export async function desativarPush() {
  const sub = await inscricaoAtual();
  if (!sub) return;
  await api('/api/push/subscriptions', { method: 'DELETE', json: { endpoint: sub.endpoint } });
  await sub.unsubscribe();
}

export const testarPush = () => api<{ enviadas: number; motivo?: string }>('/api/push/test', { method: 'POST' });

/** Para onde vai o clique de uma notificacao. Link sem tela propria abre o inicio. */
export function destinoDoLink(link: string, paciente: boolean) {
  const app: Record<string, string> = { chat: 'chat', arquivos: 'arquivos', inicio: 'hoje', alimentacao: 'dieta', treinos: 'treino', cuidados: 'cuidados' };
  if (paciente && Object.prototype.hasOwnProperty.call(app, link)) return `/${app[link]}`;
  const conversa = /^chat-profissional:([0-9a-f-]{36})$/i.exec(link);
  if (!paciente && conversa) return `/pro/mensagens/${conversa[1]}`;
  const ficha = /^paciente:([0-9a-f-]{36})$/i.exec(link);
  if (!paciente && ficha) return `/pro/pacientes/${ficha[1]}`;
  const pro: Record<string, string> = { followups: 'pro/central?aba=contatos', pendencias: 'pro/central?aba=pendencias', solicitacoes: 'pro/central?aba=pedidos', agenda: 'pro/agenda', inicio: 'pro' };
  if (!paciente && Object.prototype.hasOwnProperty.call(pro, link)) return `/${pro[link]}`;
  return '/';
}

/**
 * Com a permissao ja dada, garante que o servidor conhece a inscricao deste aparelho (sem perguntar nada).
 * Cobre a inscricao renovada pelo navegador e o aparelho que entrou com outra conta.
 */
export async function sincronizarPush() {
  if (!pushSuportado() || Notification.permission !== 'granted') return;
  const st = await statusPush();
  if (!st.habilitadoNoServidor || !st.publicKey) return;
  await ativarPush();
}

/** Tira da bandeja as notificacoes de uma conversa que acabou de ser aberta. */
export async function limparNotificacoesDaConversa(link: string) {
  if (!('serviceWorker' in navigator)) return;
  const reg = await navigator.serviceWorker.getRegistration('/');
  (await reg?.getNotifications({ tag: `aesyn-${link}` }))?.forEach((n) => n.close());
}
