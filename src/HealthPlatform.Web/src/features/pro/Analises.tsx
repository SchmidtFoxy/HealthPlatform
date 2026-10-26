import { useQuery } from "@tanstack/react-query";
import clsx from "clsx";
import { AlertTriangle, ChevronDown, Info, TrendingUp } from "lucide-react";
import { useState } from "react";
import { Card, Pill, SectionTitle, Skeleton } from "@/components/ui";
import { api } from "@/lib/api";
import { dataCurta, n0, n1 } from "@/lib/format";

// Leituras automaticas do paciente para o profissional: alertas (insights), dor por regiao,
// analise de progresso de carga e distribuicao de volume por grupo muscular.

// ---------------------------------------------------------------- Alertas

interface Insight {
  codigo: string;
  categoria: string;
  severidade: "Alta" | "Media" | "Baixa" | string;
  titulo: string;
  descricao: string;
  dataReferenciaUtc: string | null;
  valor: string | null;
  acaoSugerida: string | null;
}

const TOM_SEV: Record<string, "bad" | "warn" | "info"> = {
  Alta: "bad",
  Media: "warn",
  Baixa: "info",
};

export function AlertasPaciente({ pacienteId }: { pacienteId: string }) {
  const [aberto, setAberto] = useState<string | null>(null);
  const q = useQuery({
    queryKey: ["pro", "insights", pacienteId],
    queryFn: () =>
      api<{ total: number; insights: Insight[] }>(
        `/api/pacientes/${pacienteId}/insights`,
      ),
  });
  const lista = q.data?.insights ?? [];
  if (q.isLoading) return <Skeleton className="h-24" />;
  if (!lista.length) return null;
  return (
    <>
      <SectionTitle>Pontos de atenção</SectionTitle>
      <Card className="divide-y divide-line/60">
        {lista.map((x, i) => {
          const k = `${x.codigo}-${i}`;
          const open = aberto === k;
          return (
            <button
              key={k}
              onClick={() => setAberto(open ? null : k)}
              className="block w-full p-3.5 text-left transition hover:bg-raised/40"
            >
              <span className="flex items-start gap-3">
                <span
                  className={clsx(
                    "mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-xl",
                    x.severidade === "Alta"
                      ? "bg-bad/15 text-bad"
                      : x.severidade === "Media"
                        ? "bg-warn/15 text-warn"
                        : "bg-info/15 text-info",
                  )}
                >
                  {x.severidade === "Baixa" ? (
                    <Info className="h-4 w-4" />
                  ) : (
                    <AlertTriangle className="h-4 w-4" />
                  )}
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block font-semibold leading-snug">
                    {x.titulo}
                  </span>
                  <span className="block text-[13px] text-muted">
                    {[
                      x.categoria,
                      x.valor,
                      x.dataReferenciaUtc
                        ? dataCurta(x.dataReferenciaUtc)
                        : null,
                    ]
                      .filter(Boolean)
                      .join(" · ")}
                  </span>
                  {open && (
                    <>
                      <span className="mt-2 block text-[13px]">
                        {x.descricao}
                      </span>
                      {x.acaoSugerida && (
                        <span className="mt-1.5 block text-[13px] text-accent">
                          {x.acaoSugerida}
                        </span>
                      )}
                    </>
                  )}
                </span>
                <Pill tone={TOM_SEV[x.severidade] ?? "neutral"}>
                  {x.severidade === "Media" ? "Média" : x.severidade}
                </Pill>
              </span>
            </button>
          );
        })}
      </Card>
    </>
  );
}

// ---------------------------------------------------------------- Dor por regiao

export interface DorCorporalResumo {
  registros7: number;
  regioesAtivas: number;
  intensidadeMaxima7: number;
  impactoMaximoTreino7: number;
  nivelAtencao: string;
  mensagem: string;
  registrosRecentes: {
    id: string;
    data: string;
    regiao: string;
    lado: string | null;
    intensidade: number;
    impactoTreino: number;
    observacao: string | null;
  }[];
}

export function DorCorporalCard({
  dor,
}: {
  dor: DorCorporalResumo | null | undefined;
}) {
  if (!dor || !dor.registrosRecentes.length) return null;
  const tom =
    dor.nivelAtencao === "Alta"
      ? "bad"
      : dor.nivelAtencao === "Media"
        ? "warn"
        : "info";
  return (
    <Card className="mt-3 p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-semibold">Dor por região</p>
          <p className="text-[13px] text-muted">{dor.mensagem}</p>
        </div>
        <Pill tone={tom}>
          atenção{" "}
          {dor.nivelAtencao === "Media"
            ? "média"
            : dor.nivelAtencao.toLowerCase()}
        </Pill>
      </div>
      <div className="mt-3 flex flex-col gap-2">
        {dor.registrosRecentes.slice(0, 6).map((r) => (
          <div
            key={r.id}
            className="flex items-center gap-3 rounded-xl bg-raised/60 px-3 py-2"
          >
            <span className="min-w-0 flex-1">
              <span className="block text-[14px] font-semibold">
                {r.regiao}
                {r.lado ? ` · ${r.lado}` : ""}
              </span>
              <span className="block text-[12px] text-muted">
                {dataCurta(r.data)} · impacto no treino {r.impactoTreino}/10
                {r.observacao ? ` · ${r.observacao}` : ""}
              </span>
            </span>
            <span
              className={clsx(
                "num text-lg font-semibold",
                r.intensidade >= 7
                  ? "text-bad"
                  : r.intensidade >= 4
                    ? "text-warn"
                    : "text-muted",
              )}
            >
              {r.intensidade}
            </span>
          </div>
        ))}
      </div>
    </Card>
  );
}

// ---------------------------------------------------------------- Progresso de carga

interface ExercicioAnalise {
  exercicioId: string;
  exercicio: string;
  grupoMuscular: string | null;
  unidade: string | null;
  registros: number;
  status: string;
  revisaoSugerida: boolean;
  variacaoRecentePercentual: number | null;
  mediaRpeRecente: number | null;
  maiorCarga: number | null;
  diasSemRecorde: number | null;
  ultimaCarga: number | null;
  sinais: string[];
  pontos: {
    dataUtc: string;
    carga: number | null;
    series: number | null;
    repeticoes: string | null;
    rpe: number | null;
  }[];
}

interface AnaliseProgresso {
  resumo: {
    execucoesPeriodo: number;
    progredindo: number;
    estagnacao: number;
    possivelFadiga: number;
    estavel: number;
    semBase: number;
    revisaoSugerida: number;
  };
  exercicios: ExercicioAnalise[];
  observacao: string;
}

const STATUS: Record<
  string,
  { rotulo: string; tom: "ok" | "warn" | "bad" | "neutral" | "info" }
> = {
  Progredindo: { rotulo: "Progredindo", tom: "ok" },
  Estavel: { rotulo: "Estável", tom: "info" },
  Estagnacao: { rotulo: "Estagnado", tom: "warn" },
  PossivelFadiga: { rotulo: "Possível fadiga", tom: "bad" },
  SemBase: { rotulo: "Pouco dado", tom: "neutral" },
};
const ORDEM = [
  "PossivelFadiga",
  "Estagnacao",
  "Progredindo",
  "Estavel",
  "SemBase",
];

function MiniLinha({ pontos }: { pontos: (number | null)[] }) {
  const v = pontos.filter((x): x is number => x != null);
  if (v.length < 2) return null;
  const min = Math.min(...v);
  const max = Math.max(...v);
  const w = 72;
  const h = 24;
  const d = v
    .map(
      (y, i) =>
        `${(i / (v.length - 1)) * w},${max === min ? h / 2 : h - ((y - min) / (max - min)) * (h - 4) - 2}`,
    )
    .join(" ");
  return (
    <svg width={w} height={h} className="shrink-0 text-accent" aria-hidden>
      <polyline
        points={d}
        fill="none"
        stroke="currentColor"
        strokeWidth={2}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function ProgressoCarga({ pacienteId }: { pacienteId: string }) {
  const [aberto, setAberto] = useState<string | null>(null);
  const [todos, setTodos] = useState(false);
  const q = useQuery({
    queryKey: ["pro", "analise-progresso", pacienteId],
    queryFn: () =>
      api<AnaliseProgresso>(
        `/api/pacientes/${pacienteId}/treinos/analise-progresso`,
      ),
  });
  if (q.isLoading) return <Skeleton className="h-32" />;
  const d = q.data;
  if (!d || !d.exercicios.length) return null;
  const lista = [...d.exercicios].sort(
    (a, b) =>
      ORDEM.indexOf(a.status) - ORDEM.indexOf(b.status) ||
      (b.variacaoRecentePercentual ?? 0) - (a.variacaoRecentePercentual ?? 0),
  );
  const visiveis = todos ? lista : lista.slice(0, 6);
  const r = d.resumo;
  return (
    <section>
      <div className="mb-2">
        <p className="font-semibold">Progresso de carga</p>
        <p className="text-[13px] text-muted">
          Últimos 120 dias, pelos treinos registrados.
        </p>
      </div>
      <Card className="overflow-hidden">
        <div className="grid grid-cols-4 divide-x divide-line/60 border-b border-line/60 text-center">
          {[
            [r.progredindo, "progredindo", "text-ok"],
            [r.estavel, "estáveis", "text-info"],
            [r.estagnacao, "estagnados", "text-warn"],
            [r.possivelFadiga, "fadiga?", "text-bad"],
          ].map(([n, rot, cor]) => (
            <div key={rot as string} className="px-1 py-3">
              <p
                className={clsx(
                  "num text-xl font-semibold",
                  (n as number) ? (cor as string) : "text-faint",
                )}
              >
                {n}
              </p>
              <p className="text-[11px] text-muted">{rot}</p>
            </div>
          ))}
        </div>
        <div className="divide-y divide-line/60">
          {visiveis.map((x) => {
            const open = aberto === x.exercicioId;
            const st = STATUS[x.status] ?? {
              rotulo: x.status,
              tom: "neutral" as const,
            };
            return (
              <button
                key={x.exercicioId}
                onClick={() => setAberto(open ? null : x.exercicioId)}
                className="block w-full px-4 py-3 text-left transition hover:bg-raised/40"
              >
                <span className="flex items-center gap-3">
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">
                      {x.exercicio}
                    </span>
                    <span className="block text-[13px] text-muted">
                      {[
                        x.ultimaCarga != null
                          ? `última ${n1(x.ultimaCarga)} ${x.unidade ?? ""}`.trim()
                          : null,
                        x.maiorCarga != null
                          ? `recorde ${n1(x.maiorCarga)}`
                          : null,
                        x.variacaoRecentePercentual != null
                          ? `${x.variacaoRecentePercentual > 0 ? "+" : ""}${n1(x.variacaoRecentePercentual)}%`
                          : null,
                      ]
                        .filter(Boolean)
                        .join(" · ")}
                    </span>
                  </span>
                  <MiniLinha pontos={x.pontos.map((p) => p.carga)} />
                  <Pill tone={st.tom}>{st.rotulo}</Pill>
                </span>
                {open && (
                  <span className="mt-2 block rounded-xl bg-raised/60 p-3 text-[13px]">
                    {x.sinais.map((s) => (
                      <span key={s} className="block">
                        • {s}
                      </span>
                    ))}
                    <span className="mt-2 block text-muted">
                      {x.pontos
                        .slice(-5)
                        .map(
                          (p) =>
                            `${dataCurta(p.dataUtc)}: ${p.carga != null ? n1(p.carga) : "—"}${p.rpe != null ? ` (RPE ${n0(p.rpe)})` : ""}`,
                        )
                        .join(" · ")}
                    </span>
                  </span>
                )}
              </button>
            );
          })}
        </div>
        {lista.length > 6 && (
          <button
            onClick={() => setTodos((t) => !t)}
            className="flex w-full items-center justify-center gap-1 border-t border-line/60 py-2.5 text-[13px] font-semibold text-accent"
          >
            {todos ? "Mostrar menos" : `Ver todos os ${lista.length}`}
            <ChevronDown
              className={clsx("h-4 w-4 transition", todos && "rotate-180")}
            />
          </button>
        )}
      </Card>
      <p className="mt-2 text-[12px] text-faint">{d.observacao}</p>
    </section>
  );
}

// ---------------------------------------------------------------- Volume por grupo

interface AnaliseVolume {
  plano: { nome: string } | null;
  resumo: {
    seriesSemanaisEstimadas: number;
    gruposMusculares: number;
    maiorConcentracaoGrupo: string | null;
    maiorConcentracaoPercentual: number | null;
  };
  porGrupo: {
    grupoMuscular: string;
    seriesSemanaisEstimadas: number;
    percentualDoVolumeSemanal: number;
    mediaSeriesRealizadasSemana: number;
  }[];
  observacao: string | null;
}

export function VolumePorGrupo({ pacienteId }: { pacienteId: string }) {
  const q = useQuery({
    queryKey: ["pro", "analise-volume", pacienteId],
    queryFn: () =>
      api<AnaliseVolume>(`/api/pacientes/${pacienteId}/treinos/analise-volume`),
  });
  const d = q.data;
  if (q.isLoading) return <Skeleton className="h-32" />;
  if (!d?.plano || !d.porGrupo.length) return null;
  const max = Math.max(...d.porGrupo.map((g) => g.seriesSemanaisEstimadas));
  return (
    <section>
      <div className="mb-2">
        <p className="font-semibold">Volume semanal por grupo</p>
        <p className="text-[13px] text-muted">
          {d.plano.nome} · ~{n0(d.resumo.seriesSemanaisEstimadas)} séries por
          semana
        </p>
      </div>
      <Card className="flex flex-col gap-2.5 p-4">
        {d.porGrupo.map((g) => (
          <div key={g.grupoMuscular}>
            <div className="mb-1 flex items-baseline justify-between text-[13px]">
              <span className="font-semibold">{g.grupoMuscular}</span>
              <span className="num text-muted">
                {n0(g.seriesSemanaisEstimadas)} séries ·{" "}
                {n0(g.percentualDoVolumeSemanal)}%
              </span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-line/60">
              <div
                className="h-full rounded-full bg-accent"
                style={{ width: `${(g.seriesSemanaisEstimadas / max) * 100}%` }}
              />
            </div>
          </div>
        ))}
        {d.resumo.maiorConcentracaoPercentual != null &&
          d.resumo.maiorConcentracaoPercentual >= 35 && (
            <p className="mt-1 flex items-center gap-1.5 text-[12px] text-warn">
              <TrendingUp className="h-3.5 w-3.5" />{" "}
              {d.resumo.maiorConcentracaoGrupo} concentra{" "}
              {n0(d.resumo.maiorConcentracaoPercentual)}% do volume.
            </p>
          )}
      </Card>
    </section>
  );
}
