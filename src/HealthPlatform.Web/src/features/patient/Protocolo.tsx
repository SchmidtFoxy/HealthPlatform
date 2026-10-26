import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Activity, Check, ChevronRight } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, Card, Field, inputCls, Sheet, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { Escala } from './CheckInSheet';

/** Item do protocolo de monitoramento definido pelo profissional (peso, pressao, glicemia...). */
export interface ItemProtocolo {
  id: string;
  tipo: string;
  titulo: string;
  unidade: string | null;
  frequencia: 'Diario' | 'DiasSemana' | 'Semanal' | 'SobDemanda' | string;
  horarioLocal: string | null;
  diasSemana: string | null;
  instrucoes: string | null;
  ativo: boolean;
  profissionalNome?: string | null;
}

export interface AderenciaProtocolo {
  dias: number;
  previstos: number;
  concluidos: number;
  percentual: number | null;
  hoje: { previstos: number; concluidos: number; percentual: number | null; itens: { id: string; tipo: string; titulo: string; horarioLocal: string | null; concluido: boolean }[] };
}

export interface Protocolo {
  configurado?: boolean;
  ativos?: number;
  itens: ItemProtocolo[];
  aderencia: AderenciaProtocolo;
}

type Tipo = 'numero' | 'pressao' | 'escala' | 'texto';

/** Como cada tipo e registrado. As chaves sao as aceitas pela API. */
export const TIPOS_PROTOCOLO: Record<string, { l: string; u: string; kind: Tipo; step?: string; invertida?: boolean }> = {
  Peso: { l: 'Peso', u: 'kg', kind: 'numero', step: '0.1' },
  Pressao: { l: 'Pressão arterial', u: 'mmHg', kind: 'pressao' },
  Glicemia: { l: 'Glicemia', u: 'mg/dL', kind: 'numero', step: '1' },
  FrequenciaCardiaca: { l: 'Frequência cardíaca', u: 'bpm', kind: 'numero', step: '1' },
  Saturacao: { l: 'Saturação', u: '%', kind: 'numero', step: '1' },
  Temperatura: { l: 'Temperatura', u: '°C', kind: 'numero', step: '0.1' },
  Agua: { l: 'Água', u: 'ml', kind: 'numero', step: '50' },
  Sono: { l: 'Sono', u: 'h', kind: 'numero', step: '0.5' },
  Dor: { l: 'Dor', u: '0 a 10', kind: 'escala', invertida: true },
  Energia: { l: 'Energia', u: '0 a 10', kind: 'escala' },
  Sintoma: { l: 'Sintomas', u: '', kind: 'texto' },
};

export const FREQUENCIAS_PROTOCOLO: Record<string, string> = {
  Diario: 'Todos os dias',
  DiasSemana: 'Dias da semana',
  Semanal: '1x por semana',
  SobDemanda: 'Quando precisar',
};

export const quandoProtocolo = (x: ItemProtocolo) =>
  [x.frequencia === 'DiasSemana' && x.diasSemana ? x.diasSemana.replaceAll(',', ', ') : FREQUENCIAS_PROTOCOLO[x.frequencia] ?? x.frequencia, x.horarioLocal].filter(Boolean).join(' · ');

const offset = () => -new Date().getTimezoneOffset();

export const useMeuProtocolo = () =>
  useQuery({ queryKey: ['p', 'protocolo'], queryFn: () => api<Protocolo>(`/api/portal/me/protocolo-acompanhamento?offsetMinutos=${offset()}`) });

/** Protocolo de monitoramento do paciente: o que registrar hoje e o que registrar quando precisar. */
export function MeuProtocolo() {
  const q = useMeuProtocolo();
  const [aberto, setAberto] = useState<ItemProtocolo | null>(null);
  const d = q.data;
  if (!d?.configurado) return null;
  const hoje = d.aderencia.hoje;
  const feitos = new Map(hoje.itens.map((x) => [x.id, x.concluido]));
  const outros = d.itens.filter((x) => !feitos.has(x.id));

  const linha = (x: ItemProtocolo) => {
    const t = TIPOS_PROTOCOLO[x.tipo];
    const ok = feitos.get(x.id);
    return (
      <button key={x.id} onClick={() => setAberto(x)} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40">
        <span className={clsx('grid h-10 w-10 shrink-0 place-items-center rounded-xl', ok ? 'bg-ok text-white' : 'bg-accent/10 text-accent')}>
          {ok ? <Check className="h-5 w-5" strokeWidth={3} /> : <Activity className="h-5 w-5" />}
        </span>
        <span className="min-w-0 flex-1">
          <span className="block truncate font-semibold">{x.titulo}</span>
          <span className="block truncate text-[13px] text-muted">{[quandoProtocolo(x), x.unidade ?? t?.u].filter(Boolean).join(' · ')}</span>
        </span>
        <span className="shrink-0 text-[13px] font-semibold text-accent">{ok ? 'Feito' : 'Registrar'}</span>
        <ChevronRight className="h-4 w-4 shrink-0 text-faint" />
      </button>
    );
  };

  return (
    <>
      {hoje.previstos > 0 && (
        <p className="mb-2 text-sm text-muted">
          Hoje: <b className="text-fg">{hoje.concluidos}</b> de {hoje.previstos} registros
          {d.aderencia.percentual != null && ` · ${d.aderencia.percentual}% nos últimos ${d.aderencia.dias} dias`}
        </p>
      )}
      <Card className="divide-y divide-line/60">
        {d.itens.filter((x) => feitos.has(x.id)).map(linha)}
        {outros.map(linha)}
      </Card>
      <RegistrarProtocolo item={aberto} onClose={() => setAberto(null)} />
    </>
  );
}

function RegistrarProtocolo({ item, onClose }: { item: ItemProtocolo | null; onClose: () => void }) {
  const qc = useQueryClient();
  const t = item ? TIPOS_PROTOCOLO[item.tipo] ?? { l: item.titulo, u: item.unidade ?? '', kind: 'texto' as Tipo } : null;
  const [valor, setValor] = useState('');
  const [valor2, setValor2] = useState('');
  const [escala, setEscala] = useState(5);
  const [nota, setNota] = useState('');
  useEffect(() => {
    setValor('');
    setValor2('');
    setEscala(item?.tipo === 'Dor' ? 0 : 5);
    setNota('');
  }, [item]);

  const num = (s: string) => (s.trim() ? Number(s.replace(',', '.')) : NaN);
  const valido = !t ? false : t.kind === 'numero' ? num(valor) > 0 : t.kind === 'pressao' ? num(valor) > 0 && num(valor2) > 0 : t.kind === 'texto' ? !!nota.trim() : true;

  const salvar = useMutation({
    mutationFn: async () => {
      const base = { dataHoraUtc: new Date().toISOString(), imagemUrl: null };
      const unidade = item!.unidade ?? t!.u;
      if (t!.kind === 'pressao') {
        const desc = `${valor}/${valor2} mmHg${nota.trim() ? ` · ${nota.trim()}` : ''}`;
        await api('/api/portal/me/diario', { method: 'POST', json: { ...base, tipo: 'PressaoSistolica', descricao: desc, valorNumerico: num(valor), unidade: 'mmHg', escala: null } });
        await api('/api/portal/me/diario', { method: 'POST', json: { ...base, tipo: 'PressaoDiastolica', descricao: desc, valorNumerico: num(valor2), unidade: 'mmHg', escala: null } });
        return;
      }
      await api('/api/portal/me/diario', {
        method: 'POST',
        json: {
          ...base,
          tipo: item!.tipo,
          descricao: nota.trim() || null,
          valorNumerico: t!.kind === 'numero' ? num(valor) : null,
          unidade: t!.kind === 'numero' ? unidade : null,
          escala: t!.kind === 'escala' ? escala : null,
        },
      });
    },
    onSuccess: () => {
      toast('Registro salvo.');
      qc.invalidateQueries({ queryKey: ['p', 'protocolo'] });
      qc.invalidateQueries({ queryKey: ['p', 'diario-registros'] });
      qc.invalidateQueries({ queryKey: ['p', 'diario'] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      title={item?.titulo}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar registro
        </Button>
      }
    >
      {item && t && (
        <div className="flex flex-col gap-4">
          {item.instrucoes && <p className="rounded-xl bg-accent/10 px-3 py-2 text-[14px]">{item.instrucoes}</p>}
          {t.kind === 'numero' && (
            <Field label={`${t.l} (${item.unidade ?? t.u})`}>
              <input type="number" inputMode="decimal" step={t.step} min="0" className={inputCls} value={valor} autoFocus onChange={(e) => setValor(e.target.value)} />
            </Field>
          )}
          {t.kind === 'pressao' && (
            <div className="grid grid-cols-2 gap-3">
              <Field label="Sistólica (máx.)">
                <input type="number" inputMode="numeric" className={inputCls} value={valor} placeholder="120" autoFocus onChange={(e) => setValor(e.target.value)} />
              </Field>
              <Field label="Diastólica (mín.)">
                <input type="number" inputMode="numeric" className={inputCls} value={valor2} placeholder="80" onChange={(e) => setValor2(e.target.value)} />
              </Field>
            </div>
          )}
          {t.kind === 'escala' && (
            <Field label={`${t.l} agora (0 a 10)`} group>
              <Escala value={escala} onChange={setEscala} invertida={t.invertida} />
            </Field>
          )}
          <Field label={t.kind === 'texto' ? 'O que você está sentindo?' : 'Observação (opcional)'}>
            <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={nota} maxLength={1000} onChange={(e) => setNota(e.target.value)} />
          </Field>
        </div>
      )}
    </Sheet>
  );
}
