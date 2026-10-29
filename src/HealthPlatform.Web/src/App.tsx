import { Activity, Building2, CalendarCheck, CalendarDays, Dumbbell, Home, LayoutDashboard, MessageCircle, Salad, User, Users, UsersRound } from 'lucide-react';
import { lazy, ReactNode, Suspense } from 'react';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { AppShell } from './components/shell';
import { AtalhoAdmin, AtalhoClinico } from './features/admin/Atalhos';
import { rotaInicial } from './lib/api';
import { FaixaSimulacao } from './components/Simulacao';
import { Skeleton, Toaster } from './components/ui';
import { AtivarPage, ConfirmarEmailPage, LoginPage, RedefinirSenhaPage } from './features/auth/LoginPage';
import { HomeSitePage, RecursosPage, SiteLayout, SobrePage, VantagensPage } from './features/site/site';
import { ProfilePage } from './features/ProfilePage';
import { DietPage } from './features/patient/DietPage';
import { ProgressPage } from './features/patient/ProgressPage';
import { TodayPage } from './features/patient/TodayPage';
import { DiaryPage } from './features/patient/DiaryPage';
import { WorkoutPlanPage } from './features/patient/WorkoutPlanPage';
import { useSessao } from './lib/session';
import { useTema } from './lib/theme';

const LiveWorkoutPage = lazy(() => import('./features/patient/LiveWorkoutPage').then((m) => ({ default: m.LiveWorkoutPage })));
const ChatPage = lazy(() => import('./features/chat/Conversa').then((m) => ({ default: m.ChatPage })));
const ProChatPage = lazy(() => import('./features/chat/Conversa').then((m) => ({ default: m.ProChatPage })));
const ConfigClinicaPage = lazy(() => import('./features/pro/ConfigClinicaPage').then((m) => ({ default: m.ConfigClinicaPage })));
const BibliotecaPage = lazy(() => import('./features/pro/BibliotecaPage').then((m) => ({ default: m.BibliotecaPage })));
const GestaoPage = lazy(() => import('./features/pro/GestaoPage').then((m) => ({ default: m.GestaoPage })));
const AvisosPage = lazy(() => import('./features/pro/AvisosPage').then((m) => ({ default: m.AvisosPage })));
const CentralPage = lazy(() => import('./features/pro/CentralPage').then((m) => ({ default: m.CentralPage })));
const AgendaPage = lazy(() => import('./features/pro/AgendaPage').then((m) => ({ default: m.AgendaPage })));
const InboxPage = lazy(() => import('./features/chat/InboxPage').then((m) => ({ default: m.InboxPage })));
const ExplorePage = lazy(() => import('./features/patient/ExplorePage').then((m) => ({ default: m.ExplorePage })));
const ExploreDetailPage = lazy(() => import('./features/patient/ExplorePage').then((m) => ({ default: m.ExploreDetailPage })));
const AccountPage = lazy(() => import('./features/AccountPage').then((m) => ({ default: m.AccountPage })));
const JourneyPage = lazy(() => import('./features/patient/JourneyPage').then((m) => ({ default: m.JourneyPage })));
const CarePage = lazy(() => import('./features/patient/CarePage').then((m) => ({ default: m.CarePage })));
const FilesPage = lazy(() => import('./features/patient/FilesPage').then((m) => ({ default: m.FilesPage })));
const ProTodayPage = lazy(() => import('./features/pro/ProTodayPage').then((m) => ({ default: m.ProTodayPage })));
const PatientsPage = lazy(() => import('./features/pro/PatientsPage').then((m) => ({ default: m.PatientsPage })));
const PatientDetailPage = lazy(() => import('./features/pro/PatientDetailPage').then((m) => ({ default: m.PatientDetailPage })));
const DietEditorPage = lazy(() => import('./features/pro/DietEditorPage').then((m) => ({ default: m.DietEditorPage })));
const AdminInicioPage = lazy(() => import('./features/admin/AdminPages').then((m) => ({ default: m.AdminInicioPage })));
const AdminUsuariosPage = lazy(() => import('./features/admin/AdminPages').then((m) => ({ default: m.AdminUsuariosPage })));
const AdminAtividadePage = lazy(() => import('./features/admin/AdminPages').then((m) => ({ default: m.AdminAtividadePage })));
const AdminClinicaPage = lazy(() => import('./features/admin/AdminPages').then((m) => ({ default: m.AdminClinicaPage })));
const WorkoutEditorPage = lazy(() => import('./features/pro/WorkoutEditorPage').then((m) => ({ default: m.WorkoutEditorPage })));

function Carregando() {
  return (
    <div className="mx-auto flex max-w-xl flex-col gap-3 px-gutter pt-16">
      <Skeleton className="h-8 w-1/2" />
      <Skeleton className="h-40" />
      <Skeleton className="h-24" />
    </div>
  );
}

function Protegido({ papel, children }: { papel: 'paciente' | 'equipe' | 'admin'; children: ReactNode }) {
  const s = useSessao();
  const loc = useLocation();
  if (!s) return <Navigate to="/entrar" replace state={{ de: loc.pathname }} />;
  const paciente = s.tipoUsuario === 'Paciente';
  if (papel === 'paciente' && !paciente) return <Navigate to={rotaInicial(s.tipoUsuario)} replace />;
  if (papel === 'equipe' && paciente) return <Navigate to="/hoje" replace />;
  if (papel === 'admin' && s.tipoUsuario !== 'Admin') return <Navigate to={rotaInicial(s.tipoUsuario)} replace />;
  return <>{children}</>;
}

function Inicio() {
  const s = useSessao();
  if (!s) return <HomeSitePage />;
  return <Navigate to={rotaInicial(s.tipoUsuario)} replace />;
}

const navPaciente = [
  { to: '/hoje', label: 'Hoje', icon: Home },
  { to: '/treino', label: 'Treino', icon: Dumbbell, end: true },
  { to: '/dieta', label: 'Dieta', icon: Salad },
  { to: '/progresso', label: 'Progresso', icon: Activity },
  { to: '/perfil', label: 'Perfil', icon: User },
];

const navEquipe = [
  { to: '/pro', label: 'Hoje', icon: CalendarCheck, end: true },
  { to: '/pro/agenda', label: 'Agenda', icon: CalendarDays },
  { to: '/pro/pacientes', label: 'Pacientes', icon: Users },
  { to: '/pro/mensagens', label: 'Mensagens', icon: MessageCircle },
  { to: '/pro/perfil', label: 'Perfil', icon: User },
];

// Perfil so aparece na barra do celular; no PC a conta fica no rodape do menu lateral.
const navAdmin = [
  { to: '/admin', label: 'Início', icon: LayoutDashboard, end: true },
  { to: '/admin/usuarios', label: 'Usuários', icon: UsersRound },
  { to: '/admin/atividade', label: 'Atividade', icon: Activity },
  { to: '/admin/clinica', label: 'Clínica', icon: Building2 },
  { to: '/admin/perfil', label: 'Perfil', icon: User, only: 'mobile' as const },
];

export function App() {
  useTema();
  return (
    <>
      <Toaster />
      <FaixaSimulacao />
      <Suspense fallback={<Carregando />}>
        <Routes>
          <Route element={<SiteLayout />}>
            <Route path="/" element={<Inicio />} />
            <Route path="/recursos" element={<RecursosPage />} />
            <Route path="/vantagens" element={<VantagensPage />} />
            <Route path="/sobre" element={<SobrePage />} />
          </Route>
          <Route path="/entrar" element={<LoginPage />} />
          <Route path="/ativar" element={<AtivarPage />} />
          <Route path="/redefinir-senha" element={<RedefinirSenhaPage />} />
          <Route path="/confirmar-email" element={<ConfirmarEmailPage />} />
          <Route path="/confirmar-troca-email" element={<ConfirmarEmailPage troca />} />

          <Route
            element={
              <Protegido papel="paciente">
                <AppShell items={navPaciente} />
              </Protegido>
            }
          >
            <Route path="/hoje" element={<TodayPage />} />
            <Route path="/treino" element={<WorkoutPlanPage />} />
            <Route path="/dieta" element={<DietPage />} />
            <Route path="/progresso" element={<ProgressPage />} />
            <Route path="/perfil" element={<ProfilePage />} />
            <Route path="/conta" element={<AccountPage />} />
            <Route path="/arquivos" element={<FilesPage />} />
            <Route path="/cuidados" element={<CarePage />} />
            <Route path="/diario" element={<DiaryPage />} />
            <Route path="/jornada" element={<JourneyPage />} />
            <Route path="/explorar" element={<ExplorePage />} />
            <Route path="/explorar/:codigo" element={<ExploreDetailPage />} />
          </Route>
          <Route
            path="/chat"
            element={
              <Protegido papel="paciente">
                <ChatPage />
              </Protegido>
            }
          />
          <Route
            path="/pro/mensagens/:id"
            element={
              <Protegido papel="equipe">
                <ProChatPage />
              </Protegido>
            }
          />
          <Route
            path="/treino/:sessaoId"
            element={
              <Protegido papel="paciente">
                <LiveWorkoutPage />
              </Protegido>
            }
          />

          <Route
            element={
              <Protegido papel="equipe">
                <AppShell items={navEquipe} atalho={<AtalhoAdmin />} />
              </Protegido>
            }
          >
            <Route path="/pro" element={<ProTodayPage />} />
            <Route path="/pro/pacientes" element={<PatientsPage />} />
            <Route path="/pro/mensagens" element={<InboxPage />} />
            <Route path="/pro/agenda" element={<AgendaPage />} />
            <Route path="/pro/central" element={<CentralPage />} />
            <Route path="/pro/biblioteca" element={<BibliotecaPage />} />
            <Route path="/pro/anamnese-exames" element={<ConfigClinicaPage />} />
            <Route path="/pro/gestao" element={<GestaoPage />} />
            <Route path="/pro/avisos" element={<AvisosPage />} />
            <Route path="/pro/pacientes/:id" element={<PatientDetailPage />} />
            <Route path="/pro/pacientes/:id/arquivos" element={<FilesPage />} />
            <Route path="/pro/perfil" element={<ProfilePage />} />
            <Route path="/pro/conta" element={<AccountPage />} />
          </Route>
          <Route
            element={
              <Protegido papel="admin">
                <AppShell items={navAdmin} atalho={<AtalhoClinico />} />
              </Protegido>
            }
          >
            <Route path="/admin" element={<AdminInicioPage />} />
            <Route path="/admin/usuarios" element={<AdminUsuariosPage />} />
            <Route path="/admin/equipe" element={<Navigate to="/admin/usuarios?aba=equipe" replace />} />
            <Route path="/admin/pacientes" element={<Navigate to="/admin/usuarios?aba=pacientes" replace />} />
            <Route path="/admin/atividade" element={<AdminAtividadePage />} />
            <Route path="/admin/clinica" element={<AdminClinicaPage />} />
            <Route path="/admin/perfil" element={<ProfilePage />} />
            <Route path="/admin/conta" element={<AccountPage />} />
          </Route>
          <Route
            path="/pro/pacientes/:id/dieta/:planoId"
            element={
              <Protegido papel="equipe">
                <DietEditorPage />
              </Protegido>
            }
          />
          <Route
            path="/pro/pacientes/:id/treino/:planoId"
            element={
              <Protegido papel="equipe">
                <WorkoutEditorPage />
              </Protegido>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>
    </>
  );
}
