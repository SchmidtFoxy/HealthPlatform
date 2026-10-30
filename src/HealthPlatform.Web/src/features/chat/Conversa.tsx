import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { AlertCircle, ArrowDown, Bell, Check, CheckCheck, ChevronLeft, Clock3, Copy, FileText, Loader2, Paperclip, Reply, SendHorizontal, ShieldAlert, X, Zap } from 'lucide-react';
import { Fragment, useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ehImagem, ImagemArquivo, VisualizadorImagem } from '@/components/ImagemArquivo';
import { ErrorBox, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { abrirArquivo, enviarArquivo } from '@/lib/arquivos';
import { MenuAnexo } from '@/components/MenuAnexo';
import { iniciais } from '@/lib/format';
import { ativarPush, limparNotificacoesDaConversa, pushSuportado, statusPush } from '@/lib/push';
import { RespostasRapidas } from './RespostasRapidas';

interface Citacao {
  id: string;
  autorTipo: string;
  meu: boolean;
  trecho: string;
}

interface Mensagem {
  id: string;
  dataHoraUtc: string;
  autorTipo: string;
  meu: boolean;
  contexto: string;
  contextoRotulo: string;
  mensagem: string;
  referencia: { tipo: string; id: string | null; titulo: string | null } | null;
  respostaA?: Citacao | null;
  status: string;
  lidaEmUtc: string | null;
  novaAoAbrir?: boolean;
}

interface Conversa {
  pacienteId?: string;
  paciente?: { id: string; nome: string };
  profissional: { id: string; nome: string; especialidade: string | null } | null;
  prazoResposta?: string;
  emergencia?: string;
  mensagens: Mensagem[];
}

/** Mensagem que saiu do aparelho e ainda nao foi confirmada pelo servidor (ou falhou). */
interface Pendente {
  tempId: string;
  dataHoraUtc: string;
  mensagem: string;
  contexto: string;
  respostaA: Citacao | null;
  estado: 'Enviando' | 'Falhou';
}

interface CorpoEnvio {
  mensagem: string;
  contexto: string;
  referenciaTipo?: string;
  referenciaId?: string;
  referenciaTitulo?: string;
  respostaAId?: string;
}

const CONTEXTOS = [
  { v: 'Geral', l: 'Geral' },
  { v: 'Treino', l: 'Treino' },
  { v: 'Nutricao', l: 'Nutrição' },
  { v: 'Recuperacao', l: 'Recuperação' },
  { v: 'Exames', l: 'Exames' },
  { v: 'Medicamentos', l: 'Medicamentos' },
];
const rotuloContexto = (v: string) => (v === 'Medicamentos' ? 'Medicamentos e suplementos' : CONTEXTOS.find((c) => c.v === v)?.l ?? 'Geral');

const diaRotulo = (iso: string) => {
  const d = new Date(iso);
  const hoje = new Date();
  const ontem = new Date(Date.now() - 864e5);
  if (d.toDateString() === hoje.toDateString()) return 'Hoje';
  if (d.toDateString() === ontem.toDateString()) return 'Ontem';
  return d.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'short' });
};
const horaMsg = (iso: string) => new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
const trechoDe = (m: Pick<Mensagem, 'mensagem' | 'referencia'>) =>
  m.referencia?.tipo === 'Arquivo' && m.mensagem === 'Foto' ? '📷 Foto' : m.referencia?.tipo === 'Arquivo' && m.mensagem.startsWith('Enviei o arquivo') ? `📎 ${m.referencia.titulo ?? 'Arquivo'}` : m.mensagem;

// Links no texto viram clicaveis (exames, videos de execucao, etc.).
const URL_RE = /(https?:\/\/[^\s<]+[^\s<.,;:!?)\]'"])/g;
function TextoComLinks({ texto }: { texto: string }) {
  const partes = texto.split(URL_RE);
  return (
    <>
      {partes.map((p, i) =>
        i % 2 === 1 ? (
          <a key={i} href={p} target="_blank" rel="noopener noreferrer" className="underline underline-offset-2 [overflow-wrap:anywhere]" onClick={(e) => e.stopPropagation()}>
            {p}
          </a>
        ) : (
          <Fragment key={i}>{p}</Fragment>
        ),
      )}
    </>
  );
}

function StatusEnvio({ status }: { status: string }) {
  if (status === 'Enviando') return <Clock3 aria-label="Enviando" className="inline h-3.5 w-3.5" />;
  if (status === 'Falhou') return <AlertCircle aria-label="Não enviada" className="inline h-3.5 w-3.5 text-bad" />;
  if (status === 'Lida')
    return (
      <span className="inline-flex items-center gap-0.5 font-semibold text-accent-ink" title="Lida">
        <CheckCheck aria-hidden className="h-3.5 w-3.5" strokeWidth={2.75} />
        lida
      </span>
    );
  if (status === 'Entregue') return <CheckCheck aria-label="Entregue" className="inline h-3.5 w-3.5" />;
  return <Check aria-label="Enviada" className="inline h-3.5 w-3.5" />;
}

const CHAVE_CONVITE_PUSH = 'aesyn-chat-convite-push';

/** Convite para ligar as notificacoes, na propria conversa: sem elas a resposta so aparece com o app aberto. */
function ConvitePush({ pro }: { pro: boolean }) {
  const qc = useQueryClient();
  const [fechado, setFechado] = useState(() => {
    try {
      return localStorage.getItem(CHAVE_CONVITE_PUSH) === '1';
    } catch {
      return false;
    }
  });
  const pode = !fechado && pushSuportado() && Notification.permission === 'default';
  const st = useQuery({ queryKey: ['push', 'status'], queryFn: statusPush, staleTime: 60_000, enabled: pode });
  const ativar = useMutation({
    mutationFn: ativarPush,
    onSuccess: () => {
      toast('Notificações ligadas. Você será avisado das novas mensagens.');
      qc.invalidateQueries({ queryKey: ['push'] });
      setFechado(true);
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível ligar as notificações.', 'bad'),
  });
  if (!pode || !st.data?.habilitadoNoServidor) return null;
  const fechar = () => {
    try {
      localStorage.setItem(CHAVE_CONVITE_PUSH, '1');
    } catch {
      /* sem armazenamento: so esconde agora */
    }
    setFechado(true);
  };
  return (
    <div className="mb-3 flex items-center gap-3 rounded-2xl border border-accent/30 bg-accent/10 px-3.5 py-3 text-[13px]">
      <Bell className="h-5 w-5 shrink-0 text-accent" />
      <p className="min-w-0 flex-1">{pro ? 'Seja avisado quando o paciente responder, mesmo com o app fechado.' : 'Seja avisado quando seu profissional responder, mesmo com o app fechado.'}</p>
      <button onClick={() => ativar.mutate()} disabled={ativar.isPending} className="shrink-0 rounded-full bg-accent px-3 py-1.5 font-semibold text-accent-ink disabled:opacity-60">
        {ativar.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Ativar'}
      </button>
      <button aria-label="Agora não" onClick={fechar} className="-mr-1 grid h-8 w-8 shrink-0 place-items-center rounded-full text-muted hover:bg-raised">
        <X className="h-4 w-4" />
      </button>
    </div>
  );
}

/**
 * Uma bolha com os gestos de app de conversa: arrastar para a direita responde,
 * segurar (ou clique direito) abre as acoes. No computador aparece um botao de responder ao passar o mouse.
 */
function Bolha({ meu, onResponder, onAcoes, children }: { meu: boolean; onResponder?: () => void; onAcoes?: () => void; children: ReactNode }) {
  const toque = useRef<{ x: number; y: number; dx: number; horizontal: boolean | null; timer: number } | null>(null);
  const el = useRef<HTMLDivElement>(null);
  const segurou = useRef(false);
  const soltar = (e: React.TouchEvent) => {
    // Depois de segurar, o toque que solta nao pode virar clique no fundo da folha que acabou de abrir.
    if (segurou.current) {
      segurou.current = false;
      e.preventDefault();
    }
    const t = toque.current;
    if (!t) return;
    window.clearTimeout(t.timer);
    if (el.current) {
      el.current.style.transition = 'transform 180ms ease-out';
      el.current.style.transform = '';
    }
    if (t.horizontal && t.dx > 56) onResponder?.();
    toque.current = null;
  };
  return (
    <div className={clsx('group flex items-center gap-1', meu ? 'flex-row-reverse' : 'flex-row')}>
      <div
        ref={el}
        className="max-w-[82%] touch-pan-y select-none [-webkit-touch-callout:none] md:select-text"
        onContextMenu={(e) => {
          if (!onAcoes) return;
          e.preventDefault();
          onAcoes();
        }}
        onTouchStart={(e) => {
          const p = e.touches[0];
          segurou.current = false;
          toque.current = {
            x: p.clientX,
            y: p.clientY,
            dx: 0,
            horizontal: null,
            timer: window.setTimeout(() => {
              if (toque.current && !toque.current.horizontal) {
                segurou.current = true;
                navigator.vibrate?.(12);
                onAcoes?.();
                toque.current = null;
              }
            }, 480),
          };
          if (el.current) el.current.style.transition = '';
        }}
        onTouchMove={(e) => {
          const t = toque.current;
          if (!t) return;
          const p = e.touches[0];
          const dx = p.clientX - t.x;
          const dy = p.clientY - t.y;
          if (t.horizontal === null && (Math.abs(dx) > 8 || Math.abs(dy) > 8)) {
            t.horizontal = Math.abs(dx) > Math.abs(dy) && dx > 0 && !!onResponder;
            window.clearTimeout(t.timer);
          }
          if (!t.horizontal) return;
          t.dx = Math.max(0, Math.min(dx, 80));
          if (el.current) el.current.style.transform = `translateX(${t.dx}px)`;
        }}
        onTouchEnd={soltar}
        onTouchCancel={soltar}
      >
        {children}
      </div>
      {onResponder && (
        <button
          aria-label="Responder"
          onClick={onResponder}
          className="hidden h-8 w-8 shrink-0 place-items-center rounded-full text-faint opacity-0 transition hover:bg-raised hover:text-fg group-hover:opacity-100 md:grid"
        >
          <Reply className="h-4 w-4" />
        </button>
      )}
    </div>
  );
}

/** Conversa paciente <-> profissional. pacienteId ausente: visao do paciente (/api/chat/me). */
export function Conversa({ pacienteId }: { pacienteId?: string }) {
  const pro = !!pacienteId;
  const base = pro ? `/api/chat/pacientes/${pacienteId}` : '/api/chat/me';
  const chave = pro ? ['pro', 'chat', pacienteId] : ['p', 'chat'];
  const chaveRascunho = `aesyn-chat-rascunho:${pacienteId ?? 'me'}`;
  const nav = useNavigate();
  const qc = useQueryClient();
  const q = useQuery({ queryKey: chave, queryFn: () => api<Conversa>(base), refetchInterval: 8_000, refetchIntervalInBackground: false });
  // O que estava escrito volta se a pessoa sair e entrar de novo na conversa.
  const [texto, setTexto] = useState(() => {
    try {
      return localStorage.getItem(chaveRascunho) ?? '';
    } catch {
      return '';
    }
  });
  const [contexto, setContexto] = useState('Geral');
  const [respondendo, setRespondendo] = useState<Citacao | null>(null);
  const [pendentes, setPendentes] = useState<Pendente[]>([]);
  const [acoes, setAcoes] = useState<Mensagem | null>(null);
  const [destaque, setDestaque] = useState<string | null>(null);
  const [novasAbaixo, setNovasAbaixo] = useState(0);
  const [foto, setFoto] = useState<{ id: string; nome: string } | null>(null);
  const [menuAnexo, setMenuAnexo] = useState(false);
  const [rapidas, setRapidas] = useState(false);
  const rolagem = useRef<HTMLDivElement>(null);
  const fim = useRef<HTMLDivElement>(null);
  const campo = useRef<HTMLTextAreaElement>(null);
  const noFim = useRef(true);
  const ultimoVisto = useRef<string | null | undefined>(undefined);
  const divisorId = useRef<string | null | undefined>(undefined);
  const msgs = q.data?.mensagens ?? [];

  // Onde a leitura parou, guardado so na primeira carga: as respostas que chegarem depois nao mexem nele.
  if (q.data && divisorId.current === undefined) divisorId.current = msgs.find((m) => m.novaAoAbrir && !m.meu)?.id ?? null;

  useEffect(() => {
    try {
      if (texto) localStorage.setItem(chaveRascunho, texto);
      else localStorage.removeItem(chaveRascunho);
    } catch {
      /* sem armazenamento: o rascunho so vale nesta tela */
    }
  }, [texto, chaveRascunho]);

  // Abrir a conversa marca como lida no servidor; o contador e a notificacao na bandeja somem junto.
  useEffect(() => {
    if (!q.data) return;
    qc.invalidateQueries({ queryKey: [pro ? 'pro' : 'p', pro ? 'inbox' : 'chat-nao-lidas'] });
    limparNotificacoesDaConversa(pro ? `chat-profissional:${pacienteId}` : 'chat').catch(() => {});
  }, [q.data, qc, pro, pacienteId]);

  // Rolagem como num app de conversa: so desce sozinho se a pessoa ja esta no fim ou se foi ela que mandou.
  const ultimaChave = pendentes.at(-1)?.tempId ?? msgs.at(-1)?.id ?? null;
  useLayoutEffect(() => {
    if (!q.data) return;
    const anterior = ultimoVisto.current;
    ultimoVisto.current = ultimaChave;
    if (anterior === undefined) {
      const divisor = divisorId.current ? document.getElementById(`novas-${divisorId.current}`) : null;
      if (divisor) divisor.scrollIntoView({ block: 'start' });
      else fim.current?.scrollIntoView({ block: 'end' });
      return;
    }
    if (anterior === ultimaChave) return;
    const minha = pendentes.length > 0 || msgs.at(-1)?.meu;
    if (noFim.current || minha) {
      fim.current?.scrollIntoView({ block: 'end', behavior: 'smooth' });
      return;
    }
    const i = msgs.findIndex((m) => m.id === anterior);
    const chegaram = msgs.slice(i + 1).filter((m) => !m.meu).length;
    if (chegaram) setNovasAbaixo((n) => n + chegaram);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ultimaChave, !!q.data]);

  const aoRolar = () => {
    const el = rolagem.current;
    if (!el) return;
    noFim.current = el.scrollHeight - el.scrollTop - el.clientHeight < 120;
    if (noFim.current && novasAbaixo) setNovasAbaixo(0);
  };
  const descer = () => {
    fim.current?.scrollIntoView({ block: 'end', behavior: 'smooth' });
    setNovasAbaixo(0);
  };

  const postar = (body: CorpoEnvio) => api<Mensagem>(`${base}/mensagens`, { method: 'POST', json: body });
  const guardarNoCache = (m: Mensagem) => {
    qc.setQueryData<Conversa>(chave, (c) => (c && !c.mensagens.some((x) => x.id === m.id) ? { ...c, mensagens: [...c.mensagens, { ...m, meu: true, lidaEmUtc: null }] } : c));
    qc.invalidateQueries({ queryKey: chave });
  };

  // A mensagem aparece na hora; se a rede falhar ela fica marcada e um toque tenta de novo.
  const despachar = async (p: Pendente) => {
    setPendentes((l) => (l.some((x) => x.tempId === p.tempId) ? l.map((x) => (x.tempId === p.tempId ? { ...p, estado: 'Enviando' } : x)) : [...l, p]));
    try {
      const m = await postar({ mensagem: p.mensagem, contexto: p.contexto, respostaAId: p.respostaA?.id });
      guardarNoCache(m);
      setPendentes((l) => l.filter((x) => x.tempId !== p.tempId));
    } catch (e) {
      setPendentes((l) => l.map((x) => (x.tempId === p.tempId ? { ...x, estado: 'Falhou' } : x)));
      toast(e instanceof Error ? e.message : 'Não foi possível enviar.', 'bad');
    }
  };

  const anexar = useMutation({
    mutationFn: async (file: File) => {
      const a = await enviarArquivo(file, { categoria: file.type.startsWith('image/') ? 'Foto' : 'Exame', viaChat: true, pacienteId });
      return postar({
        mensagem: texto.trim() || (file.type.startsWith('image/') ? 'Foto' : `Enviei o arquivo ${a.nome}.`),
        contexto,
        referenciaTipo: 'Arquivo',
        referenciaId: a.id,
        referenciaTitulo: a.nome,
        respostaAId: respondendo?.id,
      });
    },
    onSuccess: (m) => {
      guardarNoCache(m);
      setTexto('');
      setRespondendo(null);
      qc.invalidateQueries({ queryKey: pro ? ['pro', 'arquivos', pacienteId] : ['p', 'arquivos'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível enviar o arquivo.', 'bad'),
  });

  const mandar = () => {
    const m = texto.trim();
    if (!m) return;
    despachar({ tempId: `tmp-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, dataHoraUtc: new Date().toISOString(), mensagem: m, contexto, respostaA: respondendo, estado: 'Enviando' });
    setTexto('');
    setRespondendo(null);
    campo.current?.focus();
  };

  const responder = (m: Mensagem) => {
    setRespondendo({ id: m.id, autorTipo: m.autorTipo, meu: m.meu, trecho: trechoDe(m) });
    campo.current?.focus();
  };

  const irPara = (id: string) => {
    const alvo = document.getElementById(`msg-${id}`);
    if (!alvo) {
      toast('Essa mensagem é antiga e não está mais carregada na conversa.', 'bad');
      return;
    }
    alvo.scrollIntoView({ block: 'center', behavior: 'smooth' });
    setDestaque(id);
    window.setTimeout(() => setDestaque((d) => (d === id ? null : d)), 1600);
  };

  const copiar = async (m: Mensagem) => {
    try {
      await navigator.clipboard.writeText(m.mensagem);
      toast('Mensagem copiada.');
    } catch {
      toast('Não foi possível copiar.', 'bad');
    }
  };

  // Campo cresce com o texto ate ~5 linhas.
  useLayoutEffect(() => {
    const el = campo.current;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = `${Math.min(el.scrollHeight, 132)}px`;
  }, [texto]);

  const prof = q.data?.profissional;
  // O cabecalho mostra quem esta do outro lado.
  const outro = pro ? (q.data?.paciente ? { nome: q.data.paciente.nome, sub: 'Paciente' } : null) : prof ? { nome: prof.nome, sub: prof.especialidade ?? 'Acompanhamento' } : null;
  const nomeAutor = (c: Citacao) => (c.meu ? 'Você' : outro?.nome ?? (c.autorTipo === 'Paciente' ? 'Paciente' : 'Profissional'));

  // Mensagens do servidor seguidas das que ainda estao saindo do aparelho.
  const itens: (Mensagem & { pendente?: Pendente })[] = [
    ...msgs,
    ...pendentes.map((p) => ({
      id: p.tempId,
      dataHoraUtc: p.dataHoraUtc,
      autorTipo: pro ? 'Profissional' : 'Paciente',
      meu: true,
      contexto: p.contexto,
      contextoRotulo: rotuloContexto(p.contexto),
      mensagem: p.mensagem,
      referencia: null,
      respostaA: p.respostaA,
      status: p.estado,
      lidaEmUtc: null,
      pendente: p,
    })),
  ];

  return (
    <div className="flex h-dvh flex-col bg-bg">
      <header className="pt-safe shrink-0 border-b border-line/70 bg-surface/90 backdrop-blur-xl">
        <div className="mx-auto flex max-w-2xl items-center gap-3 px-gutter py-3">
          <button aria-label="Voltar" onClick={() => (history.length > 1 ? nav(-1) : nav(pro ? '/pro/mensagens' : '/hoje'))} className="-ml-2 grid h-10 w-10 shrink-0 place-items-center rounded-full hover:bg-raised">
            <ChevronLeft className="h-6 w-6" />
          </button>
          <div className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-accent/15 font-display text-sm font-semibold text-accent">{iniciais(outro?.nome ?? '?')}</div>
          <div className="min-w-0 flex-1">
            <p className="truncate font-semibold leading-tight">{outro?.nome ?? (q.isLoading ? '…' : 'Seu profissional')}</p>
            <p className="truncate text-[13px] text-muted">{outro?.sub ?? 'Acompanhamento'}</p>
          </div>
          {pro && (
            <Link to={`/pro/pacientes/${pacienteId}`} className="shrink-0 rounded-full border border-line px-3 py-1.5 text-[13px] font-semibold text-muted transition hover:border-accent/40">
              Ver ficha
            </Link>
          )}
        </div>
      </header>

      <div className="relative min-h-0 flex-1">
        <div ref={rolagem} onScroll={aoRolar} className="h-full overflow-y-auto overscroll-contain">
          <div className="mx-auto flex max-w-2xl flex-col gap-1 px-gutter py-4">
            {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
            {q.isLoading && (
              <div className="flex flex-col gap-3">
                <Skeleton className="ml-auto h-12 w-2/3" />
                <Skeleton className="h-16 w-3/4" />
                <Skeleton className="ml-auto h-10 w-1/2" />
              </div>
            )}
            {q.data && !prof && <p className="mt-10 text-center text-sm text-muted">Seu acompanhamento ainda não tem um profissional responsável. A clínica já foi avisada e você poderá mandar mensagens assim que ela definir quem acompanha você.</p>}
            {q.data && prof && <ConvitePush pro={pro} />}
            {q.data && prof && !pro && (
              <div className="mb-3 flex items-start gap-2 rounded-2xl border border-line bg-surface px-3.5 py-3 text-[13px] text-muted">
                <ShieldAlert className="mt-0.5 h-4 w-4 shrink-0 text-warn" />
                <p>Respostas em até 24h úteis. Para urgências ou emergências, procure atendimento médico ou ligue 192.</p>
              </div>
            )}
            {q.data && prof && !itens.length && (
              <p className="mt-6 text-center text-sm text-muted">{pro ? 'Nenhuma mensagem ainda. Comece a conversa.' : 'Mande sua primeira mensagem: dúvidas sobre o treino, a dieta ou como você está se sentindo.'}</p>
            )}

            {itens.map((m, i) => {
              const novoDia = i === 0 || new Date(itens[i - 1].dataHoraUtc).toDateString() !== new Date(m.dataHoraUtc).toDateString();
              const colado = !novoDia && itens[i - 1].meu === m.meu && m.id !== divisorId.current;
              const fotoMsg = m.referencia?.tipo === 'Arquivo' && !!m.referencia.id && ehImagem({ nome: m.referencia.titulo });
              const enviada = !m.pendente;
              return (
                <div key={m.id} id={`msg-${m.id}`}>
                  {novoDia && <p className="my-3 text-center text-[12px] font-semibold uppercase tracking-wider text-faint">{diaRotulo(m.dataHoraUtc)}</p>}
                  {m.id === divisorId.current && (
                    <div id={`novas-${m.id}`} className="my-3 flex scroll-mt-4 items-center gap-3 text-[12px] font-semibold uppercase tracking-wider text-accent">
                      <span className="h-px flex-1 bg-accent/30" />
                      Mensagens novas
                      <span className="h-px flex-1 bg-accent/30" />
                    </div>
                  )}
                  <div className={clsx(!colado && 'mt-2')}>
                    <Bolha meu={m.meu} onResponder={enviada && prof ? () => responder(m) : undefined} onAcoes={enviada ? () => setAcoes(m) : undefined}>
                      <div
                        className={clsx(
                          'rounded-[20px] px-3.5 py-2.5 text-[15px] leading-snug transition-shadow duration-500',
                          m.meu ? 'rounded-br-md bg-accent text-accent-ink' : 'rounded-bl-md border border-line bg-surface',
                          m.pendente?.estado === 'Falhou' && 'opacity-70',
                          destaque === m.id && 'ring-4 ring-accent/40',
                        )}
                      >
                        {m.contexto !== 'Geral' && (
                          <p className={clsx('mb-0.5 text-[11px] font-semibold uppercase tracking-wider', m.meu ? 'text-accent-ink/70' : 'text-accent')}>{m.contextoRotulo}</p>
                        )}
                        {m.respostaA && (
                          <button
                            type="button"
                            onClick={() => irPara(m.respostaA!.id)}
                            className={clsx(
                              'mb-1.5 mt-0.5 block w-full min-w-[9rem] rounded-xl border-l-[3px] px-2.5 py-1.5 text-left text-[13px] leading-snug',
                              m.meu ? 'border-accent-ink/60 bg-accent-ink/10' : 'border-accent bg-raised',
                            )}
                          >
                            <span className={clsx('block text-[12px] font-semibold', m.meu ? 'text-accent-ink' : 'text-accent')}>{nomeAutor(m.respostaA)}</span>
                            <span className={clsx('line-clamp-2', m.meu ? 'text-accent-ink/80' : 'text-muted')}>{m.respostaA.trecho}</span>
                          </button>
                        )}
                        {!(fotoMsg && (m.mensagem === 'Foto' || m.mensagem.startsWith('Enviei o arquivo'))) && (
                          <p className="whitespace-pre-wrap break-words">
                            <TextoComLinks texto={m.mensagem} />
                          </p>
                        )}
                        {fotoMsg ? (
                          <ImagemArquivo
                            id={m.referencia!.id!}
                            pacienteId={pacienteId}
                            alt={m.referencia!.titulo ?? 'Foto'}
                            onAbrir={() => setFoto({ id: m.referencia!.id!, nome: m.referencia!.titulo ?? 'Foto' })}
                            maxAltura={320}
                            className="-mx-1 mt-0.5 w-60 rounded-2xl bg-black/20"
                          />
                        ) : (
                          m.referencia?.tipo === 'Arquivo' &&
                          m.referencia.id && (
                            <button
                              onClick={() => abrirArquivo({ id: m.referencia!.id!, nome: m.referencia!.titulo ?? 'arquivo' }, pacienteId).catch((e) => toast(e.message, 'bad'))}
                              className={clsx('mt-2 flex w-full items-center gap-2 rounded-xl px-2.5 py-2 text-left text-[13px] font-semibold', m.meu ? 'bg-accent-ink/10' : 'bg-raised')}
                            >
                              <FileText className="h-4 w-4 shrink-0" />
                              <span className="truncate">{m.referencia.titulo ?? 'Arquivo'}</span>
                            </button>
                          )
                        )}
                        <p className={clsx('mt-1 flex items-center justify-end gap-1 text-[11px]', m.meu ? 'text-accent-ink/60' : 'text-faint')}>
                          {horaMsg(m.dataHoraUtc)}
                          {m.meu && <StatusEnvio status={m.status} />}
                        </p>
                      </div>
                    </Bolha>
                    {m.pendente?.estado === 'Falhou' && (
                      <div className="mt-1 flex justify-end gap-3 text-[12px] font-semibold">
                        <button onClick={() => despachar(m.pendente!)} className="text-bad underline-offset-2 hover:underline">
                          Não enviada · tentar de novo
                        </button>
                        <button onClick={() => setPendentes((l) => l.filter((x) => x.tempId !== m.id))} className="text-faint hover:text-fg">
                          Descartar
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
            <div ref={fim} />
            <VisualizadorImagem arquivo={foto} pacienteId={pacienteId} onClose={() => setFoto(null)} />
          </div>
        </div>
        {novasAbaixo > 0 && (
          <button onClick={descer} className="absolute bottom-3 left-1/2 flex -translate-x-1/2 items-center gap-1.5 rounded-full bg-accent px-3.5 py-2 text-[13px] font-semibold text-accent-ink shadow-glow">
            <ArrowDown className="h-4 w-4" />
            {novasAbaixo === 1 ? '1 nova mensagem' : `${novasAbaixo} novas mensagens`}
          </button>
        )}
      </div>

      {prof && (
        <footer className="pb-safe shrink-0 border-t border-line/70 bg-surface/95 backdrop-blur-xl">
          <div className="mx-auto max-w-2xl px-gutter pt-2.5">
            {respondendo ? (
              <div className="mb-2 flex items-center gap-2 rounded-2xl border-l-[3px] border-accent bg-raised py-2 pl-3 pr-1">
                <Reply className="h-4 w-4 shrink-0 text-accent" />
                <div className="min-w-0 flex-1 text-[13px] leading-snug">
                  <p className="font-semibold text-accent">{respondendo.meu ? 'Respondendo à sua mensagem' : `Respondendo a ${nomeAutor(respondendo)}`}</p>
                  <p className="truncate text-muted">{respondendo.trecho}</p>
                </div>
                <button aria-label="Cancelar resposta" onClick={() => setRespondendo(null)} className="grid h-8 w-8 shrink-0 place-items-center rounded-full text-muted hover:bg-surface">
                  <X className="h-4 w-4" />
                </button>
              </div>
            ) : (
              <div className="-mx-gutter flex gap-1.5 overflow-x-auto px-gutter pb-2 no-scrollbar">
                {CONTEXTOS.map((c) => (
                  <button
                    key={c.v}
                    onClick={() => setContexto(c.v)}
                    className={clsx('h-8 shrink-0 rounded-full px-3 text-[13px] font-semibold transition', contexto === c.v ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
                  >
                    {c.l}
                  </button>
                ))}
              </div>
            )}
            <div className="flex items-end gap-1.5 pb-3">
              <MenuAnexo open={menuAnexo} onClose={() => setMenuAnexo(false)} onArquivo={(f) => anexar.mutate(f)} />
              <button aria-label="Anexar foto ou arquivo" disabled={anexar.isPending} onClick={() => setMenuAnexo(true)} className="grid h-11 w-10 shrink-0 place-items-center rounded-full text-muted transition hover:bg-raised disabled:opacity-50">
                {anexar.isPending ? <Loader2 className="h-5 w-5 animate-spin" /> : <Paperclip className="h-5 w-5" />}
              </button>
              {pro && (
                <>
                  <RespostasRapidas
                    open={rapidas}
                    onClose={() => setRapidas(false)}
                    textoAtual={texto}
                    nomePaciente={q.data?.paciente?.nome}
                    onEscolher={(t) => {
                      setTexto((atual) => (atual.trim() ? `${atual.trimEnd()} ${t}` : t));
                      campo.current?.focus();
                    }}
                  />
                  <button aria-label="Respostas rápidas" onClick={() => setRapidas(true)} className="grid h-11 w-10 shrink-0 place-items-center rounded-full text-muted transition hover:bg-raised">
                    <Zap className="h-5 w-5" />
                  </button>
                </>
              )}
              <textarea
                ref={campo}
                rows={1}
                value={texto}
                maxLength={3000}
                onChange={(e) => setTexto(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Escape' && respondendo) setRespondendo(null);
                  if (e.key === 'Enter' && !e.shiftKey && window.matchMedia('(pointer: fine)').matches) {
                    e.preventDefault();
                    mandar();
                  }
                }}
                placeholder={respondendo ? 'Escreva a resposta' : pro ? 'Responder' : 'Escreva sua mensagem'}
                className="min-h-[44px] w-full min-w-0 resize-none rounded-[22px] border border-line bg-bg px-4 py-2.5 text-base text-fg outline-none transition placeholder:text-faint focus:border-accent sm:text-[15px]"
              />
              <button
                aria-label="Enviar"
                disabled={!texto.trim() || anexar.isPending}
                onClick={mandar}
                className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-accent text-accent-ink shadow-glow transition active:scale-95 disabled:opacity-40 disabled:shadow-none"
              >
                <SendHorizontal className="h-5 w-5" />
              </button>
            </div>
          </div>
        </footer>
      )}

      <Sheet open={!!acoes} onClose={() => setAcoes(null)} title="Mensagem">
        {acoes && (
          <div className="flex flex-col gap-1 pb-2">
            <p className="mb-2 line-clamp-3 rounded-2xl bg-raised px-3.5 py-2.5 text-[14px] text-muted">{trechoDe(acoes)}</p>
            {prof && (
              <button
                onClick={() => {
                  responder(acoes);
                  setAcoes(null);
                }}
                className="flex items-center gap-3 rounded-2xl px-3 py-3 text-left font-semibold transition hover:bg-raised"
              >
                <Reply className="h-5 w-5 text-accent" /> Responder
              </button>
            )}
            {!!acoes.mensagem && trechoDe(acoes) === acoes.mensagem && (
              <button
                onClick={() => {
                  copiar(acoes);
                  setAcoes(null);
                }}
                className="flex items-center gap-3 rounded-2xl px-3 py-3 text-left font-semibold transition hover:bg-raised"
              >
                <Copy className="h-5 w-5 text-accent" /> Copiar texto
              </button>
            )}
            <p className="mt-2 px-3 text-[12px] text-faint">
              {acoes.meu ? `Enviada ${diaRotulo(acoes.dataHoraUtc).toLowerCase()} às ${horaMsg(acoes.dataHoraUtc)}` : `Recebida ${diaRotulo(acoes.dataHoraUtc).toLowerCase()} às ${horaMsg(acoes.dataHoraUtc)}`}
              {acoes.meu && acoes.lidaEmUtc ? ` · lida ${diaRotulo(acoes.lidaEmUtc).toLowerCase()} às ${horaMsg(acoes.lidaEmUtc)}` : ''}
            </p>
          </div>
        )}
      </Sheet>
    </div>
  );
}

export function ChatPage() {
  return <Conversa />;
}

export function ProChatPage() {
  const { id } = useParams();
  return <Conversa key={id} pacienteId={id} />;
}
