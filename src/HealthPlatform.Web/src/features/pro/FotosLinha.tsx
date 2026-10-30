import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Activity, Camera, CalendarDays, ClipboardList, Dumbbell, Eraser, FileText, FlaskConical, HeartPulse, ImagePlus, MessageCircle, NotebookPen, PenLine, Salad, Scale, Stethoscope, Target } from 'lucide-react';
import { ReactNode, useEffect, useRef, useState } from 'react';
import { Button, Card, Field, inputCls, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { Foto, useImagem } from '@/lib/imagemProtegida';
import { Chips } from './CuidadosPro';

// ---------------------------------------------------------------- Fotos de evolucao

interface RegistroDiario {
  id: string;
  dataHoraUtc: string;
  tipo: string;
  descricao: string | null;
  imagemUrl: string | null;
}

const POSES = [
  { value: 'Frente', label: 'Frente' },
  { value: 'Costas', label: 'Costas' },
  { value: 'LadoDireito', label: 'Lado direito' },
  { value: 'LadoEsquerdo', label: 'Lado esquerdo' },
] as const;
type Pose = (typeof POSES)[number]['value'];

// O tipo do registro guarda a pose: "FotoEvolucao:Frente" ou "FotoEvolucaoAnotada:Costas", igual a versao classica.
const poseDe = (tipo: string): Pose => (POSES.find((p) => tipo.endsWith(`:${p.value}`))?.value ?? 'Frente');
const rotuloPose = (tipo: string) => POSES.find((p) => p.value === poseDe(tipo))?.label ?? 'Foto';
const anotada = (tipo: string) => tipo.startsWith('FotoEvolucaoAnotada');
const dia = (iso: string) => new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });

const useFotos = (id: string) =>
  useQuery({
    queryKey: ['pro', 'fotos', id],
    queryFn: () => api<RegistroDiario[]>(`/api/pacientes/${id}/diario`),
    select: (d) => d.filter((x) => x.tipo.startsWith('FotoEvolucao') && x.imagemUrl).sort((a, b) => b.dataHoraUtc.localeCompare(a.dataHoraUtc)),
  });

async function registrarFoto(id: string, file: File | Blob, pose: Pose, nota: string, ehAnotada: boolean) {
  const fd = new FormData();
  fd.append('file', file, file instanceof File ? file.name : `aesyn-evolucao-anotada-${Date.now()}.jpg`);
  const up = await api<{ url: string }>(`/api/pacientes/${id}/fotos-evolucao/upload`, { method: 'POST', body: fd });
  await api(`/api/pacientes/${id}/diario`, {
    method: 'POST',
    json: { dataHoraUtc: new Date().toISOString(), tipo: `${ehAnotada ? 'FotoEvolucaoAnotada' : 'FotoEvolucao'}:${pose}`, descricao: nota.trim() || null, valorNumerico: null, unidade: null, escala: null, imagemUrl: up.url },
  });
}

/** Fotos de evolucao corporal: enviar, comparar lado a lado e marcar uma copia. */
export function Fotos({ id }: { id: string }) {
  const q = useFotos(id);
  const [nova, setNova] = useState(false);
  const [comparar, setComparar] = useState<number | null>(null);
  const [marcar, setMarcar] = useState<RegistroDiario | null>(null);
  const fotos = q.data ?? [];

  return (
    <>
      <div className="mb-3 flex items-center justify-between gap-2">
        <p className="text-sm text-muted">
          {fotos.length} {fotos.length === 1 ? 'foto' : 'fotos'}
        </p>
        <div className="flex gap-2">
          {fotos.length >= 2 && (
            <Button size="sm" variant="soft" onClick={() => setComparar(0)}>
              Comparar
            </Button>
          )}
          <Button size="sm" icon={<ImagePlus className="h-4 w-4" />} onClick={() => setNova(true)}>
            Nova foto
          </Button>
        </div>
      </div>
      {q.isLoading ? (
        <Skeleton className="h-48" />
      ) : !fotos.length ? (
        <Card className="flex items-start gap-3 p-4 text-sm text-muted">
          <Camera className="h-5 w-5 shrink-0 text-faint" />
          <span>Nenhuma foto de evolução. Adicione a primeira para começar a comparar ao longo do acompanhamento.</span>
        </Card>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {fotos.map((f, i) => (
            <Card key={f.id} className="overflow-hidden">
              <div className="relative">
                <Foto path={f.imagemUrl!} alt={`Foto de evolução, ${rotuloPose(f.tipo)}, ${dia(f.dataHoraUtc)}`} className="aspect-[3/4] w-full" />
                {anotada(f.tipo) && <span className="absolute left-2 top-2 rounded-full bg-bg/80 px-2 py-0.5 text-[11px] font-semibold text-accent">Anotada</span>}
              </div>
              <div className="p-2.5">
                <p className="text-[14px] font-semibold">{rotuloPose(f.tipo)}</p>
                <p className="text-[12px] text-muted">{dia(f.dataHoraUtc)}</p>
                {f.descricao && <p className="mt-1 line-clamp-2 text-[12px] text-muted [overflow-wrap:anywhere]">{f.descricao}</p>}
                <div className="mt-2 flex gap-3 text-[13px] font-semibold text-accent">
                  {fotos.length >= 2 && <button onClick={() => setComparar(i)}>Comparar</button>}
                  <button onClick={() => setMarcar(f)}>Marcar</button>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
      <p className="mt-3 text-[12px] text-faint">Uso profissional e consentido. Evite rosto ou informações que identifiquem o paciente no enquadramento.</p>
      <NovaFotoSheet id={id} open={nova} onClose={() => setNova(false)} />
      <CompararSheet fotos={fotos} inicio={comparar} onClose={() => setComparar(null)} />
      <MarcarSheet id={id} foto={marcar} onClose={() => setMarcar(null)} />
    </>
  );
}

function NovaFotoSheet({ id, open, onClose }: { id: string; open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const [pose, setPose] = useState<Pose>('Frente');
  const [nota, setNota] = useState('');
  const [preview, setPreview] = useState<string | null>(null);
  useEffect(() => {
    if (open) {
      setFile(null);
      setPose('Frente');
      setNota('');
    }
  }, [open]);
  useEffect(() => {
    if (!file) return setPreview(null);
    const u = URL.createObjectURL(file);
    setPreview(u);
    return () => URL.revokeObjectURL(u);
  }, [file]);

  const salvar = useMutation({
    mutationFn: () => registrarFoto(id, file!, pose, nota, false),
    onSuccess: () => {
      toast('Foto de evolução salva.');
      qc.invalidateQueries({ queryKey: ['pro', 'fotos', id] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  const escolher = (f: File | undefined) => {
    if (!f) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(f.type)) return toast('Use JPEG, PNG ou WebP.', 'bad');
    if (f.size > 8 * 1024 * 1024) return toast('A imagem deve ter no máximo 8 MB.', 'bad');
    setFile(f);
  };

  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Nova foto de evolução"
      footer={
        <Button size="lg" className="w-full" disabled={!file} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar foto
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <label className="flex cursor-pointer flex-col items-center justify-center gap-2 overflow-hidden rounded-2xl border border-dashed border-line bg-raised/40 p-4 text-center text-sm text-muted">
          {preview ? <img src={preview} alt="Pré-visualização" className="max-h-64 rounded-xl object-contain" /> : <ImagePlus className="h-7 w-7 text-accent" />}
          <span>{file ? file.name : 'Escolher imagem (JPEG, PNG ou WebP, até 8 MB)'}</span>
          <input type="file" accept="image/jpeg,image/png,image/webp" className="sr-only" aria-label="Imagem" onChange={(e) => escolher(e.target.files?.[0])} />
        </label>
        <Field label="Posição" group>
          <Chips<Pose> value={pose} onChange={setPose} options={[...POSES]} />
        </Field>
        <Field label="Observação (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={nota} maxLength={1000} placeholder="Ex.: foco em postura, simetria..." onChange={(e) => setNota(e.target.value)} />
        </Field>
        <p className="text-[13px] text-muted">Mesmo local, distância, iluminação e posição deixam a comparação mais útil.</p>
      </div>
    </Sheet>
  );
}

function CompararSheet({ fotos, inicio, onClose }: { fotos: RegistroDiario[]; inicio: number | null; onClose: () => void }) {
  // A lista vem da mais nova para a mais antiga: "antes" e a mais antiga das duas.
  const [antes, setAntes] = useState(1);
  const [depois, setDepois] = useState(0);
  useEffect(() => {
    if (inicio == null || fotos.length < 2) return;
    const d = Math.min(inicio, fotos.length - 2);
    setDepois(d);
    setAntes(Math.min(d + 1, fotos.length - 1));
  }, [inicio, fotos.length]);
  const opcao = (f: RegistroDiario, i: number) => (
    <option key={f.id} value={i}>
      {dia(f.dataHoraUtc)} · {rotuloPose(f.tipo)}
      {anotada(f.tipo) ? ' · anotada' : ''}
    </option>
  );
  const lado = (f: RegistroDiario | undefined, rotulo: string) =>
    f && (
      <figure className="min-w-0">
        <Foto path={f.imagemUrl!} alt={`${rotulo}: ${rotuloPose(f.tipo)}, ${dia(f.dataHoraUtc)}`} className="aspect-[3/4] w-full rounded-xl" />
        <figcaption className="mt-1 text-center text-[12px] text-muted">
          {rotulo} · {dia(f.dataHoraUtc)}
        </figcaption>
      </figure>
    );
  return (
    <Sheet open={inicio != null} onClose={onClose} title="Comparar fotos">
      <div className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-3">
          <Field label="Antes">
            <select className={inputCls} value={antes} onChange={(e) => setAntes(Number(e.target.value))}>
              {fotos.map(opcao)}
            </select>
          </Field>
          <Field label="Depois">
            <select className={inputCls} value={depois} onChange={(e) => setDepois(Number(e.target.value))}>
              {fotos.map(opcao)}
            </select>
          </Field>
        </div>
        <div className="grid grid-cols-2 gap-3">
          {lado(fotos[antes], 'Antes')}
          {lado(fotos[depois], 'Depois')}
        </div>
        <p className="text-[13px] text-muted">A foto é apoio visual. Interprete junto de medidas, composição corporal e desempenho.</p>
      </div>
    </Sheet>
  );
}

/** Desenho livre sobre uma copia da foto. A original continua intacta. */
function MarcarSheet({ id, foto, onClose }: { id: string; foto: RegistroDiario | null; onClose: () => void }) {
  const qc = useQueryClient();
  const { src } = useImagem(foto?.imagemUrl);
  const img = useRef<HTMLImageElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const desenhando = useRef(false);
  const [espessura, setEspessura] = useState(6);
  const [nota, setNota] = useState('');
  const [marcou, setMarcou] = useState(false);
  useEffect(() => {
    setNota('');
    setMarcou(false);
  }, [foto]);

  // O canvas tem o tamanho natural da imagem, entao o traco sai na resolucao original.
  const preparar = () => {
    const i = img.current;
    const c = canvas.current;
    if (!i || !c) return;
    c.width = i.naturalWidth || 1200;
    c.height = i.naturalHeight || 1600;
  };
  const ponto = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const c = canvas.current!;
    const r = c.getBoundingClientRect();
    return { x: ((e.clientX - r.left) / r.width) * c.width, y: ((e.clientY - r.top) / r.height) * c.height, k: c.width / r.width };
  };
  const inicio = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const ctx = canvas.current?.getContext('2d');
    if (!ctx) return;
    canvas.current!.setPointerCapture(e.pointerId);
    desenhando.current = true;
    const p = ponto(e);
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.strokeStyle = '#62e3bd';
    ctx.lineWidth = espessura * p.k;
    ctx.beginPath();
    ctx.moveTo(p.x, p.y);
  };
  const mover = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (!desenhando.current) return;
    const ctx = canvas.current!.getContext('2d')!;
    const p = ponto(e);
    ctx.lineTo(p.x, p.y);
    ctx.stroke();
    setMarcou(true);
  };
  const fim = () => (desenhando.current = false);
  const limpar = () => {
    const c = canvas.current;
    c?.getContext('2d')?.clearRect(0, 0, c.width, c.height);
    setMarcou(false);
  };

  const salvar = useMutation({
    mutationFn: async () => {
      const i = img.current!;
      const c = canvas.current!;
      const out = document.createElement('canvas');
      out.width = c.width;
      out.height = c.height;
      const o = out.getContext('2d')!;
      o.drawImage(i, 0, 0, out.width, out.height);
      o.drawImage(c, 0, 0);
      const blob = await new Promise<Blob | null>((r) => out.toBlob(r, 'image/jpeg', 0.9));
      if (!blob) throw new Error('Não foi possível gerar a imagem anotada.');
      await registrarFoto(id, blob, poseDe(foto!.tipo), nota, true);
    },
    onSuccess: () => {
      toast('Cópia anotada salva. A foto original foi mantida.');
      qc.invalidateQueries({ queryKey: ['pro', 'fotos', id] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  return (
    <Sheet
      open={!!foto}
      onClose={onClose}
      title="Marcar área de foco"
      footer={
        <Button size="lg" className="w-full" disabled={!marcou} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar cópia anotada
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <div className="flex items-center gap-3">
          <label className="flex flex-1 items-center gap-2 text-[13px] text-muted">
            Espessura
            <input type="range" min={2} max={18} value={espessura} onChange={(e) => setEspessura(Number(e.target.value))} className="flex-1 accent-[rgb(var(--accent))]" />
          </label>
          <Button size="sm" variant="soft" icon={<Eraser className="h-4 w-4" />} onClick={limpar}>
            Limpar
          </Button>
        </div>
        {src ? (
          <div className="relative mx-auto w-full max-w-sm">
            <img ref={img} src={src} alt="Foto para marcar" onLoad={preparar} className="block w-full select-none rounded-xl" draggable={false} />
            <canvas
              ref={canvas}
              aria-label="Área de desenho"
              className="absolute inset-0 h-full w-full touch-none rounded-xl"
              onPointerDown={inicio}
              onPointerMove={mover}
              onPointerUp={fim}
              onPointerCancel={fim}
            />
          </div>
        ) : (
          <Skeleton className="aspect-[3/4] w-full" />
        )}
        <Field label="Observação">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={nota} maxLength={1000} placeholder="Explique o foco marcado na imagem." onChange={(e) => setNota(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Linha do tempo

interface ItemTimeline {
  tipo: string;
  id: string;
  dataUtc: string;
  titulo: string;
  resumo: string | null;
  categoria: string;
  prioridade: number;
}

type Filtro = 'tudo' | 'Clinico' | 'Corpo' | 'Exames' | 'Treino' | 'Nutricao' | 'Rotina' | 'Comunicacao' | 'Metas';

const FILTROS: { value: Filtro; label: string }[] = [
  { value: 'tudo', label: 'Tudo' },
  { value: 'Clinico', label: 'Clínico' },
  { value: 'Corpo', label: 'Corpo' },
  { value: 'Exames', label: 'Exames' },
  { value: 'Treino', label: 'Treino' },
  { value: 'Nutricao', label: 'Nutrição' },
  { value: 'Rotina', label: 'Rotina' },
  { value: 'Metas', label: 'Metas' },
  { value: 'Comunicacao', label: 'Mensagens' },
];

const ICONES: Record<string, ReactNode> = {
  consulta: <Stethoscope className="h-4 w-4" />,
  avaliacao: <Scale className="h-4 w-4" />,
  anamnese: <ClipboardList className="h-4 w-4" />,
  evolucao_clinica: <NotebookPen className="h-4 w-4" />,
  exame: <FlaskConical className="h-4 w-4" />,
  relatorio: <FileText className="h-4 w-4" />,
  plano_alimentar: <Salad className="h-4 w-4" />,
  meta: <Target className="h-4 w-4" />,
  registro_diario: <PenLine className="h-4 w-4" />,
  plano_treino: <Dumbbell className="h-4 w-4" />,
  treino_executado: <Dumbbell className="h-4 w-4" />,
  checkin: <HeartPulse className="h-4 w-4" />,
  chat: <MessageCircle className="h-4 w-4" />,
};

// A API manda alguns titulos sem acento e os tipos do diario em CamelCase.
const DIARIO: Record<string, string> = {
  FechamentoDia: 'Fechamento do dia',
  Hidratacao: 'Hidratação',
  Alimentacao: 'Alimentação',
  Observacao: 'Observação',
  PressaoSistolica: 'Pressão arterial',
  PressaoDiastolica: 'Pressão arterial',
  FrequenciaCardiaca: 'Frequência cardíaca',
  Saturacao: 'Saturação',
  Sintoma: 'Sintomas',
  Sono: 'Sono',
  Dor: 'Dor',
  Energia: 'Energia',
  Humor: 'Humor',
  Agua: 'Água',
};

function titulo(x: ItemTimeline) {
  if (x.tipo === 'registro_diario') {
    const t = x.titulo.replace(/^Diario - /, '');
    if (t.startsWith('FotoEvolucao')) return `Foto de evolução (${rotuloPose(t).toLowerCase()})`;
    return `Diário · ${DIARIO[t] ?? t.replace(/([a-z])([A-Z])/g, '$1 $2')}`;
  }
  if (x.tipo === 'consulta') return x.titulo.replace(/^Consulta - /, 'Consulta · ');
  if (x.tipo === 'evolucao_clinica') return 'Evolução SOAP';
  if (x.tipo === 'avaliacao') return 'Avaliação corporal';
  if (x.tipo === 'relatorio') return x.titulo.replace(/^Relatorio clinico/, 'Relatório clínico');
  return x.titulo;
}

// Mensagens do chat podem trazer a referencia anexada como marcador "[[AESYNCTX|Tipo|id|tituloBase64]]".
const REF: Record<string, string> = { Arquivo: 'arquivo', Treino: 'treino', Exercicio: 'exercício', Refeicao: 'refeição', Exame: 'exame' };
function resumo(x: ItemTimeline) {
  if (!x.resumo || x.tipo !== 'chat') return x.resumo;
  return x.resumo
    .replace(/\[\[AESYNCTX\|(\w+)\|[^|]*\|([^\]]*)(\]\]|…?$)/g, (_, tipo: string, b64: string) => {
      let t = '';
      try {
        t = new TextDecoder().decode(Uint8Array.from(atob(b64.replace(/…$/, '')), (c) => c.charCodeAt(0)));
      } catch {
        /* titulo cortado no resumo */
      }
      return `📎 ${t || REF[tipo] || 'anexo'}`;
    })
    .trim();
}

// Registros automaticos de wearable e a pressao duplicada (sistolica + diastolica) poluem a leitura.
const visivel = (x: ItemTimeline) => !(x.tipo === 'registro_diario' && /MonitoramentoPassivo|PressaoDiastolica/.test(x.titulo));

const diaLongo = (iso: string) => new Date(iso).toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' });
const horaDe = (iso: string) => new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });

/** Tudo o que aconteceu com o paciente, em ordem, agrupado por dia. */
export function LinhaDoTempo({ id }: { id: string }) {
  const [filtro, setFiltro] = useState<Filtro>('tudo');
  const [limite, setLimite] = useState(120);
  const q = useQuery({
    queryKey: ['pro', 'timeline', id, limite],
    queryFn: () => api<ItemTimeline[]>(`/api/pacientes/${id}/timeline?limite=${limite}`),
    placeholderData: (p) => p,
  });
  const todos = (q.data ?? []).filter(visivel);
  const itens = filtro === 'tudo' ? todos : todos.filter((x) => x.categoria === filtro);
  const categorias = new Set(todos.map((x) => x.categoria));

  return (
    <>
      <div className="-mx-gutter mb-3 overflow-x-auto px-gutter pb-1">
        <Chips<Filtro> value={filtro} onChange={setFiltro} options={FILTROS.filter((f) => f.value === 'tudo' || categorias.has(f.value))} />
      </div>
      {q.isLoading ? (
        <Skeleton className="h-64" />
      ) : !itens.length ? (
        <Card className="flex items-start gap-3 p-4 text-sm text-muted">
          <Activity className="h-5 w-5 shrink-0 text-faint" />
          <span>Nada registrado {filtro === 'tudo' ? 'ainda' : 'nessa categoria'}.</span>
        </Card>
      ) : (
        <ol className="relative ml-2 border-l border-line/70 pl-5">
          {itens.map((x, i) => {
            const novoDia = i === 0 || new Date(itens[i - 1].dataUtc).toDateString() !== new Date(x.dataUtc).toDateString();
            return (
              <li key={`${x.tipo}-${x.id}`} className="relative pb-3">
                {novoDia && <p className={clsx('mb-2 text-[12px] font-semibold uppercase tracking-wide text-faint', i > 0 && 'pt-2')}>{diaLongo(x.dataUtc)}</p>}
                <span className={clsx('absolute -left-[31px] grid h-5 w-5 place-items-center rounded-full border border-line bg-bg', x.prioridade > 0 ? 'text-warn' : 'text-accent', novoDia ? (i > 0 ? 'top-[37px]' : 'top-[29px]') : 'top-[3px]')}>
                  {x.titulo.includes('FotoEvolucao') ? <Camera className="h-3 w-3" /> : ICONES[x.tipo] ? <span className="scale-75">{ICONES[x.tipo]}</span> : <CalendarDays className="h-3 w-3" />}
                </span>
                <div className="min-w-0 [overflow-wrap:anywhere]">
                  <p className="text-[14px] font-semibold">
                    {titulo(x)} <span className="font-normal text-faint">· {horaDe(x.dataUtc)}</span>
                  </p>
                  {x.resumo && <p className="line-clamp-2 text-[13px] text-muted">{resumo(x)}</p>}
                </div>
              </li>
            );
          })}
        </ol>
      )}
      {(q.data?.length ?? 0) >= limite && limite < 250 && (
        <Button variant="soft" className="mt-2 w-full" loading={q.isFetching} onClick={() => setLimite(250)}>
          Ver mais antigos
        </Button>
      )}
    </>
  );
}
