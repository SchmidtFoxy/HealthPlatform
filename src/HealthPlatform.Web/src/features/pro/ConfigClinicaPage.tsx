import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ArrowDown, ArrowUp, ClipboardList, FlaskConical, Plus, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, Segmented, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { Busca, Linha } from './BibliotecaPage';
import { Chips } from './CuidadosPro';

// Configuracao clinica do profissional: perguntas proprias que entram em toda anamnese
// e o catalogo de marcadores usado ao lancar exames laboratoriais.

const erroToast = (e: unknown) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');

function Inativos({ value, onChange, rotulo }: { value: boolean; onChange: (v: boolean) => void; rotulo: string }) {
  return (
    <label className="flex shrink-0 items-center gap-2 text-[13px] text-muted">
      <input type="checkbox" checked={value} onChange={(e) => onChange(e.target.checked)} className="h-4 w-4 accent-[rgb(var(--accent))]" />
      {rotulo}
    </label>
  );
}

type Aba = 'anamnese' | 'marcadores';

export function ConfigClinicaPage() {
  const [params, setParams] = useSearchParams();
  const aba = (params.get('aba') as Aba) || 'anamnese';
  return (
    <Page>
      <PageHeader back="/pro/perfil" title="Anamnese e exames" subtitle="Suas perguntas de anamnese e os marcadores usados nos exames." />
      <Segmented<Aba>
        value={aba}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'anamnese', label: 'Perguntas' },
          { value: 'marcadores', label: 'Marcadores' },
        ]}
      />
      <div className="mt-4">{aba === 'anamnese' ? <Perguntas /> : <Marcadores />}</div>
    </Page>
  );
}

// ---------------------------------------------------------------- Perguntas de anamnese

interface Pergunta {
  id: string;
  texto: string;
  tipoResposta: string;
  opcoes: string[];
  ordem: number;
  ativa: boolean;
}

const TIPOS = [
  { value: 'Texto', label: 'Texto livre' },
  { value: 'Numero', label: 'Número' },
  { value: 'SimNao', label: 'Sim ou não' },
  { value: 'Escala', label: 'Escala 0 a 10' },
  { value: 'Opcao', label: 'Opções' },
];
const tipoRotulo = (t: string) => TIPOS.find((x) => x.value === t)?.label ?? t;

function Perguntas() {
  const qc = useQueryClient();
  const [inativas, setInativas] = useState(false);
  const [aberta, setAberta] = useState<Pergunta | 'nova' | null>(null);
  const q = useQuery({ queryKey: ['pro', 'perguntas-anamnese', 'config', inativas], queryFn: () => api<Pergunta[]>(`/api/anamnese/perguntas?incluirInativas=${inativas}`) });
  const lista = [...(q.data ?? [])].sort((a, b) => a.ordem - b.ordem);
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'perguntas-anamnese'] });

  const mover = useMutation({
    mutationFn: async ({ i, d }: { i: number; d: number }) => {
      // Regrava a ordem em sequencia (1, 2, 3...) ja com a troca, para nao herdar empates.
      const nova = [...lista];
      [nova[i], nova[i + d]] = [nova[i + d], nova[i]];
      for (const [k, p] of nova.entries())
        if (p.ordem !== k + 1) await api(`/api/anamnese/perguntas/${p.id}`, { method: 'PUT', json: { texto: p.texto, tipoResposta: p.tipoResposta, opcoes: p.opcoes, ordem: k + 1 } });
    },
    onSuccess: invalidar,
    onError: erroToast,
  });

  return (
    <>
      <div className="mb-3 flex items-center justify-between gap-3">
        <Inativos value={inativas} onChange={setInativas} rotulo="Mostrar desativadas" />
        <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setAberta('nova')}>
          Pergunta
        </Button>
      </div>
      <p className="mb-3 text-[13px] text-muted">Estas perguntas aparecem no fim de toda anamnese que você registra, além dos campos padrão.</p>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Empty icon={<ClipboardList className="h-5 w-5" />} title="Nenhuma pergunta própria">
          Ex.: "Qual o horário que você costuma treinar?" ou "Já fez dieta com acompanhamento antes?"
        </Empty>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((p, i) => (
            <div key={p.id} className="flex items-center">
              <div className="min-w-0 flex-1">
                <Linha titulo={p.texto} sub={[tipoRotulo(p.tipoResposta), p.opcoes.length ? p.opcoes.join(' / ') : null].filter(Boolean).join(' · ')} extra={!p.ativa && <Pill>Desativada</Pill>} onClick={() => setAberta(p)} />
              </div>
              {!inativas && lista.length > 1 && (
                <div className="flex shrink-0 flex-col pr-1">
                  <button aria-label="Subir pergunta" disabled={i === 0 || mover.isPending} onClick={() => mover.mutate({ i, d: -1 })} className="grid h-8 w-8 place-items-center text-muted disabled:opacity-30">
                    <ArrowUp className="h-4 w-4" />
                  </button>
                  <button aria-label="Descer pergunta" disabled={i === lista.length - 1 || mover.isPending} onClick={() => mover.mutate({ i, d: 1 })} className="grid h-8 w-8 place-items-center text-muted disabled:opacity-30">
                    <ArrowDown className="h-4 w-4" />
                  </button>
                </div>
              )}
            </div>
          ))}
        </Card>
      )}
      <PerguntaSheet pergunta={aberta === 'nova' ? null : aberta} open={aberta !== null} onClose={() => setAberta(null)} onSalvo={invalidar} />
    </>
  );
}

function PerguntaSheet({ pergunta, open, onClose, onSalvo }: { pergunta: Pergunta | null; open: boolean; onClose: () => void; onSalvo: () => void }) {
  const [texto, setTexto] = useState('');
  const [tipo, setTipo] = useState('Texto');
  const [opcoes, setOpcoes] = useState<string[]>([]);
  const [nova, setNova] = useState('');
  useEffect(() => {
    if (!open) return;
    setTexto(pergunta?.texto ?? '');
    setTipo(pergunta?.tipoResposta ?? 'Texto');
    setOpcoes(pergunta?.opcoes ?? []);
    setNova('');
  }, [open, pergunta]);

  const salvar = useMutation({
    mutationFn: () =>
      api(pergunta ? `/api/anamnese/perguntas/${pergunta.id}` : '/api/anamnese/perguntas', {
        method: pergunta ? 'PUT' : 'POST',
        json: { texto: texto.trim(), tipoResposta: tipo, opcoes: tipo === 'Opcao' ? opcoes : [], ordem: pergunta?.ordem ?? null },
      }),
    onSuccess: () => {
      toast(pergunta ? 'Pergunta salva.' : 'Pergunta adicionada à anamnese.');
      onSalvo();
      onClose();
    },
    onError: erroToast,
  });
  const ativar = useMutation({
    mutationFn: () => (pergunta!.ativa ? api(`/api/anamnese/perguntas/${pergunta!.id}`, { method: 'DELETE' }) : api(`/api/anamnese/perguntas/${pergunta!.id}/reativar`, { method: 'POST' })),
    onSuccess: () => {
      toast(pergunta!.ativa ? 'Pergunta desativada. Respostas antigas continuam no prontuário.' : 'Pergunta reativada.');
      onSalvo();
      onClose();
    },
    onError: erroToast,
  });

  const addOpcao = () => {
    const v = nova.trim();
    if (v && !opcoes.includes(v)) setOpcoes((l) => [...l, v]);
    setNova('');
  };
  const valido = !!texto.trim() && (tipo !== 'Opcao' || opcoes.length >= 2);
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title={pergunta ? 'Pergunta da anamnese' : 'Nova pergunta'}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {pergunta ? 'Salvar' : 'Adicionar'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Pergunta">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={texto} maxLength={500} placeholder="Ex.: Como está sua disposição pela manhã?" onChange={(e) => setTexto(e.target.value)} />
        </Field>
        <Field label="Tipo de resposta" group>
          <Chips value={tipo} onChange={setTipo} options={TIPOS} />
        </Field>
        {tipo === 'Opcao' && (
          <Field label="Opções" group hint="Pelo menos duas.">
            <div className="flex flex-col gap-2">
              {opcoes.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {opcoes.map((o) => (
                    <span key={o} className="inline-flex items-center gap-1 rounded-full bg-raised py-1 pl-3 pr-1 text-[13px] font-semibold">
                      {o}
                      <button aria-label={`Tirar ${o}`} onClick={() => setOpcoes((l) => l.filter((x) => x !== o))} className="grid h-6 w-6 place-items-center rounded-full text-muted hover:bg-line/60">
                        <X className="h-3.5 w-3.5" />
                      </button>
                    </span>
                  ))}
                </div>
              )}
              <div className="flex gap-2">
                <input className={clsx(inputCls, 'min-w-0 flex-1')} value={nova} maxLength={120} placeholder="Nova opção" onChange={(e) => setNova(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addOpcao())} />
                <Button variant="soft" onClick={addOpcao} disabled={!nova.trim()}>
                  Incluir
                </Button>
              </div>
            </div>
          </Field>
        )}
        {pergunta && (
          <Button variant="ghost" className={pergunta.ativa ? 'text-muted' : 'text-accent'} loading={ativar.isPending} onClick={() => ativar.mutate()}>
            {pergunta.ativa ? 'Desativar pergunta' : 'Reativar pergunta'}
          </Button>
        )}
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Marcadores de exame

interface Marcador {
  id: string;
  nome: string;
  categoria: string | null;
  unidadePadrao: string | null;
  ativo: boolean;
}

function Marcadores() {
  const qc = useQueryClient();
  const [inativos, setInativos] = useState(false);
  const [busca, setBusca] = useState('');
  const [aberto, setAberto] = useState<Marcador | 'novo' | null>(null);
  const q = useQuery({ queryKey: ['pro', 'marcadores', 'config', inativos], queryFn: () => api<Marcador[]>(`/api/exames/marcadores?incluirInativos=${inativos}`) });
  const termo = busca.trim().toLowerCase();
  const lista = (q.data ?? []).filter((m) => !termo || `${m.nome} ${m.categoria ?? ''}`.toLowerCase().includes(termo));
  const grupos = new Map<string, Marcador[]>();
  for (const m of lista) grupos.set(m.categoria ?? 'Outros', [...(grupos.get(m.categoria ?? 'Outros') ?? []), m]);
  const categorias = [...new Set((q.data ?? []).map((m) => m.categoria).filter(Boolean) as string[])].sort();

  return (
    <>
      <div className="mb-3 flex items-center gap-2">
        <div className="min-w-0 flex-1">
          <Busca value={busca} onChange={setBusca} placeholder="Buscar marcador" />
        </div>
        <Button icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
          Novo
        </Button>
      </div>
      <div className="mb-3 flex items-center justify-between gap-3">
        <p className="text-sm text-muted">
          {lista.length} {lista.length === 1 ? 'marcador' : 'marcadores'}
        </p>
        <Inativos value={inativos} onChange={setInativos} rotulo="Mostrar inativos" />
      </div>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Empty icon={<FlaskConical className="h-5 w-5" />} title={q.data?.length ? 'Nada encontrado' : 'Nenhum marcador'}>
          Cadastre marcadores (glicemia, ferritina, vitamina D...) com a unidade padrão para lançar exames mais rápido.
        </Empty>
      ) : (
        <div className="flex flex-col gap-4">
          {[...grupos.entries()].map(([cat, itens]) => (
            <div key={cat}>
              <p className="mb-1.5 text-[12px] font-bold uppercase tracking-wider text-faint">{cat}</p>
              <Card className="divide-y divide-line/60">
                {itens.map((m) => (
                  <Linha key={m.id} titulo={m.nome} sub={m.unidadePadrao ?? undefined} extra={!m.ativo && <Pill>Inativo</Pill>} onClick={() => setAberto(m)} />
                ))}
              </Card>
            </div>
          ))}
        </div>
      )}
      <MarcadorSheet marcador={aberto === 'novo' ? null : aberto} open={aberto !== null} categorias={categorias} onClose={() => setAberto(null)} onSalvo={() => qc.invalidateQueries({ queryKey: ['pro', 'marcadores'] })} />
    </>
  );
}

function MarcadorSheet({ marcador, open, categorias, onClose, onSalvo }: { marcador: Marcador | null; open: boolean; categorias: string[]; onClose: () => void; onSalvo: () => void }) {
  const [nome, setNome] = useState('');
  const [categoria, setCategoria] = useState('');
  const [unidade, setUnidade] = useState('');
  useEffect(() => {
    if (!open) return;
    setNome(marcador?.nome ?? '');
    setCategoria(marcador?.categoria ?? '');
    setUnidade(marcador?.unidadePadrao ?? '');
  }, [open, marcador]);
  const salvar = useMutation({
    mutationFn: () =>
      api(marcador ? `/api/exames/marcadores/${marcador.id}` : '/api/exames/marcadores', {
        method: marcador ? 'PUT' : 'POST',
        json: { nome: nome.trim(), categoria: categoria.trim() || null, unidadePadrao: unidade.trim() || null },
      }),
    onSuccess: () => {
      toast(marcador ? 'Marcador salvo.' : 'Marcador cadastrado.');
      onSalvo();
      onClose();
    },
    onError: erroToast,
  });
  const ativar = useMutation({
    mutationFn: () => (marcador!.ativo ? api(`/api/exames/marcadores/${marcador!.id}`, { method: 'DELETE' }) : api(`/api/exames/marcadores/${marcador!.id}/reativar`, { method: 'POST' })),
    onSuccess: () => {
      toast(marcador!.ativo ? 'Marcador desativado. Exames antigos continuam com ele.' : 'Marcador reativado.');
      onSalvo();
      onClose();
    },
    onError: erroToast,
  });
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title={marcador ? 'Marcador de exame' : 'Novo marcador'}
      footer={
        <Button size="lg" className="w-full" disabled={!nome.trim()} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {marcador ? 'Salvar' : 'Cadastrar'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={nome} maxLength={160} placeholder="Ex.: Ferritina" onChange={(e) => setNome(e.target.value)} />
        </Field>
        <Field label="Categoria">
          <input className={inputCls} value={categoria} maxLength={80} list="categorias-marcador" placeholder="Ex.: Hematologia, Vitaminas, Hormônios" onChange={(e) => setCategoria(e.target.value)} />
          <datalist id="categorias-marcador">
            {categorias.map((c) => (
              <option key={c} value={c} />
            ))}
          </datalist>
        </Field>
        <Field label="Unidade padrão">
          <input className={inputCls} value={unidade} maxLength={40} placeholder="Ex.: ng/mL" onChange={(e) => setUnidade(e.target.value)} />
        </Field>
        {marcador && (
          <Button variant="ghost" className={marcador.ativo ? 'text-muted' : 'text-accent'} loading={ativar.isPending} onClick={() => ativar.mutate()}>
            {marcador.ativo ? 'Desativar marcador' : 'Reativar marcador'}
          </Button>
        )}
      </div>
    </Sheet>
  );
}
