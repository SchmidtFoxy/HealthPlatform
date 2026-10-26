import { keepPreviousData, useQuery } from "@tanstack/react-query";
import clsx from "clsx";
import {
  AlertTriangle,
  CalendarClock,
  PhoneCall,
  Search,
  User,
} from "lucide-react";
import { useDeferredValue, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { inputCls, Sheet, Skeleton } from "@/components/ui";
import { api } from "@/lib/api";
import { dataCurta } from "@/lib/format";

// Busca unica da equipe: pacientes, pendencias abertas, contatos e consultas.

interface Resultado {
  tipo: string;
  id: string;
  pacienteId: string | null;
  titulo: string;
  subtitulo: string | null;
  dataUtc: string | null;
  destino: string;
  severidade: string | null;
}

const ICONE: Record<string, typeof User> = {
  Paciente: User,
  Pendência: AlertTriangle,
  "Follow-up": PhoneCall,
  Consulta: CalendarClock,
};
const ROTULO: Record<string, string> = {
  Paciente: "Pacientes",
  Pendência: "Pendências",
  "Follow-up": "Contatos",
  Consulta: "Consultas",
};
const ROTA: Record<string, string> = {
  pendencias: "/pro/central?aba=pendencias",
  followups: "/pro/central?aba=contatos",
  agenda: "/pro/agenda",
};

export function BuscaGlobalSheet({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const nav = useNavigate();
  const [termo, setTermo] = useState("");
  const t = useDeferredValue(termo.trim());
  const input = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (open) {
      setTermo("");
      setTimeout(() => input.current?.focus(), 250);
    }
  }, [open]);
  const q = useQuery({
    queryKey: ["pro", "busca", t],
    queryFn: () =>
      api<{ total: number; itens: Resultado[] }>(
        `/api/busca?termo=${encodeURIComponent(t)}&limite=30`,
      ),
    enabled: open && t.length >= 2,
    placeholderData: keepPreviousData,
  });
  const itens = t.length >= 2 ? (q.data?.itens ?? []) : [];
  const grupos = [...new Set(itens.map((i) => i.tipo))];

  const abrir = (r: Resultado) => {
    onClose();
    if (r.tipo === "Paciente" || !ROTA[r.destino])
      nav(r.pacienteId ? `/pro/pacientes/${r.pacienteId}` : "/pro/pacientes");
    else nav(ROTA[r.destino]);
  };

  return (
    <Sheet open={open} onClose={onClose} title="Buscar" tall>
      <label className="relative block">
        <Search className="pointer-events-none absolute left-3.5 top-3 h-5 w-5 text-faint" />
        <input
          ref={input}
          type="search"
          aria-label="Buscar"
          placeholder="Paciente, pendência, contato ou consulta"
          value={termo}
          onChange={(e) => setTermo(e.target.value)}
          className={clsx(inputCls, "pl-11")}
        />
      </label>
      <div className="mt-4">
        {t.length < 2 ? (
          <p className="text-sm text-muted">Digite pelo menos 2 letras.</p>
        ) : q.isLoading ? (
          <Skeleton className="h-40" />
        ) : !itens.length ? (
          <p className="text-sm text-muted">Nada encontrado para “{t}”.</p>
        ) : (
          grupos.map((g) => {
            const Icone = ICONE[g] ?? Search;
            return (
              <div key={g} className="mb-4">
                <p className="eyebrow mb-1.5">{ROTULO[g] ?? g}</p>
                <div className="flex flex-col">
                  {itens
                    .filter((i) => i.tipo === g)
                    .map((r) => (
                      <button
                        key={`${r.tipo}-${r.id}`}
                        onClick={() => abrir(r)}
                        className="flex items-center gap-3 rounded-xl px-2 py-2.5 text-left transition hover:bg-raised/60"
                      >
                        <span
                          className={clsx(
                            "grid h-9 w-9 shrink-0 place-items-center rounded-full",
                            r.severidade === "Alta"
                              ? "bg-bad/15 text-bad"
                              : "bg-accent/10 text-accent",
                          )}
                        >
                          <Icone className="h-4 w-4" />
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className="block truncate font-semibold">
                            {r.titulo}
                          </span>
                          {(r.subtitulo || r.dataUtc) && (
                            <span className="block truncate text-[13px] text-muted">
                              {[r.subtitulo, r.dataUtc && dataCurta(r.dataUtc)]
                                .filter(Boolean)
                                .join(" · ")}
                            </span>
                          )}
                        </span>
                      </button>
                    ))}
                </div>
              </div>
            );
          })
        )}
      </div>
    </Sheet>
  );
}
