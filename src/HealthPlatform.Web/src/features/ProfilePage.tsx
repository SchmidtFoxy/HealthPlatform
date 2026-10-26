import { ChevronRight, ExternalLink, LogOut, Monitor, Moon, Sun } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Avatar, PAPEL, TrocarConta, useConta } from '@/components/account';
import { NotificacoesCard } from '@/components/notificacoes';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Segmented } from '@/components/ui';
import { useSessao } from '@/lib/session';
import { Tema, useTema } from '@/lib/theme';

export function ProfilePage() {
  const s = useSessao();
  const conta = useConta();
  const [tema, setTema] = useTema();
  return (
    <Page>
      <PageHeader
        title="Perfil"
        action={
          <Button variant="danger" size="sm" icon={<LogOut className="h-4 w-4" />} onClick={conta.sair}>
            Sair
          </Button>
        }
      />
      <Card className="flex items-center gap-4 p-5">
        <Avatar nome={s?.nome} tipo={s?.tipoUsuario} size="lg" />
        <div className="min-w-0">
          <p className="truncate font-display text-lg font-semibold">{s?.nome}</p>
          <p className="text-sm text-muted">{s ? PAPEL[s.tipoUsuario] : ''}</p>
        </div>
      </Card>

      <p className="eyebrow mb-2 mt-7">Trocar de conta</p>
      <TrocarConta />

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

      <p className="eyebrow mb-2 mt-7">Notificações</p>
      <NotificacoesCard />

      <p className="eyebrow mb-2 mt-7">Outras funções</p>
      <Card className="divide-y divide-line">
        {s?.tipoUsuario === 'Paciente' && (
          <>
            <Link to="/chat" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Chat com seu profissional
                <span className="block text-sm font-normal text-muted">Mensagens e dúvidas do acompanhamento</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/cuidados" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Cuidados
                <span className="block text-sm font-normal text-muted">Pedidos do profissional, remédios, suplementos e metas</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/diario" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Diário
                <span className="block text-sm font-normal text-muted">Registros do dia a dia e fechamento do dia</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/arquivos" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Arquivos e exames
                <span className="block text-sm font-normal text-muted">Documentos enviados por você e pela equipe</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
          </>
        )}
        <a href="/?classico=1" className="flex items-center justify-between p-4 font-semibold">
          <span>
            Abrir versão clássica
            <span className="block text-sm font-normal text-muted">
              {s?.tipoUsuario === 'Paciente' ? 'Exploração de esportes e demais módulos' : 'Gestão, equipe, agenda, exames e demais módulos'}
            </span>
          </span>
          <ExternalLink className="h-5 w-5 text-muted" />
        </a>
      </Card>

      <Button variant="danger" size="lg" className="mt-8 w-full" icon={<LogOut className="h-5 w-5" />} onClick={conta.sair}>
        Sair da conta
      </Button>
      <p className="mt-6 text-center text-xs text-faint">AESYN · nova experiência</p>
    </Page>
  );
}
