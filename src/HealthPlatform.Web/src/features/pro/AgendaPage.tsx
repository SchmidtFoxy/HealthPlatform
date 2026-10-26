import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { CalendarClock, ChevronLeft, ChevronRight, MessageCircle, Phone, Plus, Search } from 'lucide-react';
import { ReactNode, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { usePacientes } from './queries';

interface ConsultaAgenda {
  id: string;
  pacienteId: string;
  pacienteNome: string;
  dataHoraUtc: string;
  dataHoraLocal: string;
  status: Status;
  motivo: string | null;
  telefone: string | null;
  email: string | null;
  possuiAvaliacao: boolean;
  possuiAnamnese: boolean;
}

type Status = 'Agendada' | 'Confirmada' | 'Realizada' | 'Cancelada' | 'Faltou';

const STATUS: Record<Status, { l: string; tone: 'neutral' | 'info' | 'ok' | 'warn' | 'bad' }> = {
  Agendada: { l: 'Agendada', tone: 'neutral' },
  Confirmada: { l: 'Confirmada', tone: 'info' },
  Realizada: { l: 'Realizada', tone: 'ok' },
  Cancelada: { l: 'Cancelada', tone: 'warn' },
  Faltou: { l: 'Faltou', tone: 'bad' },
};

const offset = () => -new Date().getTimezoneOffset();
const iso = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
const segunda = (d: Date) => {
  const x = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  x.setDate(x.getDate() - ((x.getDay() + 6) % 7));
  return x;
};
const somar = (d: Date, n: number) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);
const hora = (s: string) => s.slice(11, 16);

export function AgendaPage() {
  const qc = useQueryClient();
  const [dia, setDia] = useState(() => new Date());
  const inicio = segunda(dia);
  const dias = Array.from({ length: 7 }, (_, i) => somar(inicio, i));
  const q = useQuery({
    queryKey: ['pro', 'agenda', iso(inicio)],
    queryFn: () => api<ConsultaAgenda[]>(`/api/agenda/periodo?inicio=${iso(inicio)}&fim=${iso(somar(inicio, 6))}&offsetMinutos=${offset()}`),
    placeholderData: (p) => p,
  });
  const [nova, setNova] = useState(false);
  const [reagendar, setReagendar] = useState<ConsultaAgenda | null>(null);

  const porDia = useMemo(() => {
    const m = new Map<string, ConsultaAgenda[]>();
    for (const c of q.data ?? []) m.set(c.dataHoraLocal.slice(0, 10), [...(m.get(c.dataHoraLocal.slice(0, 10)) ?? []), c]);
    return m;
  }, [q.data]);
  const doDia = porDia.get(iso(dia)) ?? [];

  const status = useMutation({
    mutationFn: ({ c, s }: { c: ConsultaAgenda; s: Status }) => api(`/api/agenda/consultas/${c.id}/status?offsetMinutos=${offset()}`, { method: 'PATCH', json: { status: s } }),
    onSuccess: (_, v) => {
      toast(`Consulta ${STATUS[v.s].l.toLowerCase()}.`);
      qc.invalidateQueries({ queryKey: ['pro', 'agenda'] });
      qc.invalidateQueries({ queryKey: ['pro', 'dashboard'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível alterar.', 'bad'),
  });

  const hoje = iso(new Date());
  return (
    <Page wide>
      <PageHeader
        eyebrow="Agenda"
        title={dia.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' }).replace(/^./, (c) => c.toUpperCase())}
        action={
          <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setNova(true)}>
            Consulta
          </Button>
        }
      />
      <div className="mb-4 flex items-center gap-1">
        <button aria-label="Semana anterior" onClick={() => setDia(somar(dia, -7))} className="grid h-10 w-8 shrink-0 place-items-center rounded-full text-muted hover:bg-raised">
          <ChevronLeft className="h-5 w-5" />
        </button>
        <div className="grid flex-1 grid-cols-7 gap-1">
          {dias.map((d) => {
            const k = iso(d);
            const n = porDia.get(k)?.filter((c) => c.status !== 'Cancelada').length ?? 0;
            const sel = k === iso(dia);
            return (
              <button
                key={k}
                onClick={() => setDia(d)}
                className={clsx('flex flex-col items-center rounded-2xl py-2 transition', sel ? 'bg-accent text-accent-ink' : k === hoje ? 'bg-accent/10 text-accent' : 'hover:bg-raised')}
              >
                <span className={clsx('text-[11px] font-semibold uppercase', sel ? 'text-accent-ink/70' : 'text-faint')}>{d.toLocaleDateString('pt-BR', { weekday: 'short' }).slice(0, 3)}</span>
                <span className="num text-[17px] font-semibold">{d.getDate()}</span>
                <span className={clsx('mt-0.5 h-1.5 w-1.5 rounded-full', n ? (sel ? 'bg-accent-ink' : 'bg-accent') : 'bg-transparent')} />
              </button>
            );
          })}
        </div>
        <button aria-label="Próxima semana" onClick={() => setDia(somar(dia, 7))} className="grid h-10 w-8 shrink-0 place-items-center rounded-full text-muted hover:bg-raised">
          <ChevronRight className="h-5 w-5" />
        </button>
      </div>
      {iso(dia) !== hoje && (
        <button onClick={() => setDia(new Date())} className="mb-3 text-sm font-semibold text-accent">
          Voltar para hoje
        </button>
      )}

      <p className="eyebrow mb-2 capitalize">{dia.toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })}</p>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="h-40" />}
      {q.data && !doDia.length && (
        <Empty icon={<CalendarClock className="h-5 w-5" />} title="Nenhuma consulta neste dia" action={<Button variant="soft" onClick={() => setNova(true)}>Marcar consulta</Button>}>
          Toque em outro dia ou marque uma consulta.
        </Empty>
      )}
      <div className="flex flex-col gap-2">
        {doDia.map((c) => (
          <Card key={c.id} className={clsx('p-4', (c.status === 'Cancelada' || c.status === 'Faltou') && 'opacity-60')}>
            <div className="flex items-start gap-3">
              <p className="num w-12 shrink-0 pt-0.5 font-display text-[17px] font-semibold">{hora(c.dataHoraLocal)}</p>
              <div className="min-w-0 flex-1">
                <Link to={`/pro/pacientes/${c.pacienteId}`} className="block truncate font-semibold hover:underline">
                  {c.pacienteNome}
                </Link>
                <p className="truncate text-[13px] text-muted">{c.motivo ?? 'Consulta'}</p>
              </div>
              <Pill tone={STATUS[c.status]?.tone ?? 'neutral'}>{STATUS[c.status]?.l ?? c.status}</Pill>
            </div>
            <div className="mt-3 flex flex-wrap gap-1.5 pl-[60px]">
              {c.status === 'Agendada' && <Acao onClick={() => status.mutate({ c, s: 'Confirmada' })}>Confirmar</Acao>}
              {(c.status === 'Agendada' || c.status === 'Confirmada') && (
                <>
                  <Acao onClick={() => status.mutate({ c, s: 'Realizada' })}>Realizada</Acao>
                  <Acao onClick={() => status.mutate({ c, s: 'Faltou' })}>Faltou</Acao>
                  <Acao onClick={() => status.mutate({ c, s: 'Cancelada' })}>Cancelar</Acao>
                </>
              )}
              <Acao onClick={() => setReagendar(c)}>{c.status === 'Cancelada' || c.status === 'Faltou' ? 'Remarcar' : 'Reagendar'}</Acao>
              <Link to={`/pro/mensagens/${c.pacienteId}`} className="grid h-8 w-8 place-items-center rounded-full bg-raised text-muted" aria-label="Chat">
                <MessageCircle className="h-4 w-4" />
              </Link>
              {c.telefone && (
                <a href={`https://wa.me/55${c.telefone.replace(/\D/g, '')}`} target="_blank" rel="noreferrer" className="grid h-8 w-8 place-items-center rounded-full bg-raised text-ok" aria-label="WhatsApp">
                  <Phone className="h-4 w-4" />
                </a>
              )}
            </div>
          </Card>
        ))}
      </div>

      <NovaConsultaSheet open={nova} dia={dia} onClose={() => setNova(false)} />
      <ReagendarSheet consulta={reagendar} onClose={() => setReagendar(null)} />
    </Page>
  );
}

function Acao({ children, onClick }: { children: ReactNode; onClick: () => void }) {
  return (
    <button onClick={onClick} className="h-8 rounded-full bg-raised px-3 text-[13px] font-semibold text-muted transition hover:text-fg">
      {children}
    </button>
  );
}

const localInput = (d: Date) => `${iso(d)}T${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;

function NovaConsultaSheet({ open, dia, onClose }: { open: boolean; dia: Date; onClose: () => void }) {
  const qc = useQueryClient();
  const [busca, setBusca] = useState('');
  const pacientes = usePacientes(busca);
  const [paciente, setPaciente] = useState<{ id: string; nome: string } | null>(null);
  const [quando, setQuando] = useState('');
  const [motivo, setMotivo] = useState('');
  useEffect(() => {
    if (open) setQuando(`${iso(dia)}T09:00`);
  }, [open, dia]);

  const criar = useMutation({
    mutationFn: () =>
      api(`/api/pacientes/${paciente!.id}/consultas`, {
        method: 'POST',
        json: { dataHoraUtc: new Date(quando).toISOString(), motivo: motivo.trim() || null, status: 'Agendada' },
      }),
    onSuccess: () => {
      toast('Consulta marcada.');
      qc.invalidateQueries({ queryKey: ['pro', 'agenda'] });
      qc.invalidateQueries({ queryKey: ['pro', 'dashboard'] });
      fechar();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível marcar.', 'bad'),
  });
  const fechar = () => {
    setPaciente(null);
    setBusca('');
    setMotivo('');
    setQuando('');
    onClose();
  };

  return (
    <Sheet
      open={open}
      onClose={fechar}
      title="Nova consulta"
      tall
      footer={
        <Button size="lg" className="w-full" disabled={!paciente || !quando} loading={criar.isPending} onClick={() => criar.mutate()}>
          Marcar consulta
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Paciente">
          {paciente ? (
            <div className="flex items-center justify-between rounded-2xl bg-accent/10 px-4 py-3">
              <span className="font-semibold">{paciente.nome}</span>
              <button className="text-sm font-semibold text-accent" onClick={() => setPaciente(null)}>
                Trocar
              </button>
            </div>
          ) : (
            <>
              <div className="relative">
                <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-faint" />
                <input className={clsx(inputCls, 'pl-10')} value={busca} placeholder="Buscar paciente" onChange={(e) => setBusca(e.target.value)} />
              </div>
              <div className="mt-1 max-h-56 overflow-y-auto rounded-2xl border border-line/70">
                {(pacientes.data?.itens ?? []).slice(0, 30).map((p) => (
                  <button key={p.id} className="block w-full border-b border-line/50 px-4 py-2.5 text-left last:border-0 hover:bg-raised/50" onClick={() => setPaciente({ id: p.id, nome: p.nome })}>
                    {p.nome}
                  </button>
                ))}
              </div>
            </>
          )}
        </Field>
        <Field label="Dia e horário">
          <input type="datetime-local" className={inputCls} value={quando} onChange={(e) => setQuando(e.target.value)} />
        </Field>
        <Field label="Motivo (opcional)">
          <input className={inputCls} value={motivo} maxLength={200} placeholder="Ex.: Retorno, avaliação, ajuste de plano" onChange={(e) => setMotivo(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

function ReagendarSheet({ consulta, onClose }: { consulta: ConsultaAgenda | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [quando, setQuando] = useState('');
  useEffect(() => {
    if (consulta) setQuando(consulta.dataHoraLocal.slice(0, 16));
  }, [consulta]);
  const salvar = useMutation({
    mutationFn: () => api(`/api/agenda/consultas/${consulta!.id}/reagendar`, { method: 'PATCH', json: { dataHoraLocal: `${quando}:00`, offsetMinutos: offset() } }),
    onSuccess: () => {
      toast('Consulta reagendada.');
      qc.invalidateQueries({ queryKey: ['pro', 'agenda'] });
      qc.invalidateQueries({ queryKey: ['pro', 'dashboard'] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível reagendar.', 'bad'),
  });
  return (
    <Sheet
      open={!!consulta}
      onClose={onClose}
      title={`Reagendar ${consulta?.pacienteNome ?? ''}`}
      footer={
        <Button size="lg" className="w-full" disabled={!quando} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar novo horário
        </Button>
      }
    >
      <Field label="Novo dia e horário">
        <input type="datetime-local" className={inputCls} value={quando} min={localInput(new Date())} onChange={(e) => setQuando(e.target.value)} />
      </Field>
    </Sheet>
  );
}
