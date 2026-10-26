import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ChevronLeft, FileText, Loader2, Paperclip, SendHorizontal, ShieldAlert } from 'lucide-react';
import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ErrorBox, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { abrirArquivo, ACEITOS, enviarArquivo } from '@/lib/arquivos';
import { iniciais } from '@/lib/format';

interface Mensagem {
  id: string;
  dataHoraUtc: string;
  autorTipo: string;
  meu: boolean;
  contexto: string;
  contextoRotulo: string;
  mensagem: string;
  referencia: { tipo: string; id: string | null; titulo: string | null } | null;
  status: string;
  lidaEmUtc: string | null;
}

interface Conversa {
  pacienteId: string;
  profissional: { id: string; nome: string; especialidade: string | null } | null;
  prazoResposta?: string;
  emergencia?: string;
  mensagens: Mensagem[];
}

const CONTEXTOS = [
  { v: 'Geral', l: 'Geral' },
  { v: 'Treino', l: 'Treino' },
  { v: 'Nutricao', l: 'Nutrição' },
  { v: 'Recuperacao', l: 'Recuperação' },
  { v: 'Exames', l: 'Exames' },
  { v: 'Medicamentos', l: 'Medicamentos' },
];

const diaRotulo = (iso: string) => {
  const d = new Date(iso);
  const hoje = new Date();
  const ontem = new Date(Date.now() - 864e5);
  if (d.toDateString() === hoje.toDateString()) return 'Hoje';
  if (d.toDateString() === ontem.toDateString()) return 'Ontem';
  return d.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'short' });
};
const horaMsg = (iso: string) => new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });

export const useConversa = () =>
  useQuery({ queryKey: ['p', 'chat'], queryFn: () => api<Conversa>('/api/chat/me'), refetchInterval: 15_000, refetchIntervalInBackground: false });

export function ChatPage() {
  const nav = useNavigate();
  const qc = useQueryClient();
  const q = useConversa();
  const [texto, setTexto] = useState('');
  const [contexto, setContexto] = useState('Geral');
  const fim = useRef<HTMLDivElement>(null);
  const campo = useRef<HTMLTextAreaElement>(null);
  const arquivoRef = useRef<HTMLInputElement>(null);
  const msgs = q.data?.mensagens ?? [];

  // Abrir a conversa marca como lida no servidor; o contador some junto.
  useEffect(() => {
    if (q.data) qc.invalidateQueries({ queryKey: ['p', 'chat-nao-lidas'] });
  }, [q.data, qc]);

  useLayoutEffect(() => {
    fim.current?.scrollIntoView({ block: 'end' });
  }, [msgs.length]);

  const enviar = useMutation({
    mutationFn: (body: { mensagem: string; contexto: string; referenciaTipo?: string; referenciaId?: string; referenciaTitulo?: string }) =>
      api<Mensagem>('/api/chat/me/mensagens', { method: 'POST', json: body }),
    onSuccess: (m) => {
      qc.setQueryData<Conversa>(['p', 'chat'], (c) => (c ? { ...c, mensagens: [...c.mensagens, { ...m, meu: true, lidaEmUtc: null }] } : c));
      qc.invalidateQueries({ queryKey: ['p', 'chat'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível enviar.', 'bad'),
  });

  const anexar = useMutation({
    mutationFn: async (file: File) => {
      const a = await enviarArquivo(file, { categoria: file.type.startsWith('image/') ? 'Foto' : 'Exame', viaChat: true });
      return enviar.mutateAsync({ mensagem: texto.trim() || `Enviei o arquivo ${a.nome}.`, contexto, referenciaTipo: 'Arquivo', referenciaId: a.id, referenciaTitulo: a.nome });
    },
    onSuccess: () => {
      setTexto('');
      qc.invalidateQueries({ queryKey: ['p', 'arquivos'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível enviar o arquivo.', 'bad'),
  });

  const mandar = () => {
    const m = texto.trim();
    if (!m || enviar.isPending) return;
    enviar.mutate({ mensagem: m, contexto }, { onSuccess: () => setTexto('') });
    campo.current?.focus();
  };

  // Campo cresce com o texto ate ~5 linhas.
  useLayoutEffect(() => {
    const el = campo.current;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = `${Math.min(el.scrollHeight, 132)}px`;
  }, [texto]);

  const prof = q.data?.profissional;
  const ocupado = enviar.isPending || anexar.isPending;

  return (
    <div className="flex h-dvh flex-col bg-bg">
      <header className="pt-safe shrink-0 border-b border-line/70 bg-surface/90 backdrop-blur-xl">
        <div className="mx-auto flex max-w-2xl items-center gap-3 px-gutter py-3">
          <button aria-label="Voltar" onClick={() => (history.length > 1 ? nav(-1) : nav('/hoje'))} className="-ml-2 grid h-10 w-10 shrink-0 place-items-center rounded-full hover:bg-raised">
            <ChevronLeft className="h-6 w-6" />
          </button>
          <div className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-accent/15 font-display text-sm font-semibold text-accent">{iniciais(prof?.nome ?? '?')}</div>
          <div className="min-w-0 flex-1">
            <p className="truncate font-semibold leading-tight">{prof?.nome ?? (q.isLoading ? '…' : 'Seu profissional')}</p>
            <p className="truncate text-[13px] text-muted">{prof?.especialidade ?? 'Acompanhamento'}</p>
          </div>
        </div>
      </header>

      <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain">
        <div className="mx-auto flex max-w-2xl flex-col gap-1 px-gutter py-4">
          {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
          {q.isLoading && (
            <div className="flex flex-col gap-3">
              <Skeleton className="ml-auto h-12 w-2/3" />
              <Skeleton className="h-16 w-3/4" />
              <Skeleton className="ml-auto h-10 w-1/2" />
            </div>
          )}
          {q.data && !prof && <p className="mt-10 text-center text-sm text-muted">Você ainda não tem um profissional vinculado. Assim que tiver, a conversa aparece aqui.</p>}
          {q.data && prof && (
            <div className="mb-3 flex items-start gap-2 rounded-2xl border border-line bg-surface px-3.5 py-3 text-[13px] text-muted">
              <ShieldAlert className="mt-0.5 h-4 w-4 shrink-0 text-warn" />
              <p>Respostas em até 24h úteis. Para urgências ou emergências, procure atendimento médico ou ligue 192.</p>
            </div>
          )}
          {q.data && prof && !msgs.length && <p className="mt-6 text-center text-sm text-muted">Mande sua primeira mensagem: dúvidas sobre o treino, a dieta ou como você está se sentindo.</p>}

          {msgs.map((m, i) => {
            const novoDia = i === 0 || new Date(msgs[i - 1].dataHoraUtc).toDateString() !== new Date(m.dataHoraUtc).toDateString();
            const colado = !novoDia && msgs[i - 1].meu === m.meu;
            return (
              <div key={m.id}>
                {novoDia && <p className="my-3 text-center text-[12px] font-semibold uppercase tracking-wider text-faint">{diaRotulo(m.dataHoraUtc)}</p>}
                <div className={clsx('flex', m.meu ? 'justify-end' : 'justify-start', !colado && 'mt-2')}>
                  <div
                    className={clsx(
                      'max-w-[82%] rounded-[20px] px-3.5 py-2.5 text-[15px] leading-snug',
                      m.meu ? 'rounded-br-md bg-accent text-accent-ink' : 'rounded-bl-md border border-line bg-surface',
                    )}
                  >
                    {m.contexto !== 'Geral' && (
                      <p className={clsx('mb-0.5 text-[11px] font-semibold uppercase tracking-wider', m.meu ? 'text-accent-ink/70' : 'text-accent')}>{m.contextoRotulo}</p>
                    )}
                    <p className="whitespace-pre-wrap break-words">{m.mensagem}</p>
                    {m.referencia?.tipo === 'Arquivo' && m.referencia.id && (
                      <button
                        onClick={() => abrirArquivo({ id: m.referencia!.id!, nome: m.referencia!.titulo ?? 'arquivo' }).catch((e) => toast(e.message, 'bad'))}
                        className={clsx('mt-2 flex w-full items-center gap-2 rounded-xl px-2.5 py-2 text-left text-[13px] font-semibold', m.meu ? 'bg-accent-ink/10' : 'bg-raised')}
                      >
                        <FileText className="h-4 w-4 shrink-0" />
                        <span className="truncate">{m.referencia.titulo ?? 'Arquivo'}</span>
                      </button>
                    )}
                    <p className={clsx('mt-1 text-right text-[11px]', m.meu ? 'text-accent-ink/60' : 'text-faint')}>
                      {horaMsg(m.dataHoraUtc)}
                      {m.meu && (m.lidaEmUtc ? ' · lida' : '')}
                    </p>
                  </div>
                </div>
              </div>
            );
          })}
          <div ref={fim} />
        </div>
      </div>

      {prof && (
        <footer className="pb-safe shrink-0 border-t border-line/70 bg-surface/95 backdrop-blur-xl">
          <div className="mx-auto max-w-2xl px-gutter pt-2.5">
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
            <div className="flex items-end gap-2 pb-3">
              <input
                ref={arquivoRef}
                type="file"
                accept={ACEITOS}
                className="hidden"
                onChange={(e) => {
                  const f = e.target.files?.[0];
                  e.target.value = '';
                  if (f) anexar.mutate(f);
                }}
              />
              <button aria-label="Anexar arquivo" disabled={ocupado} onClick={() => arquivoRef.current?.click()} className="grid h-11 w-11 shrink-0 place-items-center rounded-full text-muted transition hover:bg-raised disabled:opacity-50">
                {anexar.isPending ? <Loader2 className="h-5 w-5 animate-spin" /> : <Paperclip className="h-5 w-5" />}
              </button>
              <textarea
                ref={campo}
                rows={1}
                value={texto}
                maxLength={3000}
                onChange={(e) => setTexto(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey && window.matchMedia('(pointer: fine)').matches) {
                    e.preventDefault();
                    mandar();
                  }
                }}
                placeholder="Escreva sua mensagem"
                className="min-h-[44px] w-full min-w-0 resize-none rounded-[22px] border border-line bg-bg px-4 py-2.5 text-base text-fg outline-none transition placeholder:text-faint focus:border-accent sm:text-[15px]"
              />
              <button
                aria-label="Enviar"
                disabled={!texto.trim() || ocupado}
                onClick={mandar}
                className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-accent text-accent-ink shadow-glow transition active:scale-95 disabled:opacity-40 disabled:shadow-none"
              >
                {enviar.isPending ? <Loader2 className="h-5 w-5 animate-spin" /> : <SendHorizontal className="h-5 w-5" />}
              </button>
            </div>
          </div>
        </footer>
      )}
    </div>
  );
}
