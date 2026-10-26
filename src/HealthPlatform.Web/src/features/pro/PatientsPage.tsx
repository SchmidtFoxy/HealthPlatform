import { useMutation, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ChevronRight, Plus, Search, UserPlus } from 'lucide-react';
import { FormEvent, useDeferredValue, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Empty, ErrorBox, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { idade, iniciais, tempoRelativo } from '@/lib/format';
import type { Paciente } from '@/lib/types';
import { usePacientes, useSinais } from './queries';

const NIVEL: Record<string, { label: string; tone: 'bad' | 'warn' | 'neutral' | 'ok' }> = {
  RevisarHoje: { label: 'Revisar hoje', tone: 'bad' },
  Observar: { label: 'Observar', tone: 'warn' },
  ContextoPendente: { label: 'Sem check-in', tone: 'neutral' },
  Estavel: { label: 'Estável', tone: 'ok' },
};

function NovoPaciente({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const nav = useNavigate();
  const [f, setF] = useState({ nome: '', dataNascimento: '', sexo: '', telefone: '', email: '' });
  const criar = useMutation({
    mutationFn: () =>
      api<Paciente>('/api/pacientes', {
        method: 'POST',
        json: { nome: f.nome.trim(), cpf: null, dataNascimento: f.dataNascimento || null, sexo: f.sexo || null, telefone: f.telefone || null, email: f.email || null, profissao: null },
      }),
    onSuccess: (p) => {
      qc.invalidateQueries({ queryKey: ['pro'] });
      toast('Paciente cadastrado');
      onClose();
      nav(`/pro/pacientes/${p.id}`);
    },
    onError: (e) => toast(e.message, 'bad'),
  });
  const submit = (e: FormEvent) => {
    e.preventDefault();
    criar.mutate();
  };
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Novo paciente"
      footer={
        <Button size="lg" className="w-full" form="novo-paciente" type="submit" loading={criar.isPending} disabled={f.nome.trim().length < 3}>
          Cadastrar
        </Button>
      }
    >
      <form id="novo-paciente" onSubmit={submit} className="flex flex-col gap-4">
        <Field label="Nome completo">
          <input id="np-nome" className={inputCls} value={f.nome} onChange={(e) => setF({ ...f, nome: e.target.value })} autoFocus required />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Nascimento">
            <input id="np-nasc" type="date" className={inputCls} value={f.dataNascimento} onChange={(e) => setF({ ...f, dataNascimento: e.target.value })} />
          </Field>
          <Field label="Sexo">
            <select id="np-sexo" className={inputCls} value={f.sexo} onChange={(e) => setF({ ...f, sexo: e.target.value })}>
              <option value="">—</option>
              <option>Feminino</option>
              <option>Masculino</option>
              <option>Outro</option>
            </select>
          </Field>
        </div>
        <Field label="E-mail" hint="Usado para liberar o acesso do paciente ao app.">
          <input id="np-email" type="email" className={inputCls} value={f.email} onChange={(e) => setF({ ...f, email: e.target.value })} />
        </Field>
        <Field label="Telefone">
          <input id="np-tel" inputMode="tel" className={inputCls} value={f.telefone} onChange={(e) => setF({ ...f, telefone: e.target.value })} />
        </Field>
      </form>
    </Sheet>
  );
}

export function PatientsPage() {
  const [busca, setBusca] = useState('');
  const termo = useDeferredValue(busca);
  const [tag, setTag] = useState<string | null>(null);
  const [novo, setNovo] = useState(false);
  const q = usePacientes(termo);
  const sinaisQ = useSinais();
  const sinais = useMemo(() => new Map((sinaisQ.data?.sinais ?? []).map((x) => [x.pacienteId, x])), [sinaisQ.data]);
  const lista = q.data?.itens ?? [];
  const tags = useMemo(() => [...new Set(lista.flatMap((p) => p.tags))].sort(), [lista]);
  const filtrados = tag ? lista.filter((p) => p.tags.includes(tag)) : lista;

  return (
    <Page wide>
      <PageHeader
        title="Pacientes"
        subtitle={q.data ? `${q.data.total ?? lista.length} em acompanhamento` : undefined}
        action={
          <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setNovo(true)}>
            Novo
          </Button>
        }
      />
      <div className="sticky top-0 z-20 -mx-gutter bg-bg/90 px-gutter pb-3 pt-safe backdrop-blur-xl">
        <label className="relative block">
          <Search className="pointer-events-none absolute left-3.5 top-3.5 h-5 w-5 text-faint" />
          <input id="busca-pacientes" type="search" placeholder="Buscar por nome, e-mail ou CPF" value={busca} onChange={(e) => setBusca(e.target.value)} className={inputCls + ' h-12 rounded-2xl pl-11'} />
        </label>
        {tags.length > 0 && (
          <div className="no-scrollbar -mx-gutter mt-3 flex gap-2 overflow-x-auto px-gutter">
            {[null, ...tags].map((t) => (
              <button
                key={t ?? 'todos'}
                onClick={() => setTag(t)}
                className={clsx('h-8 shrink-0 rounded-full border px-3 text-[13px] font-semibold transition', tag === t ? 'border-accent bg-accent text-accent-ink' : 'border-line text-muted')}
              >
                {t ?? 'Todos'}
              </button>
            ))}
          </div>
        )}
      </div>

      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading ? (
        <div className="flex flex-col gap-2">
          {[0, 1, 2, 3, 4].map((i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      ) : filtrados.length === 0 ? (
        <Empty icon={<UserPlus className="h-5 w-5" />} title={busca ? 'Ninguém com esse nome' : 'Nenhum paciente ainda'} action={<Button onClick={() => setNovo(true)}>Cadastrar paciente</Button>} />
      ) : (
        <div className="grid grid-cols-1 gap-2 md:grid-cols-2 md:gap-3 xl:grid-cols-3">
          {filtrados.map((p) => {
            const a = idade(p.dataNascimento);
            const sinal = sinais.get(p.id);
            const nivel = sinal ? (NIVEL[sinal.nivel] ?? NIVEL.Estavel) : NIVEL.Estavel;
            return (
              <Link key={p.id} to={`/pro/pacientes/${p.id}`} className="group flex flex-col gap-2.5 rounded-xl2 border border-line bg-surface p-4 transition hover:border-accent/40">
                <div className="flex items-center gap-3">
                  <div className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-accent/15 font-display text-[15px] font-semibold text-accent">{iniciais(p.nome)}</div>
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold">{p.nome}</p>
                    <p className="truncate text-[13px] text-muted">{[a != null && `${a} anos`, p.profissao].filter(Boolean).join(' · ') || p.email}</p>
                  </div>
                  <ChevronRight className="h-5 w-5 shrink-0 text-faint transition group-hover:translate-x-0.5" />
                </div>
                <div className="flex flex-wrap items-center gap-1.5">
                  <Pill tone={nivel.tone}>{nivel.label}</Pill>
                  {p.statusAcompanhamento !== 'Ativo' && <Pill tone="warn">{p.statusAcompanhamento}</Pill>}
                  {p.tags.slice(0, 2).map((t) => (
                    <Pill key={t}>{t}</Pill>
                  ))}
                </div>
                <p className="num flex flex-wrap gap-x-3 text-[12px] text-muted">
                  {sinal ? (
                    <>
                      <span>
                        Prontidão{' '}
                        <b className={clsx(sinal.prontidaoScore == null ? 'text-faint' : sinal.prontidaoScore >= 75 ? 'text-ok' : sinal.prontidaoScore >= 55 ? 'text-warn' : 'text-bad')}>{sinal.prontidaoScore ?? '—'}</b>
                      </span>
                      <span>
                        <b className="text-fg">{sinal.treinosUltimos7Dias}</b> {sinal.treinosUltimos7Dias === 1 ? 'treino' : 'treinos'} em 7 dias
                      </span>
                      <span>Check-in {tempoRelativo(sinal.ultimoCheckInData)}</span>
                    </>
                  ) : (
                    <span>Sem alertas nos últimos 7 dias</span>
                  )}
                </p>
              </Link>
            );
          })}
        </div>
      )}
      <NovoPaciente open={novo} onClose={() => setNovo(false)} />
    </Page>
  );
}
