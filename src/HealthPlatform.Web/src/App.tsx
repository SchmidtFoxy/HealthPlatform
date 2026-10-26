import { Activity, CalendarCheck, Dumbbell, Home, Salad, User, Users } from 'lucide-react';
import { lazy, ReactNode, Suspense } from 'react';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { AppShell } from './components/shell';
import { Skeleton, Toaster } from './components/ui';
import { LoginPage } from './features/auth/LoginPage';
import { HomeSitePage, RecursosPage, SiteLayout, SobrePage, VantagensPage } from './features/site/site';
import { ProfilePage } from './features/ProfilePage';
import { DietPage } from './features/patient/DietPage';
import { ProgressPage } from './features/patient/ProgressPage';
import { TodayPage } from './features/patient/TodayPage';
import { WorkoutPlanPage } from './features/patient/WorkoutPlanPage';
import { useSessao } from './lib/session';
import { useTema } from './lib/theme';

const LiveWorkoutPage = lazy(() => import('./features/patient/LiveWorkoutPage').then((m) => ({ default: m.LiveWorkoutPage })));
const ChatPage = lazy(() => import('./features/patient/ChatPage').then((m) => ({ default: m.ChatPage })));
const CarePage = lazy(() => import('./features/patient/CarePage').then((m) => ({ default: m.CarePage })));
const FilesPage = lazy(() => import('./features/patient/FilesPage').then((m) => ({ default: m.FilesPage })));
const ProTodayPage = lazy(() => import('./features/pro/ProTodayPage').then((m) => ({ default: m.ProTodayPage })));
const PatientsPage = lazy(() => import('./features/pro/PatientsPage').then((m) => ({ default: m.PatientsPage })));
const PatientDetailPage = lazy(() => import('./features/pro/PatientDetailPage').then((m) => ({ default: m.PatientDetailPage })));
const DietEditorPage = lazy(() => import('./features/pro/DietEditorPage').then((m) => ({ default: m.DietEditorPage })));
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

function Protegido({ papel, children }: { papel: 'paciente' | 'equipe'; children: ReactNode }) {
  const s = useSessao();
  const loc = useLocation();
  if (!s) return <Navigate to="/entrar" replace state={{ de: loc.pathname }} />;
  const paciente = s.tipoUsuario === 'Paciente';
  if (papel === 'paciente' && !paciente) return <Navigate to="/pro" replace />;
  if (papel === 'equipe' && paciente) return <Navigate to="/hoje" replace />;
  return <>{children}</>;
}

function Inicio() {
  const s = useSessao();
  if (!s) return <HomeSitePage />;
  return <Navigate to={s.tipoUsuario === 'Paciente' ? '/hoje' : '/pro'} replace />;
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
  { to: '/pro/pacientes', label: 'Pacientes', icon: Users },
  { to: '/pro/perfil', label: 'Perfil', icon: User },
];

export function App() {
  useTema();
  return (
    <>
      <Toaster />
      <Suspense fallback={<Carregando />}>
        <Routes>
          <Route element={<SiteLayout />}>
            <Route path="/" element={<Inicio />} />
            <Route path="/recursos" element={<RecursosPage />} />
            <Route path="/vantagens" element={<VantagensPage />} />
            <Route path="/sobre" element={<SobrePage />} />
          </Route>
          <Route path="/entrar" element={<LoginPage />} />

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
            <Route path="/arquivos" element={<FilesPage />} />
            <Route path="/cuidados" element={<CarePage />} />
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
                <AppShell items={navEquipe} />
              </Protegido>
            }
          >
            <Route path="/pro" element={<ProTodayPage />} />
            <Route path="/pro/pacientes" element={<PatientsPage />} />
            <Route path="/pro/pacientes/:id" element={<PatientDetailPage />} />
            <Route path="/pro/perfil" element={<ProfilePage />} />
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
