import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Activity, AlertCircle, Droplet, Dumbbell, Moon, NotebookPen, Plus, Smile, Sunset, Utensils } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { hojeIso } from '@/lib/format';
import { Escala } from './CheckInSheet';
import { useHistoricoDiario } from './queries';

interface Registro {
  id: string;
  dataHoraUtc: string;
  tipo: string;
  descricao: string | null;
  valorNumerico: number | null;
  unidade: string | null;
  escala: number | null;
}

const TIPOS = [
  { v: 'Sono', icon: Moon, unidade: 'h' },
  { v: 'Hidratacao', l: 'Hidratação', icon: Droplet, unidade: 'L' },
  { v: 'Alimentacao', l: 'Alimentação', icon: Utensils, unidade: '' },
  { v: 'Treino', icon: Dumbbell, unidade: 'min' },
  { v: 'Sintoma', icon: AlertCircle, unidade: '' },
  { v: 'Humor', icon: Smile, unidade: '' },
  { v: 'Observacao', l: 'Observação', icon: NotebookPen, unidade: '' },
] as const;

const infoTipo = (t: string) => {
  if (t === 'FechamentoDia') return { l: 'Fechamento do dia', icon: Sunset };
  const x = TIPOS.find((i) => i.v.toLowerCase() === t.toLowerCase());
  return { l: x ? ('l' in x ? x.l : x.v) : t, icon: x?.icon ?? Activity };
};

const useDiario = () => useQuery({ queryKey: ['p', 'diario-registros'], queryFn: () => api<{ total: number; itens: Registro[] }>('/api/portal/me/diario') });

const quando = (iso: string) => {
  const d = new Date(iso);
  const hoje = new Date().toDateString();
  const ontem = new Date(Date.now() - 864e5).toDateString();
  const h = d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  if (d.toDateString() === hoje) return `Hoje, ${h}`;
  if (d.toDateString() === ontem) return `Ontem, ${h}`;
  return `${d.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'short' })}, ${h}`;
};

export function DiaryPage() {
  const q = useDiario();
  const hist = useHistoricoDiario(14);
  const [params, setParams] = useSearchParams();
  const [novo, setNovo] = useState(false);
  const [fechar, setFechar] = useState(params.has('fechar'));
  const hoje = hist.data?.itens.find((d) => d.data === hojeIso());

  useEffect(() => {
    if (params.has('fechar')) setParams({}, { replace: true });
  }, [params, setParams]);

  return (
    <Page>
      <PageHeader
        back="/perfil"
        title="Diário"
        subtitle="O que aconteceu no seu dia, do seu jeito. Seu profissional acompanha."
        action={
          <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setNovo(true)}>
            Registrar
          </Button>
        }
      />

      <button
        onClick={() => setFechar(true)}
        className={clsx('mb-5 flex w-full items-center gap-3 rounded-xl2 border p-4 text-left transition active:scale-[0.99]', hoje?.diaFechado ? 'border-line bg-surface' : 'border-accent/40 bg-accent/[0.06]')}
      >
        <span className={clsx('grid h-10 w-10 shrink-0 place-items-center rounded-xl', hoje?.diaFechado ? 'bg-ok/15 text-ok' : 'bg-accent/15 text-accent')}>
          <Sunset className="h-5 w-5" />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block font-semibold">{hoje?.diaFechado ? `Dia fechado · ${hoje.percepcaoDoDia ?? '—'}/10` : 'Fechar o dia'}</span>
          <span className="block truncate text-[13px] text-muted">{hoje?.diaFechado ? 'Toque para ajustar a reflexão de hoje' : 'Menos de um minuto para registrar como foi'}</span>
        </span>
      </button>

      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && (
        <div className="flex flex-col gap-2">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      )}
      {q.data && !q.data.itens.length && (
        <Empty icon={<NotebookPen className="h-5 w-5" />} title="Nada registrado nos últimos 30 dias">
          Sono, água, sintomas, humor ou qualquer coisa que valha anotar.
        </Empty>
      )}
      {!!q.data?.itens.length && (
        <Card className="divide-y divide-line/60">
          {q.data.itens.map((r) => {
            const t = infoTipo(r.tipo);
            const valor = [r.valorNumerico != null ? `${String(r.valorNumerico).replace('.', ',')}${r.unidade ? ` ${r.unidade}` : ''}` : null, r.escala != null ? `${r.escala}/10` : null].filter(Boolean).join(' · ');
            return (
              <div key={r.id} className="flex gap-3 p-3.5">
                <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
                  <t.icon className="h-5 w-5" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-baseline justify-between gap-2">
                    <p className="truncate font-semibold">{t.l}</p>
                    {valor && <p className="num shrink-0 text-sm font-semibold">{valor}</p>}
                  </div>
                  <p className="text-[13px] text-muted">{quando(r.dataHoraUtc)}</p>
                  {r.descricao && <p className="mt-1 whitespace-pre-wrap break-words text-[14px] text-fg/90">{r.descricao.replaceAll(' • ', '\n')}</p>}
                </div>
              </div>
            );
          })}
        </Card>
      )}

      <NovoRegistroSheet open={novo} onClose={() => setNovo(false)} />
      <FecharDiaSheet open={fechar} onClose={() => setFechar(false)} atual={hoje?.diaFechado ? hoje.percepcaoDoDia ?? null : null} />
    </Page>
  );
}

function NovoRegistroSheet({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const [tipo, setTipo] = useState<string>('Observacao');
  const [valor, setValor] = useState('');
  const [unidade, setUnidade] = useState('');
  const [escala, setEscala] = useState<number | null>(null);
  const [descricao, setDescricao] = useState('');

  useEffect(() => {
    if (!open) return;
    setTipo('Observacao');
    setValor('');
    setUnidade('');
    setEscala(null);
    setDescricao('');
  }, [open]);

  const salvar = useMutation({
    mutationFn: () => {
      const v = valor.trim() ? Number(valor.replace(',', '.')) : null;
      if (v != null && Number.isNaN(v)) throw new Error('Valor inválido.');
      if (v == null && escala == null && !descricao.trim()) throw new Error('Escreva algo, ou informe um valor ou uma nota.');
      return api('/api/portal/me/diario', {
        method: 'POST',
        json: { dataHoraUtc: new Date().toISOString(), tipo, descricao: descricao.trim() || null, valorNumerico: v, unidade: v != null ? unidade.trim() || null : null, escala, imagemUrl: null },
      });
    },
    onSuccess: () => {
      toast('Registrado no diário.');
      qc.invalidateQueries({ queryKey: ['p', 'diario-registros'] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Novo registro"
      footer={
        <Button size="lg" className="w-full" loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar no diário
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Sobre o quê">
          <div className="flex flex-wrap gap-1.5">
            {TIPOS.map((t) => (
              <button
                key={t.v}
                onClick={() => {
                  setTipo(t.v);
                  setUnidade(t.unidade);
                }}
                className={clsx('flex h-9 items-center gap-1.5 rounded-full px-3.5 text-sm font-semibold transition', tipo === t.v ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
              >
                <t.icon className="h-4 w-4" />
                {'l' in t ? t.l : t.v}
              </button>
            ))}
          </div>
        </Field>
        <div className="grid grid-cols-[1fr_96px] gap-2">
          <Field label="Valor (opcional)">
            <input className={inputCls} inputMode="decimal" value={valor} placeholder="Ex.: 7,5" onChange={(e) => setValor(e.target.value)} />
          </Field>
          <Field label="Unidade">
            <input className={inputCls} value={unidade} maxLength={20} placeholder="h, L, min" onChange={(e) => setUnidade(e.target.value)} />
          </Field>
        </div>
        <Field label={`Nota de 0 a 10 (opcional)${escala != null ? ` · ${escala}` : ''}`}>
          <Escala value={escala ?? -1} onChange={(v) => setEscala(v === escala ? null : v)} invertida={tipo === 'Sintoma'} />
        </Field>
        <Field label="Descrição">
          <textarea className={clsx(inputCls, 'min-h-[88px] py-2.5')} value={descricao} maxLength={1000} placeholder="Como foi, o que sentiu, o que mudou." onChange={(e) => setDescricao(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

const EXECUCAO = [
  ['Quase nada', 'Quase nada'],
  ['Parte do plano', 'Parte'],
  ['Maioria do plano', 'Maioria'],
  ['Tudo que estava previsto', 'Tudo'],
] as const;
const DIFICULDADE = ['Leve', 'Adequada', 'Alta', 'Muito alta'] as const;
const ENERGIA = ['Muito baixa', 'Baixa', 'Boa', 'Alta'] as const;

function Chips({ opcoes, value, onChange }: { opcoes: readonly (readonly [string, string])[]; value: string; onChange: (v: string) => void }) {
  return (
    <div className="grid grid-cols-4 gap-1.5">
      {opcoes.map(([v, l]) => (
        <button
          key={v}
          onClick={() => onChange(v === value ? '' : v)}
          className={clsx('min-h-10 rounded-xl px-1.5 text-[13px] font-semibold leading-tight transition', value === v ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
        >
          {l}
        </button>
      ))}
    </div>
  );
}

/** Reflexao do fim do dia. Mesmo formato de resumo que a versao classica grava. */
export function FecharDiaSheet({ open, onClose, atual }: { open: boolean; onClose: () => void; atual: number | null }) {
  const qc = useQueryClient();
  const [nota, setNota] = useState(7);
  const [execucao, setExecucao] = useState('');
  const [dificuldade, setDificuldade] = useState('');
  const [energia, setEnergia] = useState('');
  const [aprendizado, setAprendizado] = useState('');
  const [obs, setObs] = useState('');

  useEffect(() => {
    if (open) setNota(atual ?? 7);
  }, [open, atual]);

  const resumo = useMemo(
    () =>
      [
        execucao && `Execução: ${execucao}`,
        dificuldade && `Dificuldade: ${dificuldade}`,
        energia && `Energia final: ${energia}`,
        aprendizado.trim() && `Aprendizado: ${aprendizado.trim()}`,
        obs.trim() && `Observação: ${obs.trim()}`,
      ]
        .filter(Boolean)
        .join(' • ') || null,
    [execucao, dificuldade, energia, aprendizado, obs],
  );

  const salvar = useMutation({
    mutationFn: () => api<{ xp: number }>('/api/portal/me/fechamento-dia', { method: 'POST', json: { percepcaoDoDia: nota, resumo } }),
    onSuccess: (r) => {
      toast(r?.xp ? `Dia fechado. +${r.xp} XP` : 'Reflexão atualizada.');
      qc.invalidateQueries({ queryKey: ['p', 'diario'] });
      qc.invalidateQueries({ queryKey: ['p', 'diario-registros'] });
      qc.invalidateQueries({ queryKey: ['p', 'home'] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Fechar o dia"
      tall
      footer={
        <Button size="lg" className="w-full" loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {atual != null ? 'Atualizar reflexão' : 'Fechar o dia'}
        </Button>
      }
    >
      <div className="flex flex-col gap-5">
        <p className="text-[15px] text-muted">Registre como foi de verdade. A ideia é aprender com o dia, não dar nota para você.</p>
        <Field label={`Como o dia terminou? · ${nota}/10`}>
          <Escala value={nota} onChange={setNota} />
        </Field>
        <Field label="Quanto do planejado você conseguiu fazer?">
          <Chips opcoes={EXECUCAO} value={execucao} onChange={setExecucao} />
        </Field>
        <Field label="Como foi a dificuldade do dia?">
          <Chips opcoes={DIFICULDADE.map((x) => [x, x] as const)} value={dificuldade} onChange={setDificuldade} />
        </Field>
        <Field label="Como está sua energia agora?">
          <Chips opcoes={ENERGIA.map((x) => [x, x] as const)} value={energia} onChange={setEnergia} />
        </Field>
        <Field label="O que o dia te ensinou? (opcional)">
          <textarea className={clsx(inputCls, 'min-h-[72px] py-2.5')} maxLength={240} value={aprendizado} placeholder="Ex.: treinar mais cedo funcionou melhor." onChange={(e) => setAprendizado(e.target.value)} />
        </Field>
        <Field label="Algo que seu profissional deveria saber? (opcional)">
          <textarea className={clsx(inputCls, 'min-h-[72px] py-2.5')} maxLength={240} value={obs} onChange={(e) => setObs(e.target.value)} />
        </Field>
        <p className="rounded-2xl bg-raised/60 p-3 text-[13px] text-muted">
          <b className="text-fg">Sem punição.</b> Fazer menos ou terminar cansado não diminui nada. Esses registros ajudam a entender padrões e ajustar o acompanhamento.
        </p>
      </div>
    </Sheet>
  );
}
