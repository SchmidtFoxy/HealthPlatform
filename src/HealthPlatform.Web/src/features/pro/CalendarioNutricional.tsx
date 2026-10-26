import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ArrowLeftRight, CalendarDays, ChevronLeft, ChevronRight, Copy } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, Card, Field, inputCls, Pill, Sheet, toast } from '@/components/ui';
import { api, ApiError } from '@/lib/api';
import { n0 } from '@/lib/format';
import { Chips } from './CuidadosPro';

// Calendario nutricional: qual dieta vale em cada dia (treino, descanso, viagem...).
// Semana a semana, com copiar para a proxima semana e trocar dois dias entre si.

interface Programacao {
  id: string;
  data: string;
  planoAlimentarId: string;
  planoNome: string;
  planoVersao: number;
  metaCalorias: number | null;
  contexto: string;
  observacoes: string | null;
}

const CONTEXTOS = [
  { value: 'Padrao', label: 'Padrão' },
  { value: 'Treino', label: 'Dia de treino' },
  { value: 'Descanso', label: 'Descanso' },
  { value: 'Viagem', label: 'Viagem' },
  { value: 'Flexivel', label: 'Flexível' },
];
const TOM: Record<string, 'neutral' | 'accent' | 'info' | 'warn' | 'ok'> = { Padrao: 'neutral', Treino: 'accent', Descanso: 'info', Viagem: 'warn', Flexivel: 'ok' };
const rotulo = (c: string) => CONTEXTOS.find((x) => x.value === c)?.label ?? c;

const iso = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
const somaDias = (d: Date, n: number) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);
const segunda = (d: Date) => somaDias(d, -((d.getDay() + 6) % 7));

export function CalendarioNutricional({ pacienteId, planos }: { pacienteId: string; planos: { id: string; nome: string; status: string; versao: number }[] }) {
  const qc = useQueryClient();
  const [offset, setOffset] = useState(0);
  const [aberto, setAberto] = useState<string | null>(null);
  const [troca, setTroca] = useState<string | null | false>(false);
  const inicio = somaDias(segunda(new Date()), offset * 7);
  const dias = Array.from({ length: 7 }, (_, i) => somaDias(inicio, i));
  const chave = ['pro', 'calendario-nutricional', pacienteId, iso(inicio)];
  const q = useQuery({
    queryKey: chave,
    queryFn: () => api<Programacao[]>(`/api/pacientes/${pacienteId}/calendario-nutricional?inicio=${iso(inicio)}&fim=${iso(dias[6])}`),
  });
  const porDia = new Map((q.data ?? []).map((x) => [x.data.slice(0, 10), x]));
  const hoje = iso(new Date());
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'calendario-nutricional', pacienteId] });

  const copiar = useMutation({
    mutationFn: (sobrescrever: boolean) => api(`/api/pacientes/${pacienteId}/calendario-nutricional/copiar-semana`, { method: 'POST', json: { dataReferencia: iso(inicio), sobrescrever } }),
    onSuccess: () => {
      toast('Semana copiada para a próxima.');
      invalidar();
      setOffset((o) => o + 1);
    },
    onError: (e, sobrescrever) => {
      if (!sobrescrever && e instanceof ApiError && e.status === 409) {
        if (window.confirm('A próxima semana já tem dias programados. Substituir pela desta semana?')) copiar.mutate(true);
        return;
      }
      toast(e instanceof Error ? e.message : 'Não foi possível copiar.', 'bad');
    },
  });
  const trocar = useMutation({
    mutationFn: (d: [string, string]) => api(`/api/pacientes/${pacienteId}/calendario-nutricional/trocar-dias`, { method: 'POST', json: { dataA: d[0], dataB: d[1] } }),
    onSuccess: () => {
      toast('Dias trocados.');
      setTroca(false);
      invalidar();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível trocar.', 'bad'),
  });

  const tocar = (d: string) => {
    if (troca === false) return setAberto(d);
    if (!porDia.has(d)) return toast('Escolha um dia que já tenha dieta programada.', 'bad');
    if (troca === null) return setTroca(d);
    if (troca === d) return setTroca(null);
    trocar.mutate([troca, d]);
  };

  const fmt = (d: Date) => d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }).replace('.', '').replace(' de ', ' ');
  return (
    <section>
      <div className="mb-2 flex items-center justify-between gap-3">
        <div>
          <p className="font-semibold">Calendário nutricional</p>
          <p className="text-[13px] text-muted">Qual dieta vale em cada dia. Sem programação, vale o plano ativo.</p>
        </div>
      </div>
      <Card className="overflow-hidden">
        <div className="flex items-center gap-1 border-b border-line/60 px-2 py-1.5">
          <button aria-label="Semana anterior" onClick={() => setOffset((o) => o - 1)} className="grid h-9 w-9 place-items-center rounded-full text-muted hover:bg-raised">
            <ChevronLeft className="h-5 w-5" />
          </button>
          <p className="num flex-1 text-center text-[14px] font-semibold">
            {fmt(dias[0])} a {fmt(dias[6])}
            {offset !== 0 && (
              <button onClick={() => setOffset(0)} className="ml-2 text-[12px] font-semibold text-accent">
                hoje
              </button>
            )}
          </p>
          <button aria-label="Próxima semana" onClick={() => setOffset((o) => o + 1)} className="grid h-9 w-9 place-items-center rounded-full text-muted hover:bg-raised">
            <ChevronRight className="h-5 w-5" />
          </button>
        </div>
        {troca !== false && (
          <p className="bg-accent/10 px-4 py-2 text-[13px] text-accent">
            {troca ? 'Agora toque no outro dia.' : 'Toque no primeiro dia da troca.'}{' '}
            <button className="font-semibold underline" onClick={() => setTroca(false)}>
              Cancelar
            </button>
          </p>
        )}
        <div className="divide-y divide-line/60">
          {dias.map((d) => {
            const k = iso(d);
            const x = porDia.get(k);
            return (
              <button
                key={k}
                onClick={() => tocar(k)}
                aria-label={`${d.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric' })}${x ? `, ${x.planoNome}` : ', sem programação'}`}
                className={clsx('flex w-full items-center gap-3 px-4 py-3 text-left transition hover:bg-raised/40', troca === k && 'bg-accent/10', q.isLoading && 'opacity-60')}
              >
                <span className={clsx('w-11 shrink-0 text-center', k === hoje ? 'text-accent' : 'text-muted')}>
                  <span className="block text-[11px] font-semibold uppercase">{d.toLocaleDateString('pt-BR', { weekday: 'short' }).replace('.', '')}</span>
                  <span className="num block text-lg font-semibold leading-tight">{d.getDate()}</span>
                </span>
                <span className="min-w-0 flex-1">
                  {x ? (
                    <>
                      <span className="block truncate font-semibold">{x.planoNome}</span>
                      <span className="block truncate text-[13px] text-muted">{[`v${x.planoVersao}`, x.metaCalorias ? `${n0(x.metaCalorias)} kcal` : null, x.observacoes].filter(Boolean).join(' · ')}</span>
                    </>
                  ) : (
                    <span className="text-[14px] text-faint">Sem programação</span>
                  )}
                </span>
                {x && <Pill tone={TOM[x.contexto] ?? 'neutral'}>{rotulo(x.contexto)}</Pill>}
              </button>
            );
          })}
        </div>
        <div className="grid grid-cols-2 gap-2 border-t border-line/60 p-2">
          <Button size="sm" variant="ghost" icon={<Copy className="h-4 w-4" />} disabled={!porDia.size} loading={copiar.isPending} onClick={() => copiar.mutate(false)}>
            Copiar semana
          </Button>
          <Button size="sm" variant="ghost" icon={<ArrowLeftRight className="h-4 w-4" />} disabled={porDia.size < 2} loading={trocar.isPending} onClick={() => setTroca(troca === false ? null : false)}>
            Trocar dias
          </Button>
        </div>
      </Card>
      <DiaSheet pacienteId={pacienteId} data={aberto} item={aberto ? porDia.get(aberto) ?? null : null} planos={planos} onClose={() => setAberto(null)} onSalvo={invalidar} />
    </section>
  );
}

function DiaSheet({
  pacienteId,
  data,
  item,
  planos,
  onClose,
  onSalvo,
}: {
  pacienteId: string;
  data: string | null;
  item: Programacao | null;
  planos: { id: string; nome: string; status: string; versao: number }[];
  onClose: () => void;
  onSalvo: () => void;
}) {
  const [plano, setPlano] = useState('');
  const [contexto, setContexto] = useState('Padrao');
  const [obs, setObs] = useState('');
  useEffect(() => {
    if (!data) return;
    setPlano(item?.planoAlimentarId ?? planos.find((p) => p.status === 'Ativo')?.id ?? '');
    setContexto(item?.contexto ?? 'Padrao');
    setObs(item?.observacoes ?? '');
  }, [data, item, planos]);

  const salvar = useMutation({
    mutationFn: () => api(`/api/pacientes/${pacienteId}/calendario-nutricional/${data}`, { method: 'PUT', json: { planoAlimentarId: plano, contexto, observacoes: obs.trim() || null } }),
    onSuccess: () => {
      toast('Dia programado.');
      onSalvo();
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });
  const remover = useMutation({
    mutationFn: () => api(`/api/pacientes/${pacienteId}/calendario-nutricional/${data}`, { method: 'DELETE' }),
    onSuccess: () => {
      toast('Programação do dia removida.');
      onSalvo();
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível remover.', 'bad'),
  });

  const titulo = data ? new Date(`${data}T12:00:00`).toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' }) : '';
  const lista = planos.filter((p) => p.status !== 'Concluido' || p.id === plano);
  return (
    <Sheet
      open={!!data}
      onClose={onClose}
      title={titulo.charAt(0).toUpperCase() + titulo.slice(1)}
      footer={
        <Button size="lg" className="w-full" disabled={!plano} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar dia
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        {!planos.length ? (
          <p className="flex items-center gap-2 rounded-xl bg-raised/60 p-3 text-sm text-muted">
            <CalendarDays className="h-4 w-4 shrink-0" /> Crie uma dieta para este paciente antes de programar os dias.
          </p>
        ) : (
          <Field label="Dieta do dia">
            <select className={inputCls} value={plano} onChange={(e) => setPlano(e.target.value)}>
              <option value="">Selecione…</option>
              {lista.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nome} · v{p.versao}
                  {p.status !== 'Ativo' ? ` (${p.status === 'Inativo' ? 'não liberado' : p.status.toLowerCase()})` : ''}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Tipo de dia" group>
          <div className="-mx-1 overflow-x-auto px-1 pb-1">
            <Chips value={contexto} onChange={setContexto} options={CONTEXTOS} />
          </div>
        </Field>
        <Field label="Orientação do dia">
          <input className={inputCls} value={obs} maxLength={800} placeholder="Ex.: refeição livre no jantar" onChange={(e) => setObs(e.target.value)} />
        </Field>
        {item && (
          <Button variant="ghost" className="text-bad" loading={remover.isPending} onClick={() => remover.mutate()}>
            Tirar programação deste dia
          </Button>
        )}
      </div>
    </Sheet>
  );
}
