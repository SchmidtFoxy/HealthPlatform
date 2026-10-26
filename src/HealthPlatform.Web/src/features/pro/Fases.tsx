import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import clsx from "clsx";
import {
  ArrowDown,
  ArrowUp,
  CheckCircle2,
  Circle,
  Flag,
  Pencil,
  Plus,
} from "lucide-react";
import { useEffect, useState } from "react";
import {
  Button,
  Card,
  Field,
  inputCls,
  Pill,
  Sheet,
  Skeleton,
  toast,
} from "@/components/ui";
import { api } from "@/lib/api";
import { dataCurta, hojeIso } from "@/lib/format";

// Periodizacao: fases de treino (adaptacao, forca, deload...) e nutricionais
// (cutting, manutencao, refeed...) com metas objetivas e revisao profissional.

export type Dominio = "treino" | "nutricao";

interface Fase {
  id: string;
  nome: string;
  tipo: string;
  objetivo: string | null;
  dataInicio: string;
  dataFim: string | null;
  ordem: number;
  status: "Planejada" | "EmAndamento" | "Concluida" | "Cancelada";
  observacoes: string | null;
  metaPesoKg: number | null;
  metaAdesaoPercentual: number | null;
  duracaoMinimaDias: number | null;
  criterioTransicao: string | null;
  planoTreinoId?: string | null;
  planoAlimentarId?: string | null;
  planoNome: string | null;
  planoVersao: number | null;
}

interface StatusFase {
  faseId: string;
  diasDecorridos: number;
  checkIns: number;
  criteriosObjetivosConfigurados: number;
  criteriosObjetivosAtendidos: number;
  objetivosProntosParaRevisao: boolean;
  criterios: {
    codigo: string;
    rotulo: string;
    configurado: boolean;
    atendido: boolean | null;
    detalhe: string | null;
  }[];
}

const CFG = {
  treino: {
    rota: "fases-treino",
    planoCampo: "planoTreinoId" as const,
    tipos: [
      "Adaptação",
      "Hipertrofia",
      "Força",
      "Deload",
      "Performance",
      "Condicionamento",
      "Personalizada",
    ],
    vazio:
      "Organize o ciclo em etapas como adaptação, hipertrofia, força, deload ou performance.",
    plano: "Plano de treino",
  },
  nutricao: {
    rota: "fases-nutricionais",
    planoCampo: "planoAlimentarId" as const,
    tipos: [
      "Adaptação",
      "Cutting",
      "Manutenção",
      "Refeed",
      "Ganho",
      "Performance",
      "Personalizada",
    ],
    vazio:
      "Organize o ciclo em etapas como adaptação, cutting, manutenção, refeed ou ganho.",
    plano: "Plano alimentar",
  },
};

const STATUS: Record<
  Fase["status"],
  { l: string; tone: "accent" | "ok" | "neutral" | "bad" }
> = {
  EmAndamento: { l: "Em andamento", tone: "accent" },
  Planejada: { l: "Planejada", tone: "neutral" },
  Concluida: { l: "Concluída", tone: "ok" },
  Cancelada: { l: "Cancelada", tone: "bad" },
};

const detalhe = (d: string | null) =>
  d?.replace(/^1\/1 dias$/, "1/1 dia") ?? null;

const ROTULO_CRITERIO: Record<string, string> = {
  duracao_minima: "Duração mínima",
  adesao_minima: "Adesão mínima",
  meta_peso: "Meta de peso",
};

const chave = (id: string, d: Dominio) => ["pro", "fases", d, id];

/** Lista de fases do paciente com metas, prontidao para transicao, ordem e revisao. */
export function FasesSecao({
  id,
  dominio,
  planos,
}: {
  id: string;
  dominio: Dominio;
  planos: { id: string; nome: string; versao: number }[];
}) {
  const cfg = CFG[dominio];
  const qc = useQueryClient();
  const fases = useQuery({
    queryKey: chave(id, dominio),
    queryFn: () => api<Fase[]>(`/api/pacientes/${id}/${cfg.rota}`),
  });
  const status = useQuery({
    queryKey: ["pro", "status-fases", id],
    queryFn: () =>
      api<{ nutricao: StatusFase[]; treino: StatusFase[] }>(
        `/api/pacientes/${id}/status-transicao-fases`,
      ),
  });
  const [editando, setEditando] = useState<Fase | "nova" | null>(null);
  const [revisando, setRevisando] = useState<Fase | null>(null);
  const lista = [...(fases.data ?? [])].sort((a, b) => a.ordem - b.ordem);
  const porFase = new Map(
    (status.data?.[dominio] ?? []).map((s) => [s.faseId, s]),
  );

  const mover = useMutation({
    mutationFn: ({ idx, delta }: { idx: number; delta: number }) => {
      const l = [...lista];
      [l[idx], l[idx + delta]] = [l[idx + delta], l[idx]];
      return api(`/api/pacientes/${id}/${cfg.rota}/reordenar`, {
        method: "POST",
        json: { fases: l.map((f, i) => ({ faseId: f.id, ordem: i + 1 })) },
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: chave(id, dominio) }),
    onError: (e) => toast(e.message, "bad"),
  });

  return (
    <section className="mt-2">
      <div className="mb-3 flex items-center justify-between gap-2">
        <div className="min-w-0">
          <h2 className="font-display text-[17px] font-semibold tracking-tight">
            Fases do ciclo
          </h2>
          <p className="text-sm text-muted">
            {lista.length
              ? `${lista.length} ${lista.length === 1 ? "fase" : "fases"} · você decide quando avançar`
              : "Periodização além das versões do plano"}
          </p>
        </div>
        <Button
          size="sm"
          variant="soft"
          className="shrink-0 whitespace-nowrap"
          icon={<Plus className="h-4 w-4" />}
          onClick={() => setEditando("nova")}
        >
          Nova fase
        </Button>
      </div>
      {fases.isLoading ? (
        <Skeleton className="h-32" />
      ) : !lista.length ? (
        <Card className="flex items-start gap-3 p-4 text-sm text-muted">
          <Flag className="h-5 w-5 shrink-0 text-faint" />
          <span>{cfg.vazio}</span>
        </Card>
      ) : (
        <div className="flex flex-col gap-2">
          {lista.map((f, i) => {
            const st = porFase.get(f.id);
            const criterios = st?.criterios.filter((c) => c.configurado) ?? [];
            return (
              <Card
                key={f.id}
                className={clsx(
                  "p-4",
                  f.status === "EmAndamento" && "border-accent/40",
                )}
              >
                <div className="flex items-start gap-3">
                  <div className="flex flex-col items-center gap-0.5 pt-0.5">
                    <button
                      aria-label="Subir fase"
                      disabled={i === 0 || mover.isPending}
                      onClick={() => mover.mutate({ idx: i, delta: -1 })}
                      className="grid h-7 w-7 place-items-center rounded-full text-muted transition hover:bg-raised disabled:opacity-30"
                    >
                      <ArrowUp className="h-4 w-4" />
                    </button>
                    <span className="num text-sm font-semibold">{i + 1}</span>
                    <button
                      aria-label="Descer fase"
                      disabled={i === lista.length - 1 || mover.isPending}
                      onClick={() => mover.mutate({ idx: i, delta: 1 })}
                      className="grid h-7 w-7 place-items-center rounded-full text-muted transition hover:bg-raised disabled:opacity-30"
                    >
                      <ArrowDown className="h-4 w-4" />
                    </button>
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <p className="text-xs text-muted">
                          {f.tipo} · {dataCurta(f.dataInicio)}
                          {f.dataFim
                            ? ` a ${dataCurta(f.dataFim)}`
                            : ", sem fim definido"}
                        </p>
                        <p className="font-display text-[16px] font-semibold leading-snug">
                          {f.nome}
                        </p>
                      </div>
                      <Pill tone={STATUS[f.status]?.tone ?? "neutral"}>
                        {STATUS[f.status]?.l ?? f.status}
                      </Pill>
                    </div>
                    {f.objetivo && <p className="mt-1 text-sm">{f.objetivo}</p>}
                    {f.planoNome && (
                      <p className="mt-1 text-[13px] text-muted">
                        {cfg.plano}: {f.planoNome} · v{f.planoVersao ?? 1}
                      </p>
                    )}
                    {criterios.length > 0 && (
                      <div className="mt-2.5 flex flex-col gap-1 rounded-xl bg-raised/60 p-2.5">
                        {criterios.map((c) => (
                          <p
                            key={c.codigo}
                            className="flex items-center gap-2 text-[13px]"
                          >
                            {c.atendido ? (
                              <CheckCircle2 className="h-4 w-4 shrink-0 text-ok" />
                            ) : (
                              <Circle className="h-4 w-4 shrink-0 text-faint" />
                            )}
                            <span className="shrink-0 font-semibold">
                              {ROTULO_CRITERIO[c.codigo] ?? c.rotulo}
                            </span>
                            {c.detalhe && (
                              <span className="num min-w-0 truncate text-muted">
                                {detalhe(c.detalhe)}
                              </span>
                            )}
                          </p>
                        ))}
                      </div>
                    )}
                    {f.criterioTransicao && (
                      <p className="mt-2 text-[13px] text-muted">
                        Critério para avançar: {f.criterioTransicao}
                      </p>
                    )}
                    <div className="mt-3 flex gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        icon={<Pencil className="h-3.5 w-3.5" />}
                        onClick={() => setEditando(f)}
                      >
                        Editar
                      </Button>
                      {f.status === "EmAndamento" && (
                        <Button
                          size="sm"
                          variant="soft"
                          onClick={() => setRevisando(f)}
                        >
                          Revisar fase
                        </Button>
                      )}
                    </div>
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}
      <FaseSheet
        id={id}
        dominio={dominio}
        fase={editando}
        planos={planos}
        onClose={() => setEditando(null)}
      />
      <RevisarSheet
        id={id}
        dominio={dominio}
        fase={revisando}
        status={revisando ? porFase.get(revisando.id) : undefined}
        onClose={() => setRevisando(null)}
      />
    </section>
  );
}

type Form = {
  nome: string;
  tipo: string;
  objetivo: string;
  dataInicio: string;
  dataFim: string;
  plano: string;
  status: Fase["status"];
  metaPesoKg: string;
  metaAdesaoPercentual: string;
  duracaoMinimaDias: string;
  criterioTransicao: string;
  observacoes: string;
};

const s = (v: number | string | null | undefined) =>
  v == null ? "" : String(v);
const num = (v: string) =>
  v.trim() === "" ? null : Number(v.replace(",", "."));

function FaseSheet({
  id,
  dominio,
  fase,
  planos,
  onClose,
}: {
  id: string;
  dominio: Dominio;
  fase: Fase | "nova" | null;
  planos: { id: string; nome: string; versao: number }[];
  onClose: () => void;
}) {
  const cfg = CFG[dominio];
  const qc = useQueryClient();
  const editando = fase && fase !== "nova" ? fase : null;
  const inicial = (): Form => ({
    nome: editando?.nome ?? "",
    tipo: editando?.tipo ?? cfg.tipos[0],
    objetivo: editando?.objetivo ?? "",
    dataInicio: editando?.dataInicio ?? hojeIso(),
    dataFim: editando?.dataFim ?? "",
    plano: s(editando?.[cfg.planoCampo]),
    status: editando?.status ?? "Planejada",
    metaPesoKg: s(editando?.metaPesoKg),
    metaAdesaoPercentual: s(editando?.metaAdesaoPercentual),
    duracaoMinimaDias: s(editando?.duracaoMinimaDias),
    criterioTransicao: editando?.criterioTransicao ?? "",
    observacoes: editando?.observacoes ?? "",
  });
  const [f, setF] = useState<Form>(inicial);
  const [excluir, setExcluir] = useState(false);
  useEffect(() => {
    if (fase) {
      setF(inicial());
      setExcluir(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fase]);
  const set = (k: keyof Form) => (e: { target: { value: string } }) =>
    setF((x) => ({ ...x, [k]: e.target.value }));

  const pronto = () => {
    qc.invalidateQueries({ queryKey: chave(id, dominio) });
    qc.invalidateQueries({ queryKey: ["pro", "status-fases", id] });
    onClose();
  };
  const salvar = useMutation({
    mutationFn: () => {
      const corpo = {
        nome: f.nome.trim(),
        tipo: f.tipo,
        objetivo: f.objetivo.trim() || null,
        dataInicio: f.dataInicio,
        dataFim: f.dataFim || null,
        [cfg.planoCampo]: f.plano || null,
        metaPesoKg: num(f.metaPesoKg),
        metaAdesaoPercentual: num(f.metaAdesaoPercentual),
        duracaoMinimaDias: num(f.duracaoMinimaDias),
        criterioTransicao: f.criterioTransicao.trim() || null,
        observacoes: f.observacoes.trim() || null,
      };
      return editando
        ? api(`/api/${cfg.rota}/${editando.id}`, {
            method: "PUT",
            json: { ...corpo, status: f.status },
          })
        : api(`/api/pacientes/${id}/${cfg.rota}`, {
            method: "POST",
            json: corpo,
          });
    },
    onSuccess: () => {
      toast(editando ? "Fase atualizada." : "Fase criada.");
      pronto();
    },
    onError: (e) => toast(e.message, "bad"),
  });
  const remover = useMutation({
    mutationFn: () =>
      api(`/api/${cfg.rota}/${editando!.id}`, { method: "DELETE" }),
    onSuccess: () => {
      toast("Fase excluída.");
      pronto();
    },
    onError: (e) => toast(e.message, "bad"),
  });

  return (
    <Sheet
      open={!!fase}
      onClose={onClose}
      title={
        editando
          ? "Editar fase"
          : dominio === "treino"
            ? "Nova fase de treino"
            : "Nova fase nutricional"
      }
      footer={
        <div className="flex gap-2">
          {editando && (
            <Button
              variant="danger"
              size="lg"
              loading={remover.isPending}
              onClick={() => (excluir ? remover.mutate() : setExcluir(true))}
            >
              {excluir ? "Confirmar" : "Excluir"}
            </Button>
          )}
          <Button
            size="lg"
            className="flex-1"
            disabled={!f.nome.trim() || !f.dataInicio}
            loading={salvar.isPending}
            onClick={() => salvar.mutate()}
          >
            {editando ? "Salvar alterações" : "Criar fase"}
          </Button>
        </div>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome da fase">
          <input
            className={inputCls}
            value={f.nome}
            onChange={set("nome")}
            placeholder={
              dominio === "treino"
                ? "Ex.: Bloco de força"
                : "Ex.: Cutting moderado"
            }
          />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Tipo">
            <select className={inputCls} value={f.tipo} onChange={set("tipo")}>
              {[...new Set([...cfg.tipos, f.tipo])].map((t) => (
                <option key={t}>{t}</option>
              ))}
            </select>
          </Field>
          {editando ? (
            <Field label="Status">
              <select
                className={inputCls}
                value={f.status}
                onChange={set("status")}
              >
                {(Object.keys(STATUS) as Fase["status"][]).map((k) => (
                  <option key={k} value={k}>
                    {STATUS[k].l}
                  </option>
                ))}
              </select>
            </Field>
          ) : (
            <Field label={cfg.plano}>
              <select
                className={inputCls}
                value={f.plano}
                onChange={set("plano")}
              >
                <option value="">Sem vínculo</option>
                {planos.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nome} · v{p.versao}
                  </option>
                ))}
              </select>
            </Field>
          )}
        </div>
        {editando && (
          <Field label={cfg.plano}>
            <select
              className={inputCls}
              value={f.plano}
              onChange={set("plano")}
            >
              <option value="">Sem vínculo</option>
              {planos.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nome} · v{p.versao}
                </option>
              ))}
            </select>
          </Field>
        )}
        <div className="grid grid-cols-2 gap-3">
          <Field label="Início">
            <input
              type="date"
              className={inputCls}
              value={f.dataInicio}
              onChange={set("dataInicio")}
            />
          </Field>
          <Field label="Fim (opcional)">
            <input
              type="date"
              className={inputCls}
              value={f.dataFim}
              onChange={set("dataFim")}
            />
          </Field>
        </div>
        <Field label="Objetivo">
          <textarea
            rows={2}
            className={clsx(inputCls, "h-auto py-2.5")}
            value={f.objetivo}
            onChange={set("objetivo")}
            placeholder={
              dominio === "treino"
                ? "Ex.: priorizar força nos básicos mantendo volume moderado."
                : "Ex.: déficit leve preservando massa magra."
            }
          />
        </Field>
        <p className="-mb-2 text-[13px] font-semibold text-muted">
          Metas para revisar a fase
        </p>
        <div className="grid grid-cols-3 gap-2">
          <Field label="Peso (kg)">
            <input
              inputMode="decimal"
              className={inputCls}
              value={f.metaPesoKg}
              onChange={set("metaPesoKg")}
            />
          </Field>
          <Field label="Adesão (%)">
            <input
              type="number"
              min={0}
              max={100}
              className={inputCls}
              value={f.metaAdesaoPercentual}
              onChange={set("metaAdesaoPercentual")}
            />
          </Field>
          <Field label="Mín. dias">
            <input
              type="number"
              min={1}
              className={inputCls}
              value={f.duracaoMinimaDias}
              onChange={set("duracaoMinimaDias")}
            />
          </Field>
        </div>
        <Field
          label="Critério profissional para avançar"
          hint="A adesão e o peso vêm dos check-ins de evolução ligados à fase."
        >
          <textarea
            rows={2}
            className={clsx(inputCls, "h-auto py-2.5")}
            value={f.criterioTransicao}
            onChange={set("criterioTransicao")}
            placeholder="Ex.: completar a semana-alvo sem queda de técnica."
          />
        </Field>
        <Field label="Observações">
          <textarea
            rows={2}
            className={clsx(inputCls, "h-auto py-2.5")}
            value={f.observacoes}
            onChange={set("observacoes")}
          />
        </Field>
      </div>
    </Sheet>
  );
}

const DECISOES = [
  { v: "Manter", l: "Manter em andamento" },
  { v: "Concluir", l: "Concluir esta fase" },
  { v: "Avancar", l: "Concluir e iniciar a próxima" },
];

function RevisarSheet({
  id,
  dominio,
  fase,
  status,
  onClose,
}: {
  id: string;
  dominio: Dominio;
  fase: Fase | null;
  status?: StatusFase;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const [decisao, setDecisao] = useState("Manter");
  const [just, setJust] = useState("");
  const [confirmar, setConfirmar] = useState(false);
  useEffect(() => {
    if (fase) {
      setDecisao("Manter");
      setJust("");
      setConfirmar(false);
    }
  }, [fase]);
  const pendentes =
    !!status &&
    status.criteriosObjetivosConfigurados > 0 &&
    !status.objetivosProntosParaRevisao;
  const m = useMutation({
    mutationFn: () =>
      api<{ message?: string }>(
        `/api/${CFG[dominio].rota}/${fase!.id}/revisar`,
        {
          method: "POST",
          json: {
            decisao,
            justificativa: just.trim(),
            confirmarMesmoSemCriterios: confirmar,
          },
        },
      ),
    onSuccess: (r) => {
      toast(r?.message ?? "Revisão registrada.");
      qc.invalidateQueries({ queryKey: chave(id, dominio) });
      qc.invalidateQueries({ queryKey: ["pro", "status-fases", id] });
      onClose();
    },
    onError: (e) => toast(e.message, "bad"),
  });
  return (
    <Sheet
      open={!!fase}
      onClose={onClose}
      title={fase ? `Revisar ${fase.nome}` : ""}
      footer={
        <Button
          size="lg"
          className="w-full"
          disabled={
            just.trim().length < 5 ||
            (pendentes && decisao !== "Manter" && !confirmar)
          }
          loading={m.isPending}
          onClick={() => m.mutate()}
        >
          Registrar revisão
        </Button>
      }
    >
      {status && (
        <div className="mb-4 rounded-xl bg-raised/60 p-3 text-sm">
          <p className="num">
            <span className="font-semibold">
              {status.criteriosObjetivosAtendidos}/
              {status.criteriosObjetivosConfigurados}
            </span>{" "}
            metas atendidas · {status.diasDecorridos}{" "}
            {status.diasDecorridos === 1 ? "dia" : "dias"} de fase ·{" "}
            {status.checkIns} {status.checkIns === 1 ? "check-in" : "check-ins"}
          </p>
          {status.criterios
            .filter((c) => c.configurado)
            .map((c) => (
              <p
                key={c.codigo}
                className="mt-1 flex items-center gap-2 text-[13px]"
              >
                {c.atendido ? (
                  <CheckCircle2 className="h-4 w-4 text-ok" />
                ) : (
                  <Circle className="h-4 w-4 text-faint" />
                )}
                {ROTULO_CRITERIO[c.codigo] ?? c.rotulo}
                {c.detalhe && (
                  <span className="num text-muted">{detalhe(c.detalhe)}</span>
                )}
              </p>
            ))}
        </div>
      )}
      <div
        className="flex flex-col gap-2"
        role="radiogroup"
        aria-label="Decisão"
      >
        {DECISOES.map((d) => (
          <button
            key={d.v}
            role="radio"
            aria-checked={decisao === d.v}
            onClick={() => setDecisao(d.v)}
            className={clsx(
              "rounded-xl border p-3 text-left text-[15px] font-semibold transition",
              decisao === d.v
                ? "border-accent bg-accent/10 text-accent"
                : "border-line hover:bg-raised/50",
            )}
          >
            {d.l}
          </button>
        ))}
      </div>
      <div className="mt-4">
        <Field label="Justificativa">
          <textarea
            rows={3}
            className={clsx(inputCls, "h-auto py-2.5")}
            value={just}
            onChange={(e) => setJust(e.target.value)}
            placeholder="Sua leitura dos resultados, da adesão e do contexto do paciente."
          />
        </Field>
      </div>
      {pendentes && decisao !== "Manter" && (
        <label className="mt-3 flex items-start gap-2.5 text-sm">
          <input
            type="checkbox"
            className="mt-0.5 h-4 w-4 accent-[rgb(var(--accent))]"
            checked={confirmar}
            onChange={(e) => setConfirmar(e.target.checked)}
          />
          <span>Confirmo a decisão mesmo com metas ainda pendentes.</span>
        </label>
      )}
      <p className="mt-3 text-xs text-faint">
        O sistema nunca avança sozinho. A decisão e a justificativa ficam no
        histórico
        {status?.objetivosProntosParaRevisao
          ? "; todas as metas desta fase foram atendidas"
          : ""}
        .
      </p>
    </Sheet>
  );
}
