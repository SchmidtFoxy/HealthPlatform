// Notificacoes push. A inscricao fica no service worker da raiz (/sw.js), que e quem mostra as
// notificacoes e trata o clique; assim a mesma inscricao vale para a versao nova e para a classica.
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

/** Para onde vai o clique de uma notificacao. Paciente abre tudo no /app; o resto ainda vive na classica. */
export function destinoDoLink(link: string, paciente: boolean) {
  const app: Record<string, string> = { chat: 'chat', arquivos: 'arquivos', inicio: 'hoje', alimentacao: 'dieta', treinos: 'treino', cuidados: 'cuidados' };
  if (paciente && Object.prototype.hasOwnProperty.call(app, link)) return `/app/${app[link]}`;
  return `/?aesynPushLink=${encodeURIComponent(link)}`;
}
