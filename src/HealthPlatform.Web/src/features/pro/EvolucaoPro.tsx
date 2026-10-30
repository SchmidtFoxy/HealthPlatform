import clsx from 'clsx';
import { ClipboardPlus, ImagePlus } from 'lucide-react';
import { useState } from 'react';
import { Button, ErrorBox, SectionTitle } from '@/components/ui';
import { CheckInsSecao } from '@/features/checkins';
import { CorpoEvolucao, EvolucaoCarregando, FotosEvolucao, GruposMusculares, type Periodo, ResumoPeriodo, SeletorPeriodo, SemanaASemana, useEvolucaoPeriodo } from '@/features/evolucao/Evolucao';

/** Aba "Evolucao" da ficha: o mesmo painel do paciente, com atalhos para lancar avaliacao e fotos no prontuario. */
export function AbaEvolucao({ id, irPara }: { id: string; irPara: (secao: 'avaliacoes' | 'fotos') => void }) {
  const [periodo, setPeriodo] = useState<Periodo>('90');
  const q = useEvolucaoPeriodo(id, periodo);
  return (
    <div className="mt-6">
      <SeletorPeriodo value={periodo} onChange={setPeriodo} />
      <div className="mt-4">
        {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
        {q.isLoading && <EvolucaoCarregando />}
      </div>
      {q.data && (
        <div className={clsx('flex flex-col gap-4 transition-opacity', q.isPlaceholderData && 'opacity-60')}>
          <ResumoPeriodo dados={q.data} periodo={periodo} />
          <SemanaASemana dados={q.data} />
          <div>
            <SectionTitle
              action={
                <Button size="sm" variant="soft" icon={<ClipboardPlus className="h-4 w-4" />} onClick={() => irPara('avaliacoes')}>
                  Nova avaliação
                </Button>
              }
            >
              Corpo
            </SectionTitle>
            <div className="grid gap-4 lg:grid-cols-2">
              <CorpoEvolucao avaliacoes={q.data.avaliacoes} vazio="Nenhuma avaliação física ainda. Lance peso, medidas e composição em Prontuário › Avaliações." />
              <FotosEvolucao
                fotos={q.data.fotos}
                podeEnviar={false}
                rodape={
                  <button onClick={() => irPara('fotos')} className="inline-flex items-center gap-1.5 font-semibold text-accent">
                    <ImagePlus className="h-4 w-4" /> Enviar, comparar e marcar fotos no prontuário
                  </button>
                }
              />
            </div>
          </div>
          <GruposMusculares grupos={q.data.gruposMusculares} />
        </div>
      )}
      <div className="mt-8">
        <CheckInsSecao paciente={id} titulo="Check-ins de evolução" />
      </div>
    </div>
  );
}
