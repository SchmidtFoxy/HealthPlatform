import { ExternalLink, LogOut, Monitor, Moon, Sun } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Segmented } from '@/components/ui';
import { sair } from '@/lib/api';
import { iniciais } from '@/lib/format';
import { useSessao } from '@/lib/session';
import { Tema, useTema } from '@/lib/theme';

const PAPEL: Record<string, string> = {
  Admin: 'Administrador',
  Medico: 'Médico',
  Nutricionista: 'Nutricionista',
  Personal: 'Personal trainer',
  Secretaria: 'Secretaria',
  Paciente: 'Paciente',
};

export function ProfilePage() {
  const s = useSessao();
  const nav = useNavigate();
  const [tema, setTema] = useTema();
  return (
    <Page>
      <PageHeader title="Perfil" />
      <Card className="flex items-center gap-4 p-5">
        <div className="grid h-16 w-16 place-items-center rounded-full bg-accent/15 font-display text-xl font-semibold text-accent">{iniciais(s?.nome)}</div>
        <div className="min-w-0">
          <p className="truncate font-display text-lg font-semibold">{s?.nome}</p>
          <p className="text-sm text-muted">{PAPEL[s?.tipoUsuario ?? ''] ?? s?.tipoUsuario}</p>
        </div>
      </Card>

      <p className="eyebrow mb-2 mt-7">Aparência</p>
      <Segmented<Tema>
        value={tema}
        onChange={setTema}
        options={[
          { value: 'system', label: <span className="flex items-center justify-center gap-1.5"><Monitor className="h-4 w-4" />Auto</span> },
          { value: 'light', label: <span className="flex items-center justify-center gap-1.5"><Sun className="h-4 w-4" />Claro</span> },
          { value: 'dark', label: <span className="flex items-center justify-center gap-1.5"><Moon className="h-4 w-4" />Escuro</span> },
        ]}
      />

      <p className="eyebrow mb-2 mt-7">Outras funções</p>
      <Card className="divide-y divide-line">
        <a href="/" className="flex items-center justify-between p-4 font-semibold">
          <span>
            Abrir versão clássica
            <span className="block text-sm font-normal text-muted">Exames, anamnese, chat, arquivos e demais módulos</span>
          </span>
          <ExternalLink className="h-5 w-5 text-muted" />
        </a>
      </Card>

      <Button
        variant="danger"
        size="lg"
        className="mt-8 w-full"
        icon={<LogOut className="h-5 w-5" />}
        onClick={async () => {
          await sair();
          nav('/entrar', { replace: true });
        }}
      >
        Sair da conta
      </Button>
      <p className="mt-6 text-center text-xs text-faint">AESYN · nova experiência (prévia)</p>
    </Page>
  );
}
