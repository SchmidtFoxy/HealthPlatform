import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { AlertTriangle, BellRing, CheckCircle2, MessageCircle, UserCheck } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, Card, ErrorBox, Field, inputCls, Pill, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { tempoRelativo } from '@/lib/format';

interface ProfissionalOpcao {
  id: string;
  nome: string;
  especialidade: string | null;
  tipo: string;
  ativo: boolean;
  contaAtiva: boolean;
  dispositivosPush: number;
}

export interface VinculoPaciente {
  paciente: { id: string; nome: string; email: string | null; ativo: boolean; temAcesso: boolean; acessoAtivo: boolean; emailAcesso: string | null };
  responsavel: {
    origem: 'Definido' | 'UltimaConsulta' | 'UltimoPlano' | 'Conversa' | 'UnicoProfissional' | 'SemResponsavel';
    origemRotulo: string;
    definidoManualmente: boolean;
    profissional: { id: string; nome: string; especialidade: string | null; tipo: string; email: string | null; contaAtiva: boolean; notificarMensagens: boolean; dispositivosPush: number } | null;
  };
  ultimaConsulta: { dataHoraUtc: string; status: string; profissionalId: string; profissionalNome: string } | null;
  conversa: {
    total: number;
    doPaciente: number;
    ultimaDoPacienteEmUtc: string | null;
    ultimaDaEquipeEmUtc: string | null;
    aguardandoResposta: boolean;
    porProfissional: { profissionalId: string; nome: string; doPaciente: number; daEquipe: number; ultimaEmUtc: string; ehResponsavel: boolean }[];
  };
  avisos: { usuarioId: string; nome: string; email: string | null; total: number; lidos: number; naoLidos: number; ultimoEmUtc: string | null; ehResponsavel: boolean }[];
  problemas: { nivel: 'erro' | 'atencao'; texto: string }[];
  profissionais: ProfissionalOpcao[];
}

const STATUS_CONSULTA: Record<string, string> = { Agendada: 'agendada', Confirmada: 'confirmada', Realizada: 'realizada', Cancelada: 'cancelada', Faltou: 'faltou' };
const quando = (iso: string | null | undefined) => (iso ? tempoRelativo(iso) : 'nunca');

/**
 * Admin: quem recebe as mensagens do paciente e por que, para quem foram os avisos,
 * se o profissional consegue ser avisado no celular, e a troca do responsavel.
 */
export function VinculoPacientePainel({ pacienteId }: { pacienteId: string }) {
  const qc = useQueryClient();
  const chave = ['admin', 'vinculo', pacienteId];
  const q = useQuery({ queryKey: chave, queryFn: () => api<VinculoPaciente>(`/api/admin/pacientes/${pacienteId}/vinculo`) });
  const [escolhido, setEscolhido] = useState('');
  const [repassar, setRepassar] = useState(true);
  const d = q.data;
  useEffect(() => {
    if (d) setEscolhido(d.responsavel.profissional?.id ?? '');
  }, [d?.responsavel.profissional?.id]);

  const salvar = useMutation({
    mutationFn: (profissionalId: string | null) =>
      api<{ avisosRepassados: number; vinculo?: VinculoPaciente }>(`/api/admin/pacientes/${pacienteId}/profissional-responsavel`, {
        method: 'PUT',
        json: { profissionalId, repassarAvisos: repassar },
      }),
    onSuccess: (r) => {
      if (r.vinculo) qc.setQueryData(chave, r.vinculo);
      else qc.invalidateQueries({ queryKey: chave });
      qc.invalidateQueries({ queryKey: ['admin'] });
      toast(r.avisosRepassados ? `Responsável salvo. ${r.avisosRepassados} aviso(s) repassado(s).` : 'Responsável salvo.');
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  if (!d) return <Skeleton className="h-72" />;

  const resp = d.responsavel.profissional;
  const ativos = d.profissionais.filter((x) => x.ativo);
  const mudou = escolhido !== (resp?.id ?? '') || (!d.responsavel.definidoManualmente && !!escolhido);

  return (
    <div className="flex flex-col gap-4">
      {d.problemas.length ? (
        <div className="flex flex-col gap-2">
          {d.problemas.map((p) => (
            <div key={p.texto} className={clsx('flex items-start gap-2 rounded-xl p-3 text-[13px] [overflow-wrap:anywhere]', p.nivel === 'erro' ? 'bg-bad/10 text-bad' : 'bg-warn/10')}>
              <AlertTriangle className={clsx('mt-0.5 h-4 w-4 shrink-0', p.nivel === 'erro' ? 'text-bad' : 'text-warn')} />
              <p>{p.texto}</p>
            </div>
          ))}
        </div>
      ) : (
        <div className="flex items-center gap-2 rounded-xl bg-ok/10 p-3 text-[13px]">
          <CheckCircle2 className="h-4 w-4 shrink-0 text-ok" />
          <p>Tudo certo: as mensagens chegam para {resp?.nome}, com aviso no celular.</p>
        </div>
      )}

      <Card className="p-4">
        <p className="flex items-center gap-2 text-[13px] font-semibold text-muted">
          <UserCheck className="h-4 w-4" /> Quem recebe as mensagens
        </p>
        {resp ? (
          <>
            <p className="mt-2 text-[17px] font-semibold">{resp.nome}</p>
            <p className="text-[13px] text-muted">{[resp.especialidade, resp.email].filter(Boolean).join(' · ')}</p>
            <div className="mt-2 flex flex-wrap gap-1.5">
              <Pill tone={d.responsavel.definidoManualmente ? 'ok' : 'warn'}>{d.responsavel.origemRotulo}</Pill>
              <Pill tone={resp.contaAtiva ? 'neutral' : 'bad'}>{resp.contaAtiva ? 'Conta ativa' : 'Conta desativada'}</Pill>
              <Pill tone={resp.dispositivosPush && resp.notificarMensagens ? 'neutral' : 'warn'}>
                {!resp.notificarMensagens ? 'Avisos de mensagem desligados' : resp.dispositivosPush ? `Push em ${resp.dispositivosPush} aparelho(s)` : 'Sem push no celular'}
              </Pill>
            </div>
          </>
        ) : (
          <p className="mt-2 font-semibold text-bad">Ninguém</p>
        )}
        {d.ultimaConsulta && (
          <p className="mt-3 text-[13px] text-muted">
            Última consulta: {new Date(d.ultimaConsulta.dataHoraUtc).toLocaleDateString('pt-BR')} com {d.ultimaConsulta.profissionalNome} ({STATUS_CONSULTA[d.ultimaConsulta.status] ?? d.ultimaConsulta.status})
          </p>
        )}

        <div className="mt-4 flex flex-col gap-3 border-t border-line/60 pt-4">
          <Field label="Profissional responsável" hint="Fica fixo: as mensagens do paciente vão sempre para ele, até alguém trocar aqui.">
            <select className={inputCls} value={escolhido} onChange={(e) => setEscolhido(e.target.value)} aria-label="Profissional responsável">
              <option value="">Escolha um profissional</option>
              {ativos.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nome}
                  {p.especialidade ? ` · ${p.especialidade}` : ''}
                  {!p.contaAtiva ? ' (conta desativada)' : ''}
                </option>
              ))}
            </select>
          </Field>
          <label className="flex items-start gap-2 text-[13px]">
            <input type="checkbox" className="mt-0.5" checked={repassar} onChange={(e) => setRepassar(e.target.checked)} />
            <span>Repassar para ele os avisos de mensagens ainda não lidas</span>
          </label>
          <div className="flex flex-wrap gap-2">
            <Button size="sm" disabled={!escolhido || !mudou || salvar.isPending} onClick={() => salvar.mutate(escolhido)}>
              {d.responsavel.definidoManualmente || escolhido !== (resp?.id ?? '') ? 'Salvar responsável' : 'Confirmar responsável'}
            </Button>
            {d.responsavel.definidoManualmente && (
              <Button size="sm" variant="soft" disabled={salvar.isPending} onClick={() => salvar.mutate(null)}>
                Voltar ao automático
              </Button>
            )}
          </div>
        </div>
      </Card>

      <Card className="p-4">
        <p className="flex items-center gap-2 text-[13px] font-semibold text-muted">
          <MessageCircle className="h-4 w-4" /> Conversa
        </p>
        <div className="mt-2 grid grid-cols-3 gap-2 text-[13px]">
          <Mini l="Mensagens" v={String(d.conversa.total)} />
          <Mini l="Última do paciente" v={quando(d.conversa.ultimaDoPacienteEmUtc)} />
          <Mini l="Última da equipe" v={quando(d.conversa.ultimaDaEquipeEmUtc)} />
        </div>
        {d.conversa.aguardandoResposta && <p className="mt-2 text-[13px] text-warn">O paciente está esperando resposta.</p>}
        {d.conversa.porProfissional.length > 0 && (
          <ul className="mt-3 flex flex-col gap-1.5 text-[13px]">
            {d.conversa.porProfissional.map((p) => (
              <li key={p.profissionalId} className="flex items-center justify-between gap-2">
                <span className="min-w-0 truncate">
                  {p.nome}
                  {p.ehResponsavel && <span className="text-muted"> (responsável)</span>}
                </span>
                <span className="num shrink-0 text-muted">
                  {p.doPaciente} do paciente · {p.daEquipe} da equipe
                </span>
              </li>
            ))}
          </ul>
        )}
      </Card>

      <Card className="p-4">
        <p className="flex items-center gap-2 text-[13px] font-semibold text-muted">
          <BellRing className="h-4 w-4" /> Avisos das mensagens do paciente
        </p>
        {!d.avisos.length ? (
          <p className="mt-2 text-[13px] text-muted">Nenhum aviso gerado ainda.</p>
        ) : (
          <ul className="mt-2 flex flex-col gap-1.5 text-[13px]">
            {d.avisos.map((a) => (
              <li key={a.usuarioId} className="flex items-center justify-between gap-2">
                <span className="min-w-0 truncate">
                  {a.nome}
                  {a.ehResponsavel ? <span className="text-muted"> (responsável)</span> : <span className="text-warn"> (não é o responsável)</span>}
                </span>
                <span className="num shrink-0 text-muted">
                  {a.lidos} lido(s){a.naoLidos ? ` · ${a.naoLidos} não lido(s)` : ''}
                </span>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}

function Mini({ l, v }: { l: string; v: string }) {
  return (
    <div className="rounded-xl bg-raised/60 px-2.5 py-2">
      <p className="text-[11px] text-muted">{l}</p>
      <p className="font-semibold">{v}</p>
    </div>
  );
}
