import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ArrowDown, ArrowUp, CalendarRange, Copy, Plus, Trash2, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { hojeIso } from '@/lib/format';
import { Busca, Linha, useModelos } from './BibliotecaPage';
import { usePacientes } from './queries';

// Programas de treino: periodizacao reutilizavel em fases (adaptacao, forca, deload...).
// Cada fase dura N semanas e liga dias da semana a treinos-modelo. Ao aplicar num paciente,
// cada fase vira um plano de treino com datas encadeadas e uma fase de treino no acompanhamento.

export interface ProgramaDia {
  dia: string;
  modeloPlanoTreinoId: string | null;
  observacoes: string | null;
}

export interface ProgramaFase {
  nome: string;
  semanas: number;
  ordem: number;
  observacoes: string | null;
  dias: ProgramaDia[];
}

export interface Programa {
  id: string;
  nome: string;
  objetivo: string | null;
  descricao: string | null;
  observacoes: string | null;
  ativo: boolean;
  profissionalNome: string | null;
  fases: ProgramaFase[];
  semanas: number;
  sessoes: number;
  modelosDistintos: number;
}

export const DIAS_SEMANA = ['Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado', 'Domingo'];

const erroToast = (e: unknown) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');

export const useProgramas = (incluirInativos = false) =>
  useQuery({ queryKey: ['pro', 'programas', incluirInativos], queryFn: () => api<Programa[]>(`/api/programas-treino?incluirInativos=${incluirInativos}`) });

const plural = (n: number, um: string, varios: string) => `${n} ${n === 1 ? um : varios}`;
export const resumoPrograma = (p: Programa) =>
  [plural(p.fases.length, 'fase', 'fases'), plural(p.semanas, 'semana', 'semanas'), plural(p.sessoes, 'treino', 'treinos'), p.objetivo].filter(Boolean).join(' · ');

// ---------------------------------------------------------------- Lista na Biblioteca

export function ProgramasLista({ inativos }: { inativos: boolean }) {
  const [busca, setBusca] = useState('');
  const [aberto, setAberto] = useState<Programa | 'novo' | null>(null);
  const q = useProgramas(inativos);
  const termo = busca.trim().toLowerCase();
  const lista = (q.data ?? []).filter((p) => !termo || `${p.nome} ${p.objetivo ?? ''} ${p.descricao ?? ''}`.toLowerCase().includes(termo));
  return (
    <>
      <div className="mb-3 flex items-center gap-2">
        <div className="min-w-0 flex-1">
          <Busca value={busca} onChange={setBusca} placeholder="Buscar programa" />
        </div>
        <Button icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
          Novo
        </Button>
      </div>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Empty icon={<CalendarRange className="h-5 w-5" />} title={q.data?.length ? 'Nada encontrado' : 'Nenhum programa'}>
          Monte uma periodização em fases (adaptação, força, deload...) usando seus treinos-modelo e aplique em qualquer paciente de uma vez.
        </Empty>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((p) => (
            <Linha key={p.id} titulo={p.nome} sub={resumoPrograma(p)} extra={!p.ativo && <Pill>Inativo</Pill>} onClick={() => setAberto(p)} />
          ))}
        </Card>
      )}
      <ProgramaEditor programa={aberto === 'novo' ? null : aberto} open={aberto !== null} onClose={() => setAberto(null)} />
    </>
  );
}

// ---------------------------------------------------------------- Editor

type FaseForm = ProgramaFase & { k: number; semanasTxt: string };
let seq = 0;
const novaFase = (n: number, base?: ProgramaFase): FaseForm => ({
  k: ++seq,
  nome: base?.nome ?? `Fase ${n}`,
  semanas: base?.semanas ?? 4,
  semanasTxt: String(base?.semanas ?? 4),
  ordem: n,
  observacoes: base?.observacoes ?? null,
  dias: base?.dias.map((d) => ({ ...d })) ?? ['Segunda', 'Terça', 'Quinta', 'Sexta'].map((dia) => ({ dia, modeloPlanoTreinoId: null, observacoes: null })),
});

function ProgramaEditor({ programa, open, onClose }: { programa: Programa | null; open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const modelos = useModelos('treino');
  const [nome, setNome] = useState('');
  const [objetivo, setObjetivo] = useState('');
  const [descricao, setDescricao] = useState('');
  const [observacoes, setObservacoes] = useState('');
  const [fases, setFases] = useState<FaseForm[]>([]);
  const [aplicar, setAplicar] = useState(false);

  useEffect(() => {
    if (!open) return;
    setNome(programa?.nome ?? '');
    setObjetivo(programa?.objetivo ?? '');
    setDescricao(programa?.descricao ?? '');
    setObservacoes(programa?.observacoes ?? '');
    setFases(programa?.fases.length ? [...programa.fases].sort((a, b) => a.ordem - b.ordem).map((f, i) => novaFase(i + 1, f)) : [novaFase(1)]);
    setAplicar(false);
  }, [open, programa]);

  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'programas'] });
  const corpo = (ativo: boolean) => ({
    nome: nome.trim(),
    objetivo: objetivo.trim() || null,
    descricao: descricao.trim() || null,
    observacoes: observacoes.trim() || null,
    ativo,
    fases: fases.map((f, i) => ({
      nome: f.nome.trim(),
      semanas: Math.min(52, Math.max(1, Number(f.semanasTxt) || 1)),
      ordem: i + 1,
      observacoes: f.observacoes?.trim() || null,
      dias: f.dias.map((d) => ({ dia: d.dia, modeloPlanoTreinoId: d.modeloPlanoTreinoId || null, observacoes: d.observacoes?.trim() || null })),
    })),
  });
  const salvar = useMutation({
    mutationFn: (ativo: boolean) => api<Programa>(programa ? `/api/programas-treino/${programa.id}` : '/api/programas-treino', { method: programa ? 'PUT' : 'POST', json: corpo(ativo) }),
    onSuccess: (_, ativo) => {
      toast(!programa ? 'Programa criado.' : ativo === programa.ativo ? 'Programa salvo.' : ativo ? 'Programa reativado.' : 'Programa desativado.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });
  const duplicar = useMutation({
    mutationFn: () => api(`/api/programas-treino/${programa!.id}/duplicar`, { method: 'POST' }),
    onSuccess: () => {
      toast('Cópia criada na biblioteca.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });
  const excluir = useMutation({
    mutationFn: () => api(`/api/programas-treino/${programa!.id}`, { method: 'DELETE' }),
    onSuccess: () => {
      toast('Programa excluído. Planos já aplicados nos pacientes continuam.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });

  const lista = (modelos.data ?? []).filter((m) => m.ativo);
  const vinculados = fases.reduce((s, f) => s + f.dias.filter((d) => d.modeloPlanoTreinoId).length, 0);
  const semanas = fases.reduce((s, f) => s + Math.max(1, Number(f.semanasTxt) || 1), 0);
  const valido = !!nome.trim() && fases.length > 0 && fases.every((f) => f.nome.trim()) && vinculados > 0;
  const mudarFase = (k: number, m: Partial<FaseForm>) => setFases((l) => l.map((f) => (f.k === k ? { ...f, ...m } : f)));
  const mover = (i: number, d: number) =>
    setFases((l) => {
      const j = i + d;
      if (j < 0 || j >= l.length) return l;
      const n = [...l];
      [n[i], n[j]] = [n[j], n[i]];
      return n;
    });

  return (
    <>
      <Sheet
        open={open && !aplicar}
        onClose={onClose}
        tall
        title={programa ? 'Programa de treino' : 'Novo programa de treino'}
        footer={
          <div className="flex flex-col gap-2">
            {programa?.ativo && (
              <Button size="lg" className="w-full" icon={<Plus className="h-5 w-5" />} onClick={() => setAplicar(true)}>
                Aplicar num paciente
              </Button>
            )}
            <Button size="lg" variant={programa?.ativo ? 'soft' : 'primary'} className="w-full" disabled={!valido} loading={salvar.isPending && salvar.variables === (programa?.ativo ?? true)} onClick={() => salvar.mutate(programa?.ativo ?? true)}>
              {programa ? 'Salvar alterações' : 'Criar programa'}
            </Button>
          </div>
        }
      >
        <div className="flex flex-col gap-4">
          <Field label="Nome">
            <input className={inputCls} value={nome} maxLength={160} placeholder="Ex.: Hipertrofia 12 semanas" onChange={(e) => setNome(e.target.value)} />
          </Field>
          <Field label="Objetivo">
            <input className={inputCls} value={objetivo} maxLength={160} placeholder="Ex.: Ganho de massa, preparação para prova" onChange={(e) => setObjetivo(e.target.value)} />
          </Field>
          <Field label="Descrição">
            <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={descricao} maxLength={1000} onChange={(e) => setDescricao(e.target.value)} />
          </Field>
          <Field label="Orientações gerais">
            <input className={inputCls} value={observacoes} maxLength={1000} placeholder="Aquecimento, progressão de carga, sinais para reduzir..." onChange={(e) => setObservacoes(e.target.value)} />
          </Field>

          <div className="flex items-end justify-between gap-3">
            <div>
              <p className="font-semibold">Fases</p>
              <p className="text-[13px] text-muted">
                {plural(fases.length, 'fase', 'fases')} · {plural(semanas, 'semana', 'semanas')} no total
              </p>
            </div>
          </div>

          {modelos.isSuccess && !lista.length && (
            <p className="rounded-xl bg-warn/10 p-3 text-[13px] text-warn">
              Você ainda não tem treinos-modelo. Abra um treino de paciente e use "Salvar como modelo"; depois ele aparece aqui para montar as fases.
            </p>
          )}

          {fases.map((f, i) => (
            <Card key={f.k} className="p-3.5">
              <div className="mb-3 flex items-center gap-1">
                <span className="text-[11px] font-bold uppercase tracking-wider text-accent">Fase {i + 1}</span>
                <span className="flex-1" />
                <IconeBtn rotulo="Subir fase" disabled={i === 0} onClick={() => mover(i, -1)}>
                  <ArrowUp className="h-4 w-4" />
                </IconeBtn>
                <IconeBtn rotulo="Descer fase" disabled={i === fases.length - 1} onClick={() => mover(i, 1)}>
                  <ArrowDown className="h-4 w-4" />
                </IconeBtn>
                <IconeBtn rotulo="Duplicar fase" onClick={() => setFases((l) => [...l.slice(0, i + 1), novaFase(i + 2, { ...f, nome: `${f.nome} (cópia)`, semanas: Number(f.semanasTxt) || 1 }), ...l.slice(i + 1)])}>
                  <Copy className="h-4 w-4" />
                </IconeBtn>
                <IconeBtn rotulo="Remover fase" disabled={fases.length === 1} onClick={() => setFases((l) => l.filter((x) => x.k !== f.k))}>
                  <Trash2 className="h-4 w-4" />
                </IconeBtn>
              </div>
              <div className="grid grid-cols-[1fr_96px] gap-3">
                <Field label="Nome da fase">
                  <input className={inputCls} value={f.nome} maxLength={120} placeholder="Ex.: Adaptação" onChange={(e) => mudarFase(f.k, { nome: e.target.value })} />
                </Field>
                <Field label="Semanas">
                  <input className={inputCls} inputMode="numeric" value={f.semanasTxt} onChange={(e) => mudarFase(f.k, { semanasTxt: e.target.value.replace(/\D/g, '').slice(0, 2) })} />
                </Field>
              </div>
              <div className="mt-3">
                <Field label="Orientações da fase">
                  <input className={inputCls} value={f.observacoes ?? ''} maxLength={500} placeholder="Volume, intensidade, deload..." onChange={(e) => mudarFase(f.k, { observacoes: e.target.value })} />
                </Field>
              </div>
              <p className="mb-2 mt-4 text-[13px] font-semibold text-muted">Semana-tipo</p>
              <div className="flex flex-col gap-2">
                {f.dias.map((d, di) => (
                  <div key={di} className="rounded-xl bg-raised/60 p-2.5">
                    <div className="flex items-center gap-2">
                      <select aria-label="Dia" className={clsx(inputCls, 'w-[118px] shrink-0')} value={d.dia} onChange={(e) => mudarFase(f.k, { dias: f.dias.map((x, xi) => (xi === di ? { ...x, dia: e.target.value } : x)) })}>
                        {(DIAS_SEMANA.includes(d.dia) ? DIAS_SEMANA : [d.dia, ...DIAS_SEMANA]).map((x) => (
                          <option key={x}>{x}</option>
                        ))}
                      </select>
                      <select
                        aria-label="Treino-modelo"
                        className={clsx(inputCls, 'min-w-0 flex-1')}
                        value={d.modeloPlanoTreinoId ?? ''}
                        onChange={(e) => mudarFase(f.k, { dias: f.dias.map((x, xi) => (xi === di ? { ...x, modeloPlanoTreinoId: e.target.value || null } : x)) })}
                      >
                        <option value="">Sem treino</option>
                        {(modelos.data ?? [])
                          .filter((m) => m.ativo || m.id === d.modeloPlanoTreinoId)
                          .map((m) => (
                            <option key={m.id} value={m.id}>
                              {m.nome}
                            </option>
                          ))}
                      </select>
                      <IconeBtn rotulo="Tirar dia" onClick={() => mudarFase(f.k, { dias: f.dias.filter((_, xi) => xi !== di) })}>
                        <X className="h-4 w-4" />
                      </IconeBtn>
                    </div>
                    <input
                      aria-label="Nota do dia"
                      className={clsx(inputCls, 'mt-2 h-9 text-[14px]')}
                      value={d.observacoes ?? ''}
                      maxLength={300}
                      placeholder="Nota do dia (opcional)"
                      onChange={(e) => mudarFase(f.k, { dias: f.dias.map((x, xi) => (xi === di ? { ...x, observacoes: e.target.value } : x)) })}
                    />
                  </div>
                ))}
                <Button
                  variant="ghost"
                  size="sm"
                  icon={<Plus className="h-4 w-4" />}
                  onClick={() => mudarFase(f.k, { dias: [...f.dias, { dia: DIAS_SEMANA.find((x) => !f.dias.some((d) => d.dia === x)) ?? 'Segunda', modeloPlanoTreinoId: null, observacoes: null }] })}
                >
                  Adicionar dia
                </Button>
              </div>
            </Card>
          ))}
          <Button variant="outline" icon={<Plus className="h-4 w-4" />} onClick={() => setFases((l) => [...l, novaFase(l.length + 1)])}>
            Adicionar fase
          </Button>
          {!vinculados && <p className="text-[13px] text-muted">Ligue pelo menos um dia a um treino-modelo para salvar.</p>}

          {programa && (
            <div className="mt-2 flex flex-col gap-1 border-t border-line/60 pt-3">
              <Button variant="ghost" icon={<Copy className="h-4 w-4" />} loading={duplicar.isPending} onClick={() => duplicar.mutate()}>
                Duplicar programa
              </Button>
              <Button variant="ghost" className={programa.ativo ? 'text-muted' : 'text-accent'} disabled={!valido} loading={salvar.isPending && salvar.variables !== programa.ativo} onClick={() => salvar.mutate(!programa.ativo)}>
                {programa.ativo ? 'Desativar programa' : 'Reativar programa'}
              </Button>
              <Button variant="ghost" className="text-bad" loading={excluir.isPending} onClick={() => window.confirm(`Excluir "${programa.nome}"? Planos já aplicados nos pacientes continuam.`) && excluir.mutate()}>
                Excluir programa
              </Button>
            </div>
          )}
        </div>
      </Sheet>
      <AplicarProgramaSheet programa={aplicar ? programa : null} open={aplicar} onClose={onClose} />
    </>
  );
}

function IconeBtn({ rotulo, onClick, disabled, children }: { rotulo: string; onClick: () => void; disabled?: boolean; children: React.ReactNode }) {
  return (
    <button type="button" aria-label={rotulo} title={rotulo} disabled={disabled} onClick={onClick} className="grid h-9 w-9 shrink-0 place-items-center rounded-full text-muted transition hover:bg-raised disabled:opacity-30">
      {children}
    </button>
  );
}

// ---------------------------------------------------------------- Aplicar no paciente

/**
 * Aplica um programa num paciente: cada fase vira um plano de treino com datas encadeadas.
 * Sem pacienteId, pede o paciente; sem programa, pede o programa.
 */
export function AplicarProgramaSheet({ programa, pacienteId, open, onClose }: { programa?: Programa | null; pacienteId?: string; open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const programas = useProgramas();
  const [escolhido, setEscolhido] = useState<Programa | null>(null);
  const [paciente, setPaciente] = useState<{ id: string; nome: string } | null>(null);
  const [busca, setBusca] = useState('');
  const [inicio, setInicio] = useState(hojeIso());
  const [concluir, setConcluir] = useState(true);
  const [ativar, setAtivar] = useState(true);
  const [obs, setObs] = useState('');
  const pacientes = usePacientes(busca);
  const p = programa ?? escolhido;
  const pid = pacienteId ?? paciente?.id;

  useEffect(() => {
    if (!open) return;
    setEscolhido(null);
    setPaciente(null);
    setBusca('');
    setInicio(hojeIso());
    setConcluir(true);
    setAtivar(true);
    setObs('');
  }, [open]);

  const aplicar = useMutation({
    mutationFn: () =>
      api<{ fasesCriadas: unknown[]; dataFim: string }>(`/api/pacientes/${pid}/programas-treino/${p!.id}/atribuir`, {
        method: 'POST',
        json: { dataInicio: inicio, concluirPlanosAtivos: ativar && concluir, ativarPrimeiraFase: ativar, observacoes: obs.trim() || null },
      }),
    onSuccess: (r) => {
      toast(`Programa aplicado: ${plural(r.fasesCriadas.length, 'fase criada', 'fases criadas')}.`);
      qc.invalidateQueries({ queryKey: ['pro', 'treinos', pid] });
      qc.invalidateQueries({ queryKey: ['pro', 'fases'] });
      qc.invalidateQueries({ queryKey: ['pro', 'status-fases', pid] });
      onClose();
    },
    onError: erroToast,
  });

  const disponiveis = programas.data ?? [];
  const fim = p ? new Date(new Date(`${inicio}T12:00:00`).getTime() + (p.semanas * 7 - 1) * 86400000) : null;
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Aplicar programa de treino"
      footer={
        <Button size="lg" className="w-full" disabled={!p || !pid || !inicio} loading={aplicar.isPending} onClick={() => aplicar.mutate()}>
          Aplicar programa
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        {!programa &&
          (programas.isLoading ? (
            <Skeleton className="h-24" />
          ) : !disponiveis.length ? (
            <p className="rounded-xl bg-raised/60 p-3 text-sm text-muted">
              Nenhum programa ainda.{' '}
              <Link to="/pro/biblioteca?aba=modelos&tipo=programa" className="font-semibold text-accent">
                Montar na Biblioteca
              </Link>
            </p>
          ) : (
            <Field label="Programa" group>
              <div className="flex flex-col gap-2">
                {disponiveis.map((x) => (
                  <button
                    key={x.id}
                    type="button"
                    aria-pressed={escolhido?.id === x.id}
                    onClick={() => setEscolhido(x)}
                    className={clsx('rounded-xl border p-3 text-left transition', escolhido?.id === x.id ? 'border-accent bg-accent/10' : 'border-line hover:bg-raised')}
                  >
                    <span className="block font-semibold">{x.nome}</span>
                    <span className="block text-[13px] text-muted">{resumoPrograma(x)}</span>
                  </button>
                ))}
              </div>
            </Field>
          ))}
        {!pacienteId && (
          <Field label="Paciente" group>
            {paciente ? (
              <div className="flex items-center justify-between rounded-xl border border-accent bg-accent/10 p-3">
                <span className="font-semibold">{paciente.nome}</span>
                <button type="button" className="text-sm font-semibold text-accent" onClick={() => setPaciente(null)}>
                  Trocar
                </button>
              </div>
            ) : (
              <div className="flex flex-col gap-2">
                <Busca value={busca} onChange={setBusca} placeholder="Buscar paciente" />
                <Card className="max-h-56 divide-y divide-line/60 overflow-y-auto">
                  {(pacientes.data?.itens ?? []).slice(0, 30).map((x) => (
                    <button key={x.id} type="button" onClick={() => setPaciente({ id: x.id, nome: x.nome })} className="block w-full p-3 text-left text-[15px] hover:bg-raised/40">
                      {x.nome}
                    </button>
                  ))}
                  {pacientes.data && !pacientes.data.itens.length && <p className="p-3 text-sm text-muted">Nenhum paciente encontrado.</p>}
                </Card>
              </div>
            )}
          </Field>
        )}
        {p && (
          <>
            <div className="flex flex-col gap-1.5 rounded-xl bg-raised/60 p-3">
              {[...p.fases]
                .sort((a, b) => a.ordem - b.ordem)
                .map((f, i) => (
                  <p key={i} className="text-[13px]">
                    <span className="font-semibold">{f.nome}</span>
                    <span className="text-muted">
                      {' '}
                      · {plural(f.semanas, 'semana', 'semanas')} · {f.dias.filter((d) => d.modeloPlanoTreinoId).map((d) => d.dia.slice(0, 3)).join(', ') || 'sem treinos'}
                    </span>
                  </p>
                ))}
            </div>
            <Field label="Começa em" hint={fim ? `Termina em ${fim.toLocaleDateString('pt-BR')}` : undefined}>
              <input type="date" className={inputCls} value={inicio} onChange={(e) => setInicio(e.target.value)} />
            </Field>
            <Check value={ativar} onChange={setAtivar}>
              Liberar a primeira fase já ativa para o paciente
            </Check>
            {ativar && (
              <Check value={concluir} onChange={setConcluir}>
                Arquivar os treinos ativos atuais
              </Check>
            )}
            <Field label="Observação para o paciente">
              <input className={inputCls} value={obs} maxLength={500} placeholder="Opcional" onChange={(e) => setObs(e.target.value)} />
            </Field>
            <p className="text-[13px] text-muted">Cada fase vira um plano de treino com as datas encadeadas. As fases seguintes ficam planejadas e você ajusta qualquer uma depois.</p>
          </>
        )}
      </div>
    </Sheet>
  );
}

function Check({ value, onChange, children }: { value: boolean; onChange: (v: boolean) => void; children: React.ReactNode }) {
  return (
    <label className="flex items-start gap-3 text-[14px]">
      <input type="checkbox" checked={value} onChange={(e) => onChange(e.target.checked)} className="mt-0.5 h-4 w-4 shrink-0 accent-[rgb(var(--accent))]" />
      <span>{children}</span>
    </label>
  );
}
