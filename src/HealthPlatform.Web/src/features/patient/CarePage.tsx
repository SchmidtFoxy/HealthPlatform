import { useMutation, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { motion } from 'framer-motion';
import { Check, ClipboardList, Paperclip, Pill as PillIcon, Target, X } from 'lucide-react';
import { useRef, useState } from 'react';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, ErrorBox, Field, inputCls, Pill, SectionTitle, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { ACEITOS, enviarArquivo } from '@/lib/arquivos';
import { dataCurta, hojeIso, n0, n1 } from '@/lib/format';
import { horariosHoje, Medicamento, Meta, Solicitacao, useMedicamentos, useMetas, useSolicitacoes } from './queries';

const STATUS_PEDIDO: Record<string, { label: string; tone: 'warn' | 'info' | 'ok' | 'neutral' }> = {
  Pendente: { label: 'Para responder', tone: 'warn' },
  Enviada: { label: 'Enviado', tone: 'info' },
  Revisada: { label: 'Revisado', tone: 'ok' },
  Cancelada: { label: 'Cancelado', tone: 'neutral' },
};

const prazo = (iso: string | null) => {
  if (!iso) return null;
  const dias = Math.ceil((new Date(iso).getTime() - Date.now()) / 864e5);
  if (dias < 0) return { txt: 'prazo vencido', atrasado: true };
  if (dias === 0) return { txt: 'vence hoje', atrasado: true };
  if (dias === 1) return { txt: 'vence amanhã', atrasado: false };
  return { txt: `até ${dataCurta(iso)}`, atrasado: false };
};

// ---------------------------------------------------------------- Pedidos do profissional

function Pedidos() {
  const q = useSolicitacoes();
  const [aberto, setAberto] = useState<Solicitacao | null>(null);
  const itens = (q.data?.itens ?? []).filter((s) => s.status !== 'Cancelada');
  if (q.isLoading) return <Skeleton className="h-24" />;
  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  if (!itens.length) return <Card className="p-4 text-sm text-muted">Nenhum pedido do seu profissional no momento.</Card>;
  return (
    <>
      <Card className="divide-y divide-line/60">
        {itens.map((s) => {
          const st = STATUS_PEDIDO[s.status] ?? { label: s.status, tone: 'neutral' as const };
          const p = s.status === 'Pendente' ? prazo(s.dataLimiteUtc) : null;
          return (
            <button key={s.id} onClick={() => setAberto(s)} className="flex w-full items-start gap-3 p-4 text-left transition hover:bg-raised/50">
              <span className={clsx('mt-0.5 grid h-10 w-10 shrink-0 place-items-center rounded-xl', s.status === 'Pendente' ? 'bg-warn/15 text-warn' : 'bg-raised text-muted')}>
                <ClipboardList className="h-5 w-5" />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block font-semibold leading-snug">{s.titulo}</span>
                <span className="mt-0.5 block text-[13px] text-muted">
                  {s.tipo} · {s.profissionalNome}
                  {p && <span className={clsx(p.atrasado && 'font-semibold text-bad')}> · {p.txt}</span>}
                </span>
              </span>
              <Pill tone={st.tone}>{st.label}</Pill>
            </button>
          );
        })}
      </Card>
      <ResponderPedido s={aberto} onClose={() => setAberto(null)} />
    </>
  );
}

function ResponderPedido({ s, onClose }: { s: Solicitacao | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [resposta, setResposta] = useState('');
  const [link, setLink] = useState('');
  const [anexo, setAnexo] = useState<File | null>(null);
  const input = useRef<HTMLInputElement>(null);

  const enviar = useMutation({
    mutationFn: async () => {
      let texto = resposta.trim();
      if (anexo) {
        const a = await enviarArquivo(anexo, { categoria: s?.tipo === 'Exame' ? 'Exame' : 'Documento', descricao: s?.titulo });
        texto = [texto, `Arquivo enviado: ${a.nome}`].filter(Boolean).join('\n');
      }
      return api(`/api/portal/me/solicitacoes/${s!.id}/responder`, { method: 'POST', json: { resposta: texto || null, linkResposta: link.trim() || null } });
    },
    onSuccess: () => {
      toast('Resposta enviada ao seu profissional.');
      setResposta('');
      setLink('');
      setAnexo(null);
      qc.invalidateQueries({ queryKey: ['p', 'solicitacoes'] });
      qc.invalidateQueries({ queryKey: ['p', 'arquivos'] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível enviar.', 'bad'),
  });

  const pendente = s?.status === 'Pendente';
  const pode = !!(resposta.trim() || link.trim() || anexo);
  return (
    <Sheet
      open={!!s}
      onClose={() => !enviar.isPending && onClose()}
      title={s?.titulo}
      footer={
        pendente ? (
          <Button size="lg" className="w-full" disabled={!pode} loading={enviar.isPending} onClick={() => enviar.mutate()}>
            Enviar resposta
          </Button>
        ) : undefined
      }
    >
      {s && (
        <div className="flex flex-col gap-4 text-[15px]">
          <p className="text-[13px] text-muted">
            Pedido de {s.profissionalNome} · {dataCurta(s.createdAtUtc)}
            {s.dataLimiteUtc && ` · prazo ${dataCurta(s.dataLimiteUtc)}`}
          </p>
          {s.descricao && <p className="rounded-2xl bg-raised/60 p-3.5">{s.descricao}</p>}
          {pendente ? (
            <>
              <Field label="Sua resposta">
                <textarea className={inputCls + ' h-28 py-3'} value={resposta} maxLength={4000} placeholder="Escreva aqui" onChange={(e) => setResposta(e.target.value)} />
              </Field>
              <input
                ref={input}
                type="file"
                accept={ACEITOS}
                className="hidden"
                onChange={(e) => {
                  setAnexo(e.target.files?.[0] ?? null);
                  e.target.value = '';
                }}
              />
              {anexo ? (
                <div className="flex items-center gap-3 rounded-2xl border border-line p-3">
                  <Paperclip className="h-4 w-4 shrink-0 text-accent" />
                  <span className="min-w-0 flex-1 truncate text-sm font-semibold">{anexo.name}</span>
                  <button aria-label="Tirar anexo" onClick={() => setAnexo(null)} className="grid h-8 w-8 place-items-center rounded-full text-muted hover:bg-raised">
                    <X className="h-4 w-4" />
                  </button>
                </div>
              ) : (
                <Button variant="outline" icon={<Paperclip className="h-4 w-4" />} onClick={() => input.current?.click()}>
                  Anexar foto ou PDF
                </Button>
              )}
              <Field label="Link (opcional)">
                <input className={inputCls} type="url" inputMode="url" value={link} placeholder="https://" onChange={(e) => setLink(e.target.value)} />
              </Field>
            </>
          ) : (
            <div className="flex flex-col gap-2">
              {s.respostaPaciente && <p className="whitespace-pre-wrap rounded-2xl border border-line p-3.5">{s.respostaPaciente}</p>}
              {s.linkResposta && (
                <a href={s.linkResposta} target="_blank" rel="noreferrer" className="truncate text-sm font-semibold text-accent">
                  {s.linkResposta}
                </a>
              )}
              {s.observacaoRevisao && (
                <p className="rounded-2xl bg-ok/10 p-3.5 text-sm">
                  <b>Retorno do profissional:</b> {s.observacaoRevisao}
                </p>
              )}
            </div>
          )}
        </div>
      )}
    </Sheet>
  );
}

// ---------------------------------------------------------------- Medicamentos e suplementos

function Remedios() {
  const qc = useQueryClient();
  const q = useMedicamentos();
  const registrar = useMutation({
    mutationFn: (v: { m: Medicamento; utc: string; status: 'Tomado' | 'Pulado' }) =>
      api(`/api/portal/me/medicamentos/${v.m.id}/tomadas`, { method: 'POST', json: { dataHoraPrevistaUtc: v.utc, status: v.status, observacao: null } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['p', 'medicamentos'] }),
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível registrar.', 'bad'),
  });
  if (q.isLoading) return <Skeleton className="h-24" />;
  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  const meds = q.data?.medicamentos ?? [];
  if (!meds.length) return <Card className="p-4 text-sm text-muted">Nenhum medicamento ou suplemento prescrito para hoje.</Card>;
  const regs = q.data?.registrosHoje ?? [];
  const statusDe = (m: Medicamento, utc: string) => regs.find((r) => r.medicamentoId === m.id && new Date(r.dataHoraPrevistaUtc).getTime() === new Date(utc).getTime())?.status;

  return (
    <Card className="divide-y divide-line/60">
      {meds.map((m) => (
        <div key={m.id} className="p-4">
          <div className="flex items-start gap-3">
            <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-info/15 text-info">
              <PillIcon className="h-5 w-5" />
            </span>
            <div className="min-w-0 flex-1">
              <p className="font-semibold leading-snug">{m.nome}</p>
              <p className="text-[13px] text-muted">
                {[m.dose != null && `${n1(m.dose)} ${m.unidade ?? ''}`.trim(), m.via].filter(Boolean).join(' · ')}
              </p>
              {m.orientacao && <p className="mt-1 text-[13px] text-muted">{m.orientacao}</p>}
            </div>
          </div>
          <div className="mt-3 flex flex-col gap-2">
            {horariosHoje(m).map((h) => {
              const st = statusDe(m, h.utc);
              const ocupado = registrar.isPending && registrar.variables?.m.id === m.id && registrar.variables.utc === h.utc;
              return (
                <div key={h.utc} className="flex items-center gap-2">
                  <span className="num w-14 text-sm font-semibold">{h.hora}</span>
                  <button
                    disabled={ocupado}
                    onClick={() => registrar.mutate({ m, utc: h.utc, status: 'Tomado' })}
                    className={clsx(
                      'flex h-10 flex-1 items-center justify-center gap-1.5 rounded-full text-sm font-semibold transition active:scale-[0.98]',
                      st === 'Tomado' ? 'bg-ok text-white' : 'bg-ok/10 text-ok',
                    )}
                  >
                    <Check className="h-4 w-4" strokeWidth={3} /> {st === 'Tomado' ? 'Tomei' : 'Tomar'}
                  </button>
                  <button
                    disabled={ocupado}
                    onClick={() => registrar.mutate({ m, utc: h.utc, status: 'Pulado' })}
                    className={clsx('h-10 rounded-full px-4 text-sm font-semibold transition', st === 'Pulado' ? 'bg-bad/15 text-bad' : 'bg-raised text-muted')}
                  >
                    Pulei
                  </button>
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </Card>
  );
}

// ---------------------------------------------------------------- Metas

function MetaLinha({ m }: { m: Meta }) {
  const qc = useQueryClient();
  const hoje = hojeIso();
  const reg = m.registros.find((r) => r.data === hoje);
  const quantitativa = m.valorObjetivo != null;
  const [valor, setValor] = useState(reg?.valor != null ? String(reg.valor).replace('.', ',') : '');
  const salvar = useMutation({
    mutationFn: (body: { valor: number | null; concluida: boolean | null }) => api(`/api/portal/me/metas/${m.id}/registro`, { method: 'POST', json: { data: hoje, ...body, observacao: null } }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['p', 'metas'] });
      qc.invalidateQueries({ queryKey: ['p', 'home'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });
  // Ultimos 7 dias, do mais antigo ao de hoje.
  const semana = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(Date.now() - (6 - i) * 864e5);
    const k = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    return m.registros.find((r) => r.data === k)?.concluida;
  });
  const feita = reg?.concluida === true;

  return (
    <div className="p-4">
      <div className="flex items-start gap-3">
        <span className={clsx('grid h-10 w-10 shrink-0 place-items-center rounded-xl', feita ? 'bg-ok text-white' : 'bg-accent/10 text-accent')}>
          {feita ? <Check className="h-5 w-5" strokeWidth={3} /> : <Target className="h-5 w-5" />}
        </span>
        <div className="min-w-0 flex-1">
          <p className="font-semibold leading-snug">{m.nome}</p>
          <p className="text-[13px] text-muted">
            {quantitativa ? `Meta: ${n1(m.valorObjetivo)} ${m.unidade ?? ''}` : m.frequencia === 'Diaria' ? 'Todo dia' : m.frequencia}
            {m.resumo.registros > 0 && ` · ${n0(m.resumo.percentualConclusao)}% cumprida no mês`}
          </p>
        </div>
        <div className="flex shrink-0 gap-1 pt-1.5" aria-label="Últimos 7 dias">
          {semana.map((c, i) => (
            <span key={i} className={clsx('h-2.5 w-2.5 rounded-full', c === true ? 'bg-ok' : c === false ? 'bg-bad/50' : 'bg-raised')} />
          ))}
        </div>
      </div>
      {m.observacoes && <p className="mt-2 text-[13px] text-muted">{m.observacoes}</p>}
      {quantitativa ? (
        <div className="mt-3 flex gap-2">
          <input
            className={inputCls + ' num flex-1'}
            inputMode="decimal"
            value={valor}
            placeholder={`Quanto hoje? (${m.unidade ?? 'valor'})`}
            onChange={(e) => setValor(e.target.value.replace(/[^\d.,]/g, ''))}
          />
          <Button
            variant={feita ? 'soft' : 'primary'}
            loading={salvar.isPending}
            disabled={!valor || Number.isNaN(Number(valor.replace(',', '.')))}
            onClick={() => salvar.mutate({ valor: Number(valor.replace(',', '.')), concluida: null })}
          >
            Salvar
          </Button>
        </div>
      ) : (
        <div className="mt-3 flex gap-2">
          <button
            onClick={() => salvar.mutate({ valor: null, concluida: true })}
            className={clsx('flex h-10 flex-1 items-center justify-center gap-1.5 rounded-full text-sm font-semibold transition', feita ? 'bg-ok text-white' : 'bg-ok/10 text-ok')}
          >
            <Check className="h-4 w-4" strokeWidth={3} /> {feita ? 'Cumpri hoje' : 'Cumpri'}
          </button>
          <button
            onClick={() => salvar.mutate({ valor: null, concluida: false })}
            className={clsx('h-10 rounded-full px-4 text-sm font-semibold transition', reg?.concluida === false ? 'bg-bad/15 text-bad' : 'bg-raised text-muted')}
          >
            Não deu
          </button>
        </div>
      )}
    </div>
  );
}

function Metas() {
  const q = useMetas();
  if (q.isLoading) return <Skeleton className="h-24" />;
  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  const ativas = (q.data?.metas ?? []).filter((m) => m.status === 'Ativa');
  if (!ativas.length) return <Card className="p-4 text-sm text-muted">Seu profissional ainda não definiu metas para você.</Card>;
  return (
    <Card className="divide-y divide-line/60">
      {ativas.map((m) => (
        <MetaLinha key={m.id} m={m} />
      ))}
    </Card>
  );
}

export function CarePage() {
  return (
    <Page wide>
      <PageHeader back="/hoje" eyebrow="Cuidados" title="Seu acompanhamento" subtitle="Pedidos do profissional, remédios e suplementos de hoje e suas metas." />
      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="grid gap-x-6 lg:grid-cols-2">
        <section className="lg:col-span-2">
          <SectionTitle>Pedidos do profissional</SectionTitle>
          <Pedidos />
        </section>
        <section>
          <SectionTitle>Remédios e suplementos de hoje</SectionTitle>
          <Remedios />
        </section>
        <section>
          <SectionTitle>Metas</SectionTitle>
          <Metas />
        </section>
      </motion.div>
    </Page>
  );
}
