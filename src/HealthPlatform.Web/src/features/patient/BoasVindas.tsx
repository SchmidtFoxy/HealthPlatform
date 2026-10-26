import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CheckCircle2, ClipboardList, Dumbbell, MessageCircle, Sun, UtensilsCrossed } from 'lucide-react';
import { useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Sheet } from '@/components/ui';
import { api } from '@/lib/api';

// Tutorial curto de boas-vindas do paciente, o mesmo roteiro da versao classica.
// Aparece sozinho enquanto o onboarding nao foi concluido e pode ser revisto pelo perfil.

interface Onboarding {
  concluido: boolean;
}

export function useOnboardingPaciente(ativo: boolean) {
  return useQuery({
    queryKey: ['onboarding-paciente'],
    queryFn: () => api<Onboarding>('/api/configuracoes/minha-conta/onboarding-paciente'),
    enabled: ativo,
    staleTime: Infinity,
  });
}

type Acao = 'checkin' | '/treino' | '/dieta' | '/chat';

const PASSOS: { icone: ReactNode; titulo: string; texto: string; acao?: { rotulo: string; alvo: Acao } }[] = [
  {
    icone: <Sun className="h-7 w-7" />,
    titulo: 'Seu acompanhamento começa pelo hoje',
    texto: 'A tela Hoje junta o que importa no dia: treino, refeições, cuidados e o que seu profissional pediu.',
  },
  {
    icone: <ClipboardList className="h-7 w-7" />,
    titulo: 'Conte como você está',
    texto: 'O check-in leva menos de um minuto. Sono, energia e dor ajudam a ajustar o treino do dia.',
    acao: { rotulo: 'Fazer check-in agora', alvo: 'checkin' },
  },
  {
    icone: <Dumbbell className="h-7 w-7" />,
    titulo: 'Escolha uma sessão publicada e comece',
    texto: 'Na aba Treino ficam as sessões do seu plano. Abra uma, registre as séries e acompanhe a evolução.',
    acao: { rotulo: 'Ver meus treinos', alvo: '/treino' },
  },
  {
    icone: <UtensilsCrossed className="h-7 w-7" />,
    titulo: 'Seu plano alimentar fica no bolso',
    texto: 'Veja as refeições do dia, marque o que comeu e consulte as substituições quando precisar.',
    acao: { rotulo: 'Ver meu plano', alvo: '/dieta' },
  },
  {
    icone: <MessageCircle className="h-7 w-7" />,
    titulo: 'Dúvida não precisa esperar a próxima consulta',
    texto: 'Mande mensagem para seu profissional pelo chat. Você é avisado quando ele responder.',
    acao: { rotulo: 'Abrir o chat', alvo: '/chat' },
  },
  {
    icone: <CheckCircle2 className="h-7 w-7" />,
    titulo: 'Você já sabe o essencial',
    texto: 'Se quiser rever este passo a passo, ele fica no seu perfil, em “Rever boas-vindas”.',
  },
];

export function BoasVindasSheet({ open, onClose, onCheckin }: { open: boolean; onClose: () => void; onCheckin?: () => void }) {
  const nav = useNavigate();
  const qc = useQueryClient();
  const [passo, setPasso] = useState(0);
  const concluir = useMutation({
    mutationFn: () => api('/api/configuracoes/minha-conta/onboarding-paciente/concluir', { method: 'POST' }),
    onSettled: () => qc.setQueryData<Onboarding>(['onboarding-paciente'], { concluido: true }),
  });

  const fechar = () => {
    concluir.mutate();
    setPasso(0);
    onClose();
  };
  const p = PASSOS[passo];
  const ultimo = passo === PASSOS.length - 1;

  const executar = (alvo: Acao) => {
    fechar();
    if (alvo === 'checkin') onCheckin?.();
    else nav(alvo);
  };

  return (
    <Sheet
      open={open}
      onClose={fechar}
      title="Boas-vindas"
      footer={
        <div className="flex gap-2">
          {ultimo ? (
            <Button className="flex-1" size="lg" onClick={fechar}>
              Começar
            </Button>
          ) : (
            <>
              <Button variant="ghost" size="lg" onClick={fechar}>
                Pular
              </Button>
              <Button className="flex-1" size="lg" onClick={() => setPasso(passo + 1)}>
                Próximo
              </Button>
            </>
          )}
        </div>
      }
    >
      <div className="mb-6 flex gap-1.5" aria-label={`Passo ${passo + 1} de ${PASSOS.length}`}>
        {PASSOS.map((_, i) => (
          <span key={i} className={`h-1.5 flex-1 rounded-full ${i <= passo ? 'bg-accent' : 'bg-line'}`} />
        ))}
      </div>
      <div className="mb-4 grid h-14 w-14 place-items-center rounded-2xl bg-accent/10 text-accent">{p.icone}</div>
      <h3 className="font-display text-xl font-bold">{p.titulo}</h3>
      <p className="mt-2 text-muted">{p.texto}</p>
      {p.acao && (
        <Button variant="soft" className="mt-5 w-full" onClick={() => executar(p.acao!.alvo)}>
          {p.acao.rotulo}
        </Button>
      )}
    </Sheet>
  );
}
