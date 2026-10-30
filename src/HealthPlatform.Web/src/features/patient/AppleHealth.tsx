import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Copy, Watch } from 'lucide-react';
import { useState } from 'react';
import { Button, Card, Sheet, toast } from '@/components/ui';
import { api } from '@/lib/api';

interface StatusAppleHealth {
  ativo: boolean;
  criadoEmUtc: string | null;
  ultimoEnvioUtc: string | null;
  linkAtalho: string | null;
  urlEnvio: string;
}

const quando = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });

/** Piloto Apple Watch / Saude do iPhone: conecta pelo app Atalhos com um token pessoal. */
export function AppleHealthCard() {
  const qc = useQueryClient();
  const st = useQuery({ queryKey: ['p', 'apple-health'], queryFn: () => api<StatusAppleHealth>('/api/portal/me/apple-health') });
  const [token, setToken] = useState<string | null>(null);
  const [aberto, setAberto] = useState(false);

  const gerar = useMutation({
    mutationFn: () => api<{ token: string }>('/api/portal/me/apple-health/token', { method: 'POST' }),
    onSuccess: (r) => {
      setToken(r.token);
      setAberto(true);
      qc.invalidateQueries({ queryKey: ['p', 'apple-health'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível gerar o código.', 'bad'),
  });
  const desconectar = useMutation({
    mutationFn: () => api('/api/portal/me/apple-health/token', { method: 'DELETE' }),
    onSuccess: () => {
      setToken(null);
      toast('Apple Saúde desconectado. O atalho para de enviar dados.');
      qc.invalidateQueries({ queryKey: ['p', 'apple-health'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível desconectar.', 'bad'),
  });

  const copiar = async () => {
    if (!token) return;
    try {
      await navigator.clipboard.writeText(token);
      toast('Código copiado.');
    } catch {
      toast('Toque e segure o código para copiar.', 'bad');
    }
  };

  const d = st.data;
  const ativo = !!d?.ativo;
  const resumo = !ativo
    ? 'Envie passos, exercício, FC de repouso e outros dados do Apple Watch para a sua equipe.'
    : d?.ultimoEnvioUtc
      ? `Último envio em ${quando(d.ultimoEnvioUtc)}.`
      : 'Conectado. Aguardando o primeiro envio do atalho.';

  return (
    <Card className="p-4">
      <div className="flex items-center gap-3">
        <span className={clsx('grid h-11 w-11 shrink-0 place-items-center rounded-xl', ativo ? 'bg-ok/15 text-ok' : 'bg-raised text-muted')}>
          <Watch className="h-5 w-5" />
        </span>
        <div className="min-w-0 flex-1">
          <p className="font-semibold">{ativo ? 'Apple Saúde conectado' : 'Apple Saúde (iPhone)'}</p>
          <p className="text-sm text-muted">{resumo}</p>
        </div>
      </div>
      <div className="mt-3 flex gap-2">
        <Button variant={ativo ? 'outline' : 'primary'} className="flex-1" loading={gerar.isPending} onClick={() => (ativo ? setAberto(true) : gerar.mutate())}>
          {ativo ? 'Como funciona' : 'Conectar'}
        </Button>
        {ativo && (
          <Button variant="soft" loading={desconectar.isPending} onClick={() => desconectar.mutate()}>
            Desconectar
          </Button>
        )}
      </div>

      <Sheet open={aberto} onClose={() => setAberto(false)} title="Conectar Apple Saúde" tall>
        <div className="space-y-4 text-[15px] leading-relaxed">
          <p className="text-muted">
            O Apple Watch guarda tudo no app Saúde do iPhone. Um atalho do app <b>Atalhos</b> lê o resumo do dia e manda para o AESYN uma vez por dia. Nada é
            enviado sem a sua permissão, e você pode desconectar quando quiser.
          </p>

          <div>
            <p className="font-semibold">1. Copie o seu código</p>
            {token ? (
              <div className="mt-2 rounded-xl border border-line bg-raised p-3">
                <p className="select-all break-all font-mono text-[13px]">{token}</p>
                <Button variant="soft" size="sm" className="mt-2" icon={<Copy className="h-4 w-4" />} onClick={copiar}>
                  Copiar código
                </Button>
                <p className="mt-2 text-[13px] text-muted">Ele aparece só agora. Se perder, toque em “Gerar novo código” e o anterior deixa de funcionar.</p>
              </div>
            ) : (
              <Button variant="soft" size="sm" className="mt-2" loading={gerar.isPending} onClick={() => gerar.mutate()}>
                Gerar novo código
              </Button>
            )}
          </div>

          <div>
            <p className="font-semibold">2. Instale o atalho do AESYN</p>
            {d?.linkAtalho ? (
              <a href={d.linkAtalho} target="_blank" rel="noreferrer" className="mt-2 inline-block font-semibold text-accent underline">
                Abrir o atalho no iPhone
              </a>
            ) : (
              <p className="mt-1 text-muted">O link do atalho ainda vai ser enviado pela sua equipe.</p>
            )}
            <p className="mt-1 text-muted">Toque em “Adicionar atalho” e cole o código quando ele pedir. Na primeira vez, permita o acesso ao Saúde.</p>
          </div>

          <div>
            <p className="font-semibold">3. Deixe automático</p>
            <p className="mt-1 text-muted">
              No app Atalhos, vá em <b>Automação › Nova automação › Hora do Dia</b>, escolha um horário em que você costuma usar o celular (por exemplo 9h), marque{' '}
              <b>Executar imediatamente</b> e escolha o atalho <b>AESYN Saúde</b>. Ele envia os dados do dia anterior.
            </p>
          </div>

          <p className="text-[13px] text-faint">
            São enviados passos, energia ativa, distância, minutos de exercício, FC de repouso e variabilidade da FC. Esses dados ajudam a sua equipe a acompanhar
            tendências e não geram diagnóstico nem mudam o seu plano sozinhos.
          </p>
        </div>
      </Sheet>
    </Card>
  );
}
