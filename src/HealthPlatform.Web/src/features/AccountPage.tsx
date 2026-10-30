import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Camera, ChevronDown, ShieldCheck, Trash2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Avatar, useConta } from '@/components/account';
import { AtividadeConta } from './AtividadeConta';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, ErrorBox, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api, renomearSessao } from '@/lib/api';
import { dataCurta } from '@/lib/format';

export interface MinhaConta {
  id: string;
  nome: string;
  email: string;
  tipoUsuario: string;
  fotoPerfilDataUrl: string | null;
  temaPreferido: string | null;
  notificarMensagens: boolean;
  notificarAtualizacoesPlano: boolean;
  notificarLembretes: boolean;
  notificarCheckIns: boolean;
}

export interface Privacidade {
  termos: { versaoAtual: string; aceitosEmUtc: string | null; atualizado: boolean; resumo: string };
  politicaPrivacidade: { versaoAtual: string; aceitaEmUtc: string | null; atualizada: boolean; resumo: string };
  exclusaoDadosSolicitadaEmUtc: string | null;
  revisaoJuridica: string;
  direitos: string[];
}

export const useMinhaConta = () => useQuery({ queryKey: ['conta'], queryFn: () => api<MinhaConta>('/api/configuracoes/minha-conta'), staleTime: 60_000 });
export const usePrivacidade = () => useQuery({ queryKey: ['conta', 'privacidade'], queryFn: () => api<Privacidade>('/api/configuracoes/minha-conta/privacidade'), staleTime: 300_000 });

const AVISOS: { k: keyof MinhaConta; l: string; d: string }[] = [
  { k: 'notificarMensagens', l: 'Mensagens', d: 'Novas mensagens no chat' },
  { k: 'notificarAtualizacoesPlano', l: 'Planos', d: 'Treino ou dieta atualizados' },
  { k: 'notificarLembretes', l: 'Lembretes', d: 'Remédios, metas e pedidos' },
  { k: 'notificarCheckIns', l: 'Check-ins', d: 'Avisos sobre check-ins' },
];

/** Reduz a foto para caber no limite da API (JPEG ate ~400 KB em data URL). */
async function fotoReduzida(file: File) {
  const img = await createImageBitmap(file);
  const lado = 320;
  const escala = Math.min(1, lado / Math.max(img.width, img.height));
  const c = document.createElement('canvas');
  c.width = Math.round(img.width * escala);
  c.height = Math.round(img.height * escala);
  c.getContext('2d')!.drawImage(img, 0, 0, c.width, c.height);
  return c.toDataURL('image/jpeg', 0.82);
}

export function Toggle({ on, onChange, label }: { on: boolean; onChange: (v: boolean) => void; label: string }) {
  return (
    <button
      role="switch"
      aria-checked={on}
      aria-label={label}
      onClick={() => onChange(!on)}
      className={clsx('relative h-7 w-12 shrink-0 rounded-full transition', on ? 'bg-accent' : 'bg-raised')}
    >
      <span className={clsx('absolute top-0.5 h-6 w-6 rounded-full bg-white shadow transition-all', on ? 'left-[22px]' : 'left-0.5')} />
    </button>
  );
}

export function AccountPage() {
  const pro = useLocation().pathname.startsWith('/pro');
  const qc = useQueryClient();
  const conta = useMinhaConta();
  const priv = usePrivacidade();
  const c = conta.data;
  const [nome, setNome] = useState('');
  const fotoRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (c) setNome(c.nome);
  }, [c]);

  const atualizar = () => qc.invalidateQueries({ queryKey: ['conta'] });

  const salvarNome = useMutation({
    mutationFn: () => api<{ nome: string }>('/api/configuracoes/minha-conta', { method: 'PUT', json: { nome: nome.trim() } }),
    onSuccess: (r) => {
      renomearSessao(r.nome);
      toast('Nome atualizado.');
      atualizar();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  const salvarFoto = useMutation({
    mutationFn: (foto: string | null) => api('/api/configuracoes/minha-conta/foto', { method: 'PUT', json: { fotoDataUrl: foto } }),
    onSuccess: () => {
      toast('Foto atualizada.');
      atualizar();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar a foto.', 'bad'),
  });

  const salvarAvisos = useMutation({
    mutationFn: (p: Partial<MinhaConta>) => {
      const tema = document.documentElement.classList.contains('dark') ? 'dark' : 'light';
      const atual = { ...c!, ...p };
      return api('/api/configuracoes/minha-conta/preferencias', {
        method: 'PUT',
        json: {
          temaPreferido: tema,
          notificarMensagens: atual.notificarMensagens,
          notificarAtualizacoesPlano: atual.notificarAtualizacoesPlano,
          notificarLembretes: atual.notificarLembretes,
          notificarCheckIns: atual.notificarCheckIns,
        },
      });
    },
    onMutate: (p) => qc.setQueryData<MinhaConta>(['conta'], (x) => (x ? { ...x, ...p } : x)),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['p', 'lembretes'] }),
    onError: (e) => {
      toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');
      atualizar();
    },
  });

  return (
    <Page>
      <PageHeader back={pro ? '/pro/perfil' : '/perfil'} title="Minha conta" subtitle="Seus dados, senha, avisos e privacidade." />
      {conta.isError && <ErrorBox error={conta.error} onRetry={() => conta.refetch()} />}
      {conta.isLoading && <Skeleton className="h-64" />}
      {c && (
        <>
          <Card className="p-5">
            <div className="flex items-center gap-4">
              <button onClick={() => fotoRef.current?.click()} className="relative shrink-0" aria-label="Trocar foto">
                {c.fotoPerfilDataUrl ? <img src={c.fotoPerfilDataUrl} alt="" className="h-16 w-16 rounded-full object-cover" /> : <Avatar nome={c.nome} size="lg" />}
                <span className="absolute -bottom-1 -right-1 grid h-7 w-7 place-items-center rounded-full border-2 border-surface bg-accent text-accent-ink">
                  <Camera className="h-3.5 w-3.5" />
                </span>
              </button>
              <div className="min-w-0">
                <p className="truncate text-sm text-muted">{c.email}</p>
                {c.fotoPerfilDataUrl && (
                  <button className="mt-1 text-sm font-semibold text-bad" onClick={() => salvarFoto.mutate(null)}>
                    Remover foto
                  </button>
                )}
              </div>
            </div>
            <input
              ref={fotoRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
              onChange={async (e) => {
                const f = e.target.files?.[0];
                e.target.value = '';
                if (!f) return;
                try {
                  salvarFoto.mutate(await fotoReduzida(f));
                } catch {
                  toast('Não consegui ler essa imagem.', 'bad');
                }
              }}
            />
            <div className="mt-4 grid grid-cols-[1fr_auto] items-end gap-2">
              <Field label="Nome">
                <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
              </Field>
              <Button variant="soft" disabled={!nome.trim() || nome.trim() === c.nome} loading={salvarNome.isPending} onClick={() => salvarNome.mutate()}>
                Salvar
              </Button>
            </div>
          </Card>

          {pro && <PerfilProfissionalCard nome={c.nome} />}

          <p className="eyebrow mb-2 mt-7">Avisos que você quer receber</p>
          <Card className="divide-y divide-line/60">
            {AVISOS.map((a) => (
              <div key={a.k} className="flex items-center justify-between gap-3 p-4">
                <span>
                  <span className="block font-semibold">{a.l}</span>
                  <span className="block text-sm text-muted">{a.d}</span>
                </span>
                <Toggle label={a.l} on={!!c[a.k]} onChange={(v) => salvarAvisos.mutate({ [a.k]: v })} />
              </div>
            ))}
          </Card>

          <p className="eyebrow mb-2 mt-7">Segurança</p>
          <SenhaCard />
          <p className="eyebrow mb-2 mt-7">Atividade da conta</p>
          <AtividadeConta />

          <p className="eyebrow mb-2 mt-7">Privacidade</p>
          {priv.data ? <PrivacidadeCard p={priv.data} /> : <Skeleton className="h-40" />}
        </>
      )}
    </Page>
  );
}

function SenhaCard() {
  const [aberto, setAberto] = useState(false);
  const [atual, setAtual] = useState('');
  const [nova, setNova] = useState('');
  const [conf, setConf] = useState('');
  const trocar = useMutation({
    mutationFn: () => api('/api/configuracoes/minha-conta/alterar-senha', { method: 'POST', json: { senhaAtual: atual, novaSenha: nova, confirmacaoNovaSenha: conf } }),
    onSuccess: () => {
      toast('Senha alterada.');
      setAtual('');
      setNova('');
      setConf('');
      setAberto(false);
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível alterar.', 'bad'),
  });
  return (
    <Card className="p-4">
      <button className="flex w-full items-center justify-between font-semibold" onClick={() => setAberto(!aberto)} aria-expanded={aberto}>
        Trocar senha
        <ChevronDown className={clsx('h-5 w-5 text-faint transition', aberto && 'rotate-180')} />
      </button>
      {aberto && (
        <div className="mt-4 flex flex-col gap-3">
          <Field label="Senha atual">
            <input className={inputCls} type="password" autoComplete="current-password" value={atual} onChange={(e) => setAtual(e.target.value)} />
          </Field>
          <Field label="Nova senha" hint="Pelo menos 10 caracteres.">
            <input className={inputCls} type="password" autoComplete="new-password" value={nova} onChange={(e) => setNova(e.target.value)} />
          </Field>
          <Field label="Repita a nova senha">
            <input className={inputCls} type="password" autoComplete="new-password" value={conf} onChange={(e) => setConf(e.target.value)} />
          </Field>
          <Button disabled={!atual || nova.length < 10 || nova !== conf} loading={trocar.isPending} onClick={() => trocar.mutate()}>
            {nova && conf && nova !== conf ? 'As senhas não conferem' : 'Trocar senha'}
          </Button>
        </div>
      )}
    </Card>
  );
}

function PrivacidadeCard({ p }: { p: Privacidade }) {
  const qc = useQueryClient();
  const conta = useConta();
  const emDia = p.termos.atualizado && p.politicaPrivacidade.atualizada;
  const [li, setLi] = useState(false);
  const [desativar, setDesativar] = useState(false);
  const [senha, setSenha] = useState('');
  const [frase, setFrase] = useState('');

  const aceitar = useMutation({
    mutationFn: () => api('/api/configuracoes/minha-conta/consentimentos/aceitar', { method: 'POST', json: { aceitarTermos: true, aceitarPoliticaPrivacidade: true } }),
    onSuccess: () => {
      toast('Aceite registrado com data e versão.');
      qc.invalidateQueries({ queryKey: ['conta', 'privacidade'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível registrar.', 'bad'),
  });
  const exclusao = useMutation({
    mutationFn: () => api('/api/configuracoes/minha-conta/solicitar-exclusao-dados', { method: 'POST' }),
    onSuccess: () => {
      toast('Pedido registrado. A equipe vai analisar.');
      qc.invalidateQueries({ queryKey: ['conta', 'privacidade'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível registrar.', 'bad'),
  });
  const desativa = useMutation({
    mutationFn: () => api('/api/configuracoes/minha-conta/desativar', { method: 'POST', json: { senhaAtual: senha, confirmacao: frase.trim() } }),
    onSuccess: async () => {
      toast('Conta desativada.');
      await conta.sair();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível desativar.', 'bad'),
  });

  return (
    <>
      <Card className="p-4">
        <div className="flex items-center justify-between gap-3">
          <p className="flex items-center gap-2 font-semibold">
            <ShieldCheck className="h-4 w-4 text-accent" /> Termos e privacidade
          </p>
          {emDia ? <Pill tone="ok">Em dia</Pill> : <Pill tone="warn">Aceite pendente</Pill>}
        </div>
        <div className="mt-3 grid grid-cols-2 gap-2 text-[13px]">
          <div className="rounded-xl bg-raised/50 p-3">
            <p className="text-muted">Termos de Uso</p>
            <p className="font-semibold">{p.termos.versaoAtual}</p>
            <p className="text-faint">{p.termos.atualizado && p.termos.aceitosEmUtc ? `Aceito em ${dataCurta(p.termos.aceitosEmUtc)}` : 'Ainda não aceito'}</p>
          </div>
          <div className="rounded-xl bg-raised/50 p-3">
            <p className="text-muted">Privacidade</p>
            <p className="font-semibold">{p.politicaPrivacidade.versaoAtual}</p>
            <p className="text-faint">{p.politicaPrivacidade.atualizada && p.politicaPrivacidade.aceitaEmUtc ? `Aceita em ${dataCurta(p.politicaPrivacidade.aceitaEmUtc)}` : 'Ainda não aceita'}</p>
          </div>
        </div>
        <details className="mt-3 text-[14px]">
          <summary className="cursor-pointer font-semibold">Resumo dos Termos de Uso</summary>
          <p className="mt-1.5 text-muted">{p.termos.resumo}</p>
        </details>
        <details className="mt-2 text-[14px]">
          <summary className="cursor-pointer font-semibold">Resumo da Política de Privacidade</summary>
          <p className="mt-1.5 text-muted">{p.politicaPrivacidade.resumo}</p>
        </details>
        {!emDia && (
          <div className="mt-4 rounded-2xl border border-warn/40 bg-warn/[0.06] p-3">
            <label className="flex items-start gap-2 text-[14px]">
              <input type="checkbox" className="mt-1 h-4 w-4 accent-[rgb(var(--accent))]" checked={li} onChange={(e) => setLi(e.target.checked)} />
              Li e aceito os Termos de Uso e a Política de Privacidade vigentes.
            </label>
            <Button className="mt-3 w-full" disabled={!li} loading={aceitar.isPending} onClick={() => aceitar.mutate()}>
              Registrar aceite
            </Button>
          </div>
        )}
        <p className="mb-1.5 mt-4 font-semibold">Seus direitos</p>
        <ul className="flex list-disc flex-col gap-1 pl-5 text-[14px] text-muted">
          {p.direitos.map((d) => (
            <li key={d}>{d}</li>
          ))}
        </ul>
      </Card>

      <Card className="mt-3 divide-y divide-line/60">
        <div className="flex items-center justify-between gap-3 p-4">
          <span>
            <span className="block font-semibold">Pedir exclusão dos dados</span>
            <span className="block text-sm text-muted">
              {p.exclusaoDadosSolicitadaEmUtc ? `Pedido feito em ${dataCurta(p.exclusaoDadosSolicitadaEmUtc)}, em análise.` : 'A equipe analisa o que pode ser apagado e o que a lei manda guardar.'}
            </span>
          </span>
          {!p.exclusaoDadosSolicitadaEmUtc && (
            <Button size="sm" variant="outline" loading={exclusao.isPending} onClick={() => exclusao.mutate()}>
              Pedir
            </Button>
          )}
        </div>
        <div className="flex items-center justify-between gap-3 p-4">
          <span>
            <span className="block font-semibold text-bad">Desativar conta</span>
            <span className="block text-sm text-muted">Encerra seu acesso. O histórico clínico fica guardado.</span>
          </span>
          <Button size="sm" variant="danger" icon={<Trash2 className="h-4 w-4" />} onClick={() => setDesativar(true)}>
            Desativar
          </Button>
        </div>
      </Card>
      <p className="mt-3 text-[12px] text-faint">{p.revisaoJuridica}</p>

      <Sheet
        open={desativar}
        onClose={() => setDesativar(false)}
        title="Desativar conta?"
        footer={
          <Button variant="danger" size="lg" className="w-full" disabled={!senha || frase.trim() !== 'DESATIVAR'} loading={desativa.isPending} onClick={() => desativa.mutate()}>
            Desativar minha conta
          </Button>
        }
      >
        <div className="flex flex-col gap-4">
          <p className="text-[15px] text-muted">Você perde o acesso ao AESYN e as notificações param. Para voltar, fale com seu profissional.</p>
          <Field label="Sua senha">
            <input className={inputCls} type="password" autoComplete="current-password" value={senha} onChange={(e) => setSenha(e.target.value)} />
          </Field>
          <Field label="Digite DESATIVAR para confirmar">
            <input className={inputCls} value={frase} autoCapitalize="characters" onChange={(e) => setFrase(e.target.value)} />
          </Field>
        </div>
      </Sheet>
    </>
  );
}

interface PerfilProfissional {
  nome: string;
  registroProfissional: string;
  especialidade: string | null;
}

/** Registro e especialidade: sem isso a agenda e as consultas nao funcionam para o profissional. */
function PerfilProfissionalCard({ nome }: { nome: string }) {
  const qc = useQueryClient();
  const q = useQuery({
    queryKey: ['conta', 'profissional'],
    queryFn: () => api<PerfilProfissional>('/api/profissionais/me').catch((e) => (e?.status === 404 ? null : Promise.reject(e))),
  });
  const [registro, setRegistro] = useState('');
  const [especialidade, setEspecialidade] = useState('');
  useEffect(() => {
    if (q.data) {
      setRegistro(q.data.registroProfissional);
      setEspecialidade(q.data.especialidade ?? '');
    }
  }, [q.data]);
  const salvar = useMutation({
    mutationFn: () => api('/api/profissionais/me', { method: 'PUT', json: { nome: q.data?.nome ?? nome, registroProfissional: registro.trim(), especialidade: especialidade.trim() || null } }),
    onSuccess: () => {
      toast('Perfil profissional salvo.');
      qc.invalidateQueries({ queryKey: ['conta', 'profissional'] });
      qc.invalidateQueries({ queryKey: ['pro'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });
  const mudou = q.data ? registro.trim() !== q.data.registroProfissional || especialidade.trim() !== (q.data.especialidade ?? '') : !!registro.trim();
  return (
    <>
      <p className="eyebrow mb-2 mt-7">Perfil profissional</p>
      <Card className="p-4">
        {q.isLoading ? (
          <Skeleton className="h-28" />
        ) : (
          <div className="flex flex-col gap-3">
            {!q.data && <p className="rounded-xl bg-warn/10 p-3 text-sm text-warn">Cadastre seu registro para usar agenda, consultas e mensagens.</p>}
            <Field label="Registro profissional (CRM, CRN, CREF…)">
              <input className={inputCls} value={registro} maxLength={40} placeholder="Ex.: CRM-SP 123456" onChange={(e) => setRegistro(e.target.value)} />
            </Field>
            <Field label="Especialidade">
              <input className={inputCls} value={especialidade} maxLength={120} placeholder="Ex.: Medicina esportiva" onChange={(e) => setEspecialidade(e.target.value)} />
            </Field>
            <Button variant="soft" disabled={!registro.trim() || !mudou} loading={salvar.isPending} onClick={() => salvar.mutate()}>
              Salvar perfil profissional
            </Button>
          </div>
        )}
      </Card>
    </>
  );
}
