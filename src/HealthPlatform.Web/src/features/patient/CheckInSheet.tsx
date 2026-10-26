import clsx from 'clsx';
import { useState } from 'react';
import { Button, Sheet, Stepper, toast } from '@/components/ui';
import { hojeIso } from '@/lib/format';
import { useRegistrarCheckIn } from './queries';

const ESCALAS = [
  { key: 'sonoQualidade', label: 'Qualidade do sono', baixo: 'Péssima', alto: 'Ótima' },
  { key: 'energiaNivel', label: 'Energia', baixo: 'Sem energia', alto: 'Cheio de gás' },
  { key: 'disposicaoNivel', label: 'Disposição', baixo: 'Desanimado', alto: 'Motivado' },
  { key: 'recuperacaoNivel', label: 'Recuperação muscular', baixo: 'Muito dolorido', alto: 'Recuperado' },
  { key: 'dorNivel', label: 'Dor', baixo: 'Nenhuma', alto: 'Muita', invertida: true },
] as const;

type Chave = (typeof ESCALAS)[number]['key'];

/** Escala 0-10 em botoes grandes: mais rapido que slider com o polegar. */
function Escala({ value, onChange, invertida }: { value: number; onChange: (v: number) => void; invertida?: boolean }) {
  return (
    <div className="grid grid-cols-11 gap-1">
      {Array.from({ length: 11 }, (_, i) => {
        const ativo = i === value;
        const bom = invertida ? i <= 3 : i >= 7;
        const ruim = invertida ? i >= 7 : i <= 3;
        return (
          <button
            key={i}
            type="button"
            onClick={() => onChange(i)}
            aria-label={String(i)}
            aria-pressed={ativo}
            className={clsx(
              'num h-10 rounded-lg text-sm font-semibold transition active:scale-90',
              ativo
                ? bom
                  ? 'bg-ok text-white'
                  : ruim
                    ? 'bg-bad text-white'
                    : 'bg-warn text-black'
                : 'bg-raised text-muted',
            )}
          >
            {i}
          </button>
        );
      })}
    </div>
  );
}

export function CheckInSheet({ open, onClose }: { open: boolean; onClose: () => void }) {
  const salvar = useRegistrarCheckIn();
  const [sono, setSono] = useState(7);
  const [v, setV] = useState<Record<Chave, number>>({ sonoQualidade: 7, energiaNivel: 7, disposicaoNivel: 7, recuperacaoNivel: 7, dorNivel: 0 });

  async function enviar() {
    try {
      await salvar.mutateAsync({ data: hojeIso(), sonoHoras: sono, ...v });
      toast('Check-in registrado');
      onClose();
    } catch (e) {
      toast(e instanceof Error ? e.message : 'Falhou', 'bad');
    }
  }

  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Como você está hoje?"
      footer={
        <Button size="lg" className="w-full" loading={salvar.isPending} onClick={enviar}>
          Salvar check-in
        </Button>
      }
    >
      <p className="-mt-1 mb-5 text-sm text-muted">Leva uns 20 segundos. Seu profissional usa isso para ajustar a carga do dia.</p>
      <div className="flex flex-col gap-6">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="font-semibold">Horas de sono</p>
            <p className="text-sm text-muted">Última noite</p>
          </div>
          <Stepper value={sono} onChange={setSono} step={0.5} min={0} max={16} suffix="h" />
        </div>
        {ESCALAS.map((e) => (
          <div key={e.key}>
            <div className="mb-2 flex items-baseline justify-between">
              <p className="font-semibold">{e.label}</p>
              <p className="num text-lg font-semibold text-accent">{v[e.key]}</p>
            </div>
            <Escala value={v[e.key]} onChange={(n) => setV((s) => ({ ...s, [e.key]: n }))} invertida={'invertida' in e && e.invertida} />
            <div className="mt-1 flex justify-between text-[11px] text-faint">
              <span>{e.baixo}</span>
              <span>{e.alto}</span>
            </div>
          </div>
        ))}
      </div>
    </Sheet>
  );
}
