import { ChevronRight, LogOut, Monitor, Moon, ShieldAlert, Sun } from 'lucide-react';
import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { AppleHealthCard } from '@/features/patient/AppleHealth';
import { BoasVindasSheet } from '@/features/patient/BoasVindas';
import { CheckInSheet } from '@/features/patient/CheckInSheet';
import { useMinhaConta, usePrivacidade } from '@/features/AccountPage';
import { Avatar, PAPEL, TrocarConta, useConta } from '@/components/account';
import { NotificacoesCard } from '@/components/notificacoes';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Segmented } from '@/components/ui';
import { useSessao } from '@/lib/session';
import { Tema, useTema } from '@/lib/theme';

export function ProfilePage() {
  const s = useSessao();
  const conta = useConta();
  const caminho = useLocation().pathname;
  const base = caminho.startsWith('/admin') ? '/admin' : caminho.startsWith('/pro') ? '/pro' : '';
  const minha = useMinhaConta().data;
  const priv = usePrivacidade().data;
  const aceitePendente = priv && !(priv.termos.atualizado && priv.politicaPrivacidade.atualizada);
  const [tema, setTema] = useTema();
  const [boasVindas, setBoasVindas] = useState(false);
  const [checkin, setCheckin] = useState(false);
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
      {aceitePendente && (
        <Link to={`${base}/conta`} className="mb-3 flex items-center gap-3 rounded-xl2 border border-warn/40 bg-warn/[0.06] p-4">
          <ShieldAlert className="h-5 w-5 shrink-0 text-warn" />
          <span className="min-w-0 flex-1 text-sm">
            <b className="block">Termos atualizados</b>
            Revise e registre o aceite dos Termos e da Política de Privacidade.
          </span>
          <ChevronRight className="h-5 w-5 shrink-0 text-faint" />
        </Link>
      )}
      <Link to={`${base}/conta`} className="block">
        <Card className="flex items-center gap-4 p-5 transition active:scale-[0.99]">
          {minha?.fotoPerfilDataUrl ? <img src={minha.fotoPerfilDataUrl} alt="" className="h-16 w-16 shrink-0 rounded-full object-cover" /> : <Avatar nome={s?.nome} tipo={s?.tipoUsuario} size="lg" />}
          <div className="min-w-0 flex-1">
            <p className="truncate font-display text-lg font-semibold">{s?.nome}</p>
            <p className="text-sm text-muted">{s ? PAPEL[s.tipoUsuario] : ''} · Minha conta</p>
          </div>
          <ChevronRight className="h-5 w-5 shrink-0 text-faint" />
        </Card>
      </Link>

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

      {s?.tipoUsuario === 'Paciente' && (
        <>
          <p className="eyebrow mb-2 mt-7">Apple Watch e Saúde</p>
          <AppleHealthCard />
        </>
      )}

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
            <Link to="/jornada" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Sua jornada
                <span className="block text-sm font-normal text-muted">Linha do tempo do acompanhamento</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/explorar" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Explorar
                <span className="block text-sm font-normal text-muted">Modalidades, esportes e jeitos de se mover</span>
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
            <button type="button" onClick={() => setBoasVindas(true)} className="flex w-full items-center justify-between p-4 text-left font-semibold">
              <span>
                Rever boas-vindas
                <span className="block text-sm font-normal text-muted">O passo a passo do primeiro acesso</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </button>
          </>
        )}
        {s?.tipoUsuario === 'Admin' && !s.impersonating && (
          <Link to={base === '/admin' ? '/pro' : '/admin'} className="flex items-center justify-between p-4 font-semibold">
            <span>
              {base === '/admin' ? 'Rotina clínica' : 'Painel do administrador'}
              <span className="block text-sm font-normal text-muted">{base === '/admin' ? 'Agenda, pacientes e mensagens, como os profissionais veem' : 'Equipe, pacientes, atividade e dados da clínica'}</span>
            </span>
            <ChevronRight className="h-5 w-5 text-muted" />
          </Link>
        )}
        {s && s.tipoUsuario !== 'Paciente' && base !== '/admin' && (
          <>
            <Link to="/pro/central" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Central do dia
                <span className="block text-sm font-normal text-muted">Pendências, contatos de acompanhamento e pedidos</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/pro/gestao" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Gestão
                <span className="block text-sm font-normal text-muted">Indicadores, carteira, equipe e dados da clínica</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/pro/avisos" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Avisos
                <span className="block text-sm font-normal text-muted">Mensagens, contatos atrasados, pedidos e agenda</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/pro/biblioteca" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Biblioteca
                <span className="block text-sm font-normal text-muted">Exercícios, alimentos, suplementos, modelos e programas</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
            <Link to="/pro/anamnese-exames" className="flex items-center justify-between p-4 font-semibold">
              <span>
                Anamnese e exames
                <span className="block text-sm font-normal text-muted">Suas perguntas de anamnese e marcadores de exame</span>
              </span>
              <ChevronRight className="h-5 w-5 text-muted" />
            </Link>
          </>
        )}
      </Card>

      <BoasVindasSheet open={boasVindas} onClose={() => setBoasVindas(false)} onCheckin={() => setCheckin(true)} />
      <CheckInSheet open={checkin} onClose={() => setCheckin(false)} />
      <Button variant="danger" size="lg" className="mt-8 w-full" icon={<LogOut className="h-5 w-5" />} onClick={conta.sair}>
        Sair da conta
      </Button>
      <p className="mt-6 text-center text-xs text-faint">AESYN · nova experiência</p>
    </Page>
  );
}
