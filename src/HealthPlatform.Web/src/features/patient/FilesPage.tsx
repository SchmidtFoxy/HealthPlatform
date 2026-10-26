import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { FileImage, FileText, FolderOpen, Trash2, Upload } from 'lucide-react';
import { useMemo, useRef, useState } from 'react';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { abrirArquivo, ACEITOS, ArquivoPaciente, CATEGORIAS, enviarArquivo, listarArquivos, removerArquivo, tamanho } from '@/lib/arquivos';
import { dataCurta } from '@/lib/format';

export function FilesPage() {
  const qc = useQueryClient();
  const q = useQuery({ queryKey: ['p', 'arquivos'], queryFn: () => listarArquivos() });
  const [filtro, setFiltro] = useState<string>('Todos');
  const [novo, setNovo] = useState<File | null>(null);
  const [categoria, setCategoria] = useState<string>('Exame');
  const [descricao, setDescricao] = useState('');
  const [remover, setRemover] = useState<ArquivoPaciente | null>(null);
  const input = useRef<HTMLInputElement>(null);

  const lista = useMemo(() => [...(q.data ?? [])].sort((a, b) => b.dataUtc.localeCompare(a.dataUtc)), [q.data]);
  const categorias = useMemo(() => ['Todos', ...CATEGORIAS.filter((c) => lista.some((a) => a.categoria === c))], [lista]);
  const visiveis = filtro === 'Todos' ? lista : lista.filter((a) => a.categoria === filtro);

  const enviar = useMutation({
    mutationFn: () => enviarArquivo(novo!, { categoria, descricao: descricao.trim() || undefined }),
    onSuccess: () => {
      toast('Arquivo enviado. Seu profissional já pode ver.');
      setNovo(null);
      setDescricao('');
      qc.invalidateQueries({ queryKey: ['p', 'arquivos'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível enviar.', 'bad'),
  });

  const apagar = useMutation({
    mutationFn: (a: ArquivoPaciente) => removerArquivo(a.id),
    onSuccess: () => {
      setRemover(null);
      qc.invalidateQueries({ queryKey: ['p', 'arquivos'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível remover.', 'bad'),
  });

  return (
    <Page>
      <PageHeader
        back="/perfil"
        title="Arquivos"
        subtitle="Exames, laudos e documentos do seu acompanhamento."
        action={
          <Button size="sm" icon={<Upload className="h-4 w-4" />} onClick={() => input.current?.click()}>
            Enviar
          </Button>
        }
      />
      <input
        ref={input}
        type="file"
        accept={ACEITOS}
        className="hidden"
        onChange={(e) => {
          const f = e.target.files?.[0];
          e.target.value = '';
          if (!f) return;
          setCategoria(f.type.startsWith('image/') ? 'Foto' : 'Exame');
          setNovo(f);
        }}
      />

      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && (
        <div className="flex flex-col gap-2">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      )}
      {q.data && !lista.length && (
        <Empty
          icon={<FolderOpen className="h-5 w-5" />}
          title="Nenhum arquivo ainda"
          action={
            <Button variant="soft" icon={<Upload className="h-4 w-4" />} onClick={() => input.current?.click()}>
              Enviar exame ou documento
            </Button>
          }
        >
          PDF, foto ou documento de até 15 MB. Seu profissional vê na hora.
        </Empty>
      )}

      {categorias.length > 2 && (
        <div className="-mx-gutter mb-3 flex gap-1.5 overflow-x-auto px-gutter no-scrollbar">
          {categorias.map((c) => (
            <button
              key={c}
              onClick={() => setFiltro(c)}
              className={clsx('h-9 shrink-0 rounded-full px-3.5 text-sm font-semibold transition', filtro === c ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
            >
              {c}
            </button>
          ))}
        </div>
      )}

      {!!visiveis.length && (
        <Card className="divide-y divide-line/60">
          {visiveis.map((a) => {
            const Icone = a.contentType.startsWith('image/') ? FileImage : FileText;
            return (
              <div key={a.id} className="flex items-center gap-3 p-3.5">
                <button onClick={() => abrirArquivo(a).catch((e) => toast(e.message, 'bad'))} className="flex min-w-0 flex-1 items-center gap-3 text-left">
                  <span className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
                    <Icone className="h-5 w-5" />
                  </span>
                  <span className="min-w-0">
                    <span className="block truncate font-semibold">{a.descricao || a.nome}</span>
                    <span className="block truncate text-[13px] text-muted">
                      {a.categoria} · {dataCurta(a.dataUtc)} · {tamanho(a.tamanhoBytes)}
                    </span>
                  </span>
                </button>
                {a.origem === 'Profissional' ? (
                  <Pill tone="info">Da equipe</Pill>
                ) : (
                  <button aria-label={`Remover ${a.nome}`} onClick={() => setRemover(a)} className="grid h-9 w-9 shrink-0 place-items-center rounded-full text-faint transition hover:bg-bad/10 hover:text-bad">
                    <Trash2 className="h-4 w-4" />
                  </button>
                )}
              </div>
            );
          })}
        </Card>
      )}

      <Sheet
        open={!!novo}
        onClose={() => !enviar.isPending && setNovo(null)}
        title="Enviar arquivo"
        footer={
          <Button size="lg" className="w-full" loading={enviar.isPending} onClick={() => enviar.mutate()}>
            Enviar para o profissional
          </Button>
        }
      >
        {novo && (
          <div className="flex flex-col gap-4">
            <div className="flex items-center gap-3 rounded-2xl bg-raised/60 p-3">
              <FileText className="h-5 w-5 shrink-0 text-accent" />
              <div className="min-w-0">
                <p className="truncate font-semibold">{novo.name}</p>
                <p className="text-[13px] text-muted">{tamanho(novo.size)}</p>
              </div>
            </div>
            <Field label="Tipo">
              <div className="flex flex-wrap gap-1.5">
                {CATEGORIAS.map((c) => (
                  <button
                    key={c}
                    onClick={() => setCategoria(c)}
                    className={clsx('h-9 rounded-full px-3.5 text-sm font-semibold transition', categoria === c ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
                  >
                    {c}
                  </button>
                ))}
              </div>
            </Field>
            <Field label="Descrição (opcional)">
              <input className={inputCls} value={descricao} maxLength={200} placeholder="Ex.: Hemograma de outubro" onChange={(e) => setDescricao(e.target.value)} />
            </Field>
          </div>
        )}
      </Sheet>

      <Sheet
        open={!!remover}
        onClose={() => setRemover(null)}
        title="Remover arquivo?"
        footer={
          <div className="flex gap-2">
            <Button variant="outline" className="flex-1" onClick={() => setRemover(null)}>
              Cancelar
            </Button>
            <Button variant="danger" className="flex-1" loading={apagar.isPending} onClick={() => remover && apagar.mutate(remover)}>
              Remover
            </Button>
          </div>
        }
      >
        <p className="text-[15px] text-muted">{remover?.nome} sai da sua lista e da do seu profissional.</p>
      </Sheet>
    </Page>
  );
}
