import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Archive, ChevronRight, Copy, FolderOpen, KeyRound, Lock, MoreHorizontal, Pin, PinOff, StickyNote, Tag, UserCog, UserRoundCheck, X } from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import type { Paciente } from '@/lib/types';
import { Chips } from './CuidadosPro';

type PacienteFicha = Paciente & { motivoStatusAcompanhamento?: string | null };
type Acao = 'dados' | 'acesso' | 'status' | 'tags' | 'notas';

const erroToast = (e: unknown) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');

const invalidarPaciente = (qc: ReturnType<typeof useQueryClient>, id: string) => {
  qc.invalidateQueries({ queryKey: ['pro', 'paciente', id] });
  qc.invalidateQueries({ queryKey: ['pro', 'pacientes'] });
};

export const STATUS_ACOMPANHAMENTO: Record<string, string> = {
  Ativo: 'Ativo',
  Pausado: 'Pausado',
  AguardandoAvaliacao: 'Aguardando avaliação',
  Encerrado: 'Encerrado',
};

/** Botao "Mais" da ficha: dados cadastrais, acesso ao app, status, tags e notas internas. */
export function MaisAcoes({ paciente, className }: { paciente: PacienteFicha; className?: string }) {
  const [menu, setMenu] = useState(false);
  const [acao, setAcao] = useState<Acao | null>(null);
  const abrir = (a: Acao) => {
    setMenu(false);
    // Espera o menu fechar para nao empilhar dois sheets.
    setTimeout(() => setAcao(a), 180);
  };
  const item = (a: Acao, icon: ReactNode, titulo: string, sub: string) => (
    <button key={a} onClick={() => abrir(a)} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40">
      <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">{icon}</span>
      <span className="min-w-0 flex-1">
        <span className="block font-semibold">{titulo}</span>
        <span className="block text-[13px] text-muted">{sub}</span>
      </span>
      <ChevronRight className="h-4 w-4 shrink-0 text-faint" />
    </button>
  );
  const fechar = () => setAcao(null);
  return (
    <>
      <button onClick={() => setMenu(true)} className={className}>
        <MoreHorizontal className="h-4 w-4 shrink-0 text-muted" /> Mais
      </button>
      <Sheet open={menu} onClose={() => setMenu(false)} title={paciente.nome}>
        <div className="-mx-1 divide-y divide-line/60">
          <Link to={`/pro/pacientes/${paciente.id}/arquivos`} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40">
            <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
              <FolderOpen className="h-5 w-5" />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block font-semibold">Arquivos</span>
              <span className="block text-[13px] text-muted">Exames, laudos e fotos enviados</span>
            </span>
            <ChevronRight className="h-4 w-4 shrink-0 text-faint" />
          </Link>
          {item('acesso', <KeyRound className="h-5 w-5" />, 'Acesso ao app', 'Convite, novo link ou revogar')}
          {item('dados', <UserCog className="h-5 w-5" />, 'Editar dados', 'Nome, contato, CPF e nascimento')}
          {item('status', <UserRoundCheck className="h-5 w-5" />, 'Status do acompanhamento', STATUS_ACOMPANHAMENTO[paciente.statusAcompanhamento] ?? paciente.statusAcompanhamento)}
          {item('tags', <Tag className="h-5 w-5" />, 'Tags', paciente.tags?.length ? paciente.tags.join(', ') : 'Organize a carteira por grupos')}
          {item('notas', <StickyNote className="h-5 w-5" />, 'Notas internas', 'Só a equipe vê')}
        </div>
      </Sheet>
      <EditarDadosSheet paciente={acao === 'dados' ? paciente : null} onClose={fechar} />
      <AcessoSheet paciente={acao === 'acesso' ? paciente : null} onClose={fechar} />
      <StatusSheet paciente={acao === 'status' ? paciente : null} onClose={fechar} />
      <TagsSheet paciente={acao === 'tags' ? paciente : null} onClose={fechar} />
      <NotasSheet paciente={acao === 'notas' ? paciente : null} onClose={fechar} />
    </>
  );
}

// ---------------------------------------------------------------- Dados

function EditarDadosSheet({ paciente, onClose }: { paciente: PacienteFicha | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [f, setF] = useState({ nome: '', cpf: '', dataNascimento: '', sexo: '', telefone: '', email: '', profissao: '' });
  useEffect(() => {
    if (!paciente) return;
    setF({
      nome: paciente.nome,
      cpf: paciente.cpf ?? '',
      dataNascimento: paciente.dataNascimento?.slice(0, 10) ?? '',
      sexo: paciente.sexo ?? '',
      telefone: paciente.telefone ?? '',
      email: paciente.email ?? '',
      profissao: paciente.profissao ?? '',
    });
  }, [paciente]);
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF((x) => ({ ...x, [k]: e.target.value }));
  const salvar = useMutation({
    mutationFn: () =>
      api(`/api/pacientes/${paciente!.id}`, {
        method: 'PUT',
        json: Object.fromEntries(Object.entries(f).map(([k, v]) => [k, v.trim() || null])),
      }),
    onSuccess: () => {
      toast('Dados do paciente atualizados.');
      invalidarPaciente(qc, paciente!.id);
      onClose();
    },
    onError: erroToast,
  });
  return (
    <Sheet
      open={!!paciente}
      onClose={onClose}
      title="Editar dados"
      footer={
        <Button size="lg" className="w-full" disabled={f.nome.trim().length < 3} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar alterações
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome completo">
          <input className={inputCls} value={f.nome} maxLength={160} autoComplete="off" onChange={set('nome')} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="CPF">
            <input className={inputCls} value={f.cpf} inputMode="numeric" maxLength={14} onChange={set('cpf')} />
          </Field>
          <Field label="Nascimento">
            <input type="date" className={inputCls} value={f.dataNascimento} onChange={set('dataNascimento')} />
          </Field>
        </div>
        <Field label="Sexo" group>
          <Chips
            value={f.sexo}
            onChange={(v) => setF((x) => ({ ...x, sexo: v }))}
            options={[
              { value: 'Feminino', label: 'Feminino' },
              { value: 'Masculino', label: 'Masculino' },
              { value: 'Outro', label: 'Outro' },
              { value: '', label: 'Não informado' },
            ]}
          />
        </Field>
        <Field label="Telefone">
          <input className={inputCls} type="tel" inputMode="tel" value={f.telefone} maxLength={30} onChange={set('telefone')} />
        </Field>
        <Field label="E-mail">
          <input className={inputCls} type="email" inputMode="email" autoCapitalize="none" value={f.email} maxLength={200} onChange={set('email')} />
        </Field>
        <Field label="Profissão">
          <input className={inputCls} value={f.profissao} maxLength={120} onChange={set('profissao')} />
        </Field>
        <p className="text-[13px] text-muted">Mudar o e-mail aqui não muda o login do app. Para isso, gere um novo convite em Acesso ao app.</p>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Acesso

interface StatusAcesso {
  pacienteId: string;
  possuiAcesso: boolean;
  ativado: boolean;
  email: string | null;
}

export function AcessoSheet({ paciente, onClose }: { paciente: Pick<PacienteFicha, 'id' | 'nome' | 'email' | 'telefone'> | null; onClose: () => void }) {
  const qc = useQueryClient();
  const q = useQuery({
    queryKey: ['pro', 'acesso', paciente?.id],
    queryFn: () => api<StatusAcesso>(`/api/pacientes/${paciente!.id}/acesso`),
    enabled: !!paciente,
  });
  const [email, setEmail] = useState('');
  const [link, setLink] = useState<string | null>(null);
  const [revogar, setRevogar] = useState(false);
  useEffect(() => {
    setLink(null);
    setRevogar(false);
  }, [paciente]);
  useEffect(() => setEmail(q.data?.email ?? paciente?.email ?? ''), [q.data, paciente]);

  const convidar = useMutation({
    mutationFn: () => api<{ email: string; activationToken: string }>(`/api/pacientes/${paciente!.id}/acesso`, { method: 'POST', json: { email: email.trim() } }),
    onSuccess: (r) => {
      setLink(`${location.origin}/ativar?email=${encodeURIComponent(r.email)}&token=${encodeURIComponent(r.activationToken)}`);
      qc.invalidateQueries({ queryKey: ['pro', 'acesso', paciente!.id] });
    },
    onError: erroToast,
  });
  const revogarAcesso = useMutation({
    mutationFn: () => api(`/api/pacientes/${paciente!.id}/acesso`, { method: 'DELETE' }),
    onSuccess: () => {
      toast('Acesso revogado.');
      setRevogar(false);
      setLink(null);
      qc.invalidateQueries({ queryKey: ['pro', 'acesso', paciente!.id] });
    },
    onError: erroToast,
  });

  const s = q.data;
  const copiar = async () => {
    try {
      await navigator.clipboard.writeText(link!);
      toast('Link copiado.');
    } catch {
      toast('Não deu para copiar. Selecione o link e copie.', 'bad');
    }
  };
  const whats = link && paciente?.telefone ? `https://wa.me/${paciente.telefone.replace(/\D/g, '').replace(/^(?!55)(\d{10,11})$/, '55$1')}?text=${encodeURIComponent(`Olá, ${paciente.nome.split(' ')[0]}! Este é o seu acesso ao acompanhamento na AESYN. Abra o link para criar sua senha: ${link}`)}` : null;

  return (
    <Sheet
      open={!!paciente}
      onClose={onClose}
      title="Acesso ao app"
      footer={
        !link && (
          <Button size="lg" className="w-full" disabled={!/^\S+@\S+\.\S+$/.test(email.trim())} loading={convidar.isPending} onClick={() => convidar.mutate()}>
            {s?.possuiAcesso ? 'Gerar novo convite' : 'Liberar acesso'}
          </Button>
        )
      }
    >
      {q.isLoading || !s ? (
        <Skeleton className="h-32" />
      ) : (
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between gap-3 rounded-xl bg-raised/60 p-3">
            <span className="min-w-0 text-[14px]">
              {s.possuiAcesso ? (s.ativado ? 'O paciente já entra no app com este e-mail.' : 'Convite gerado. Aguardando o paciente criar a senha.') : 'Este paciente ainda não tem acesso ao app.'}
            </span>
            <Pill tone={s.ativado ? 'ok' : s.possuiAcesso ? 'warn' : 'neutral'}>{s.ativado ? 'Ativo' : s.possuiAcesso ? 'Pendente' : 'Sem acesso'}</Pill>
          </div>
          {link ? (
            <div className="flex flex-col gap-3">
              <p className="text-[14px] font-semibold">Convite pronto</p>
              <p className="text-[13px] text-muted">Envie este link ao paciente. Ao abrir, ele cria a própria senha e já entra no app. Gerar outro convite invalida este.</p>
              <textarea readOnly className={clsx(inputCls, 'h-24 py-2.5 text-[13px]')} value={link} onFocus={(e) => e.currentTarget.select()} aria-label="Link do convite" />
              <div className="grid grid-cols-2 gap-2">
                <Button variant="soft" icon={<Copy className="h-4 w-4" />} onClick={copiar}>
                  Copiar link
                </Button>
                {whats ? (
                  <a href={whats} target="_blank" rel="noreferrer">
                    <Button variant="outline" className="w-full">
                      WhatsApp
                    </Button>
                  </a>
                ) : (
                  <Button variant="outline" onClick={() => setLink(null)}>
                    Concluir
                  </Button>
                )}
              </div>
            </div>
          ) : (
            <>
              <Field label="E-mail de acesso">
                <input className={inputCls} type="email" inputMode="email" autoCapitalize="none" value={email} maxLength={200} onChange={(e) => setEmail(e.target.value)} />
              </Field>
              {s.possuiAcesso && <p className="text-[13px] text-muted">Um novo convite desativa o login atual até o paciente criar a nova senha.</p>}
              {s.possuiAcesso &&
                (revogar ? (
                  <div className="rounded-xl border border-bad/40 p-3">
                    <p className="text-[14px]">O paciente deixa de entrar no app. O histórico continua salvo.</p>
                    <div className="mt-3 grid grid-cols-2 gap-2">
                      <Button variant="ghost" onClick={() => setRevogar(false)}>
                        Cancelar
                      </Button>
                      <Button variant="danger" loading={revogarAcesso.isPending} onClick={() => revogarAcesso.mutate()}>
                        Revogar
                      </Button>
                    </div>
                  </div>
                ) : (
                  <Button variant="ghost" className="text-bad" icon={<Lock className="h-4 w-4" />} onClick={() => setRevogar(true)}>
                    Revogar acesso
                  </Button>
                ))}
            </>
          )}
        </div>
      )}
    </Sheet>
  );
}

// ---------------------------------------------------------------- Status

function StatusSheet({ paciente, onClose }: { paciente: PacienteFicha | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [status, setStatus] = useState('Ativo');
  const [motivo, setMotivo] = useState('');
  useEffect(() => {
    if (!paciente) return;
    setStatus(paciente.statusAcompanhamento in STATUS_ACOMPANHAMENTO ? paciente.statusAcompanhamento : 'Ativo');
    setMotivo(paciente.motivoStatusAcompanhamento ?? '');
  }, [paciente]);
  const salvar = useMutation({
    mutationFn: () => api(`/api/pacientes/${paciente!.id}/status`, { method: 'PATCH', json: { status, motivo: motivo.trim() || null } }),
    onSuccess: () => {
      toast('Status atualizado.');
      invalidarPaciente(qc, paciente!.id);
      onClose();
    },
    onError: erroToast,
  });
  return (
    <Sheet
      open={!!paciente}
      onClose={onClose}
      title="Status do acompanhamento"
      footer={
        <Button size="lg" className="w-full" loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar status
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Status" group>
          <Chips value={status} onChange={setStatus} options={Object.entries(STATUS_ACOMPANHAMENTO).map(([value, label]) => ({ value, label }))} />
        </Field>
        <Field label="Motivo ou contexto (opcional)">
          <textarea className={clsx(inputCls, 'h-24 py-2.5')} value={motivo} maxLength={500} placeholder="Ex.: pausa nas férias, aguardando exames, acompanhamento encerrado..." onChange={(e) => setMotivo(e.target.value)} />
        </Field>
        <p className="text-[13px] text-muted">É uma organização da carteira. Histórico, prescrições e acesso ao app não são apagados. Encerrado tira o paciente da lista de ativos.</p>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Tags

function TagsSheet({ paciente, onClose }: { paciente: PacienteFicha | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [tags, setTags] = useState<string[]>([]);
  const [nova, setNova] = useState('');
  useEffect(() => {
    setTags(paciente?.tags ?? []);
    setNova('');
  }, [paciente]);
  const adicionar = () => {
    const partes = nova.split(',').map((x) => x.trim()).filter(Boolean);
    if (!partes.length) return;
    setTags((t) => Array.from(new Set([...t, ...partes])).slice(0, 20));
    setNova('');
  };
  const salvar = useMutation({
    mutationFn: () => {
      const final = Array.from(new Set([...tags, ...nova.split(',').map((x) => x.trim()).filter(Boolean)])).slice(0, 20);
      return api(`/api/pacientes/${paciente!.id}/tags`, { method: 'PATCH', json: { tags: final } });
    },
    onSuccess: () => {
      toast('Tags salvas.');
      invalidarPaciente(qc, paciente!.id);
      onClose();
    },
    onError: erroToast,
  });
  return (
    <Sheet
      open={!!paciente}
      onClose={onClose}
      title="Tags"
      footer={
        <Button size="lg" className="w-full" loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar tags
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap gap-2">
          {tags.length ? (
            tags.map((t) => (
              <span key={t} className="inline-flex h-8 items-center gap-1 rounded-full bg-accent/10 pl-3 pr-1.5 text-[13px] font-semibold text-accent">
                {t}
                <button type="button" aria-label={`Remover ${t}`} onClick={() => setTags((x) => x.filter((y) => y !== t))} className="grid h-6 w-6 place-items-center rounded-full hover:bg-accent/20">
                  <X className="h-3.5 w-3.5" />
                </button>
              </span>
            ))
          ) : (
            <p className="text-sm text-muted">Nenhuma tag ainda.</p>
          )}
        </div>
        <form
          className="flex gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            adicionar();
          }}
        >
          <input className={inputCls} value={nova} maxLength={60} placeholder="Ex.: Hipertrofia, Retorno mensal" aria-label="Nova tag" onChange={(e) => setNova(e.target.value)} />
          <Button type="submit" variant="soft" disabled={!nova.trim() || tags.length >= 20}>
            Adicionar
          </Button>
        </form>
        <p className="text-[13px] text-muted">Até 20 tags por paciente. Use para agrupar a carteira e filtrar na lista de pacientes.</p>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Notas internas

interface Nota {
  id: string;
  autorNome: string | null;
  categoria: string | null;
  conteudo: string;
  fixada: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

const quando = (iso: string) => new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

function NotasSheet({ paciente, onClose }: { paciente: PacienteFicha | null; onClose: () => void }) {
  const qc = useQueryClient();
  const id = paciente?.id;
  const q = useQuery({ queryKey: ['pro', 'notas', id], queryFn: () => api<Nota[]>(`/api/pacientes/${id}/notas-internas`), enabled: !!id });
  const [editando, setEditando] = useState<Nota | null>(null);
  const [conteudo, setConteudo] = useState('');
  const [categoria, setCategoria] = useState('');
  const limpar = () => {
    setEditando(null);
    setConteudo('');
    setCategoria('');
  };
  useEffect(limpar, [paciente]);
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'notas', id] });

  const salvar = useMutation({
    mutationFn: () =>
      api(editando ? `/api/pacientes/${id}/notas-internas/${editando.id}` : `/api/pacientes/${id}/notas-internas`, {
        method: editando ? 'PUT' : 'POST',
        json: { conteudo: conteudo.trim(), categoria: categoria.trim() || null },
      }),
    onSuccess: () => {
      toast(editando ? 'Nota atualizada.' : 'Nota registrada.');
      limpar();
      invalidar();
    },
    onError: erroToast,
  });
  const fixar = useMutation({
    mutationFn: (n: Nota) => api(`/api/pacientes/${id}/notas-internas/${n.id}/fixacao`, { method: 'PATCH', json: { fixada: !n.fixada } }),
    onSuccess: invalidar,
    onError: erroToast,
  });
  const arquivar = useMutation({
    mutationFn: (n: Nota) => api(`/api/pacientes/${id}/notas-internas/${n.id}`, { method: 'DELETE' }),
    onSuccess: () => {
      toast('Nota arquivada.');
      invalidar();
    },
    onError: erroToast,
  });

  const notas = q.data ?? [];
  return (
    <Sheet open={!!paciente} onClose={onClose} title="Notas internas" tall>
      <div className="flex flex-col gap-4">
        <p className="flex items-center gap-2 rounded-xl bg-raised/60 px-3 py-2 text-[13px] text-muted">
          <Lock className="h-4 w-4 shrink-0" /> Só a equipe vê. Não aparece para o paciente.
        </p>
        <div className="flex flex-col gap-3">
          <Field label={editando ? 'Editar nota' : 'Nova nota'}>
            <textarea className={clsx(inputCls, 'h-24 py-2.5')} value={conteudo} maxLength={4000} placeholder="Contexto útil para a equipe..." onChange={(e) => setConteudo(e.target.value)} />
          </Field>
          <div className="flex gap-2">
            <input className={clsx(inputCls, 'flex-1')} value={categoria} maxLength={40} placeholder="Categoria (opcional)" aria-label="Categoria" onChange={(e) => setCategoria(e.target.value)} />
            {editando && (
              <Button variant="ghost" onClick={limpar}>
                Cancelar
              </Button>
            )}
            <Button disabled={!conteudo.trim()} loading={salvar.isPending} onClick={() => salvar.mutate()}>
              {editando ? 'Atualizar' : 'Salvar'}
            </Button>
          </div>
        </div>
        {q.isLoading ? (
          <Skeleton className="h-24" />
        ) : !notas.length ? (
          <p className="text-sm text-muted">Nenhuma nota ainda.</p>
        ) : (
          <div className="flex flex-col gap-2">
            {notas.map((n) => (
              <div key={n.id} className={clsx('rounded-xl border p-3 [overflow-wrap:anywhere]', n.fixada ? 'border-accent/50 bg-accent/[0.05]' : 'border-line')}>
                <div className="flex items-center gap-2">
                  {n.categoria && <Pill>{n.categoria}</Pill>}
                  {n.fixada && <Pill tone="accent">Fixada</Pill>}
                  <span className="ml-auto text-[12px] text-faint">{quando(n.updatedAtUtc ?? n.createdAtUtc)}</span>
                </div>
                <p className="mt-2 whitespace-pre-line text-[14px]">{n.conteudo}</p>
                <div className="mt-2 flex items-center gap-1 text-[13px]">
                  <span className="mr-auto text-faint">{n.autorNome}</span>
                  <button
                    className="rounded-full px-2 py-1 font-semibold text-accent hover:bg-raised"
                    onClick={() => {
                      setEditando(n);
                      setConteudo(n.conteudo);
                      setCategoria(n.categoria ?? '');
                    }}
                  >
                    Editar
                  </button>
                  <button aria-label={n.fixada ? 'Desafixar' : 'Fixar'} className="grid h-8 w-8 place-items-center rounded-full text-muted hover:bg-raised" onClick={() => fixar.mutate(n)}>
                    {n.fixada ? <PinOff className="h-4 w-4" /> : <Pin className="h-4 w-4" />}
                  </button>
                  <button aria-label="Arquivar" className="grid h-8 w-8 place-items-center rounded-full text-muted hover:bg-raised" onClick={() => arquivar.mutate(n)}>
                    <Archive className="h-4 w-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </Sheet>
  );
}
