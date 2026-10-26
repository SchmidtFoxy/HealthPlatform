import { Camera, FileText, Images } from 'lucide-react';
import { useRef } from 'react';
import { Sheet } from '@/components/ui';
import { ACEITOS_DOCUMENTO } from '@/lib/arquivos';

// Escolha de anexo em tres caminhos, como no WhatsApp: galeria de fotos, camera ou documento.
// Cada um tem seu proprio input; assim o celular abre direto a galeria em vez do gerenciador de arquivos.

export function MenuAnexo({ open, onClose, onArquivo, documentos = true }: { open: boolean; onClose: () => void; onArquivo: (f: File) => void; documentos?: boolean }) {
  const galeria = useRef<HTMLInputElement>(null);
  const camera = useRef<HTMLInputElement>(null);
  const documento = useRef<HTMLInputElement>(null);
  const escolher = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    e.target.value = '';
    onClose();
    if (f) onArquivo(f);
  };
  const opcoes = [
    { ref: galeria, icone: Images, rotulo: 'Galeria de fotos', cor: 'bg-info/15 text-info' },
    { ref: camera, icone: Camera, rotulo: 'Tirar foto', cor: 'bg-accent/15 text-accent' },
    ...(documentos ? [{ ref: documento, icone: FileText, rotulo: 'Documento ou PDF', cor: 'bg-warn/15 text-warn' }] : []),
  ];
  return (
    <>
      <input ref={galeria} type="file" accept="image/*" className="hidden" onChange={escolher} />
      <input ref={camera} type="file" accept="image/*" capture="environment" className="hidden" onChange={escolher} />
      <input ref={documento} type="file" accept={ACEITOS_DOCUMENTO} className="hidden" onChange={escolher} />
      <Sheet open={open} onClose={onClose} title="Enviar">
        <div className="grid grid-cols-3 gap-3 pb-2">
          {opcoes.map(({ ref, icone: Icone, rotulo, cor }) => (
            <button key={rotulo} type="button" onClick={() => ref.current?.click()} className="flex flex-col items-center gap-2 rounded-2xl p-3 text-center transition hover:bg-raised active:scale-95">
              <span className={`grid h-14 w-14 place-items-center rounded-full ${cor}`}>
                <Icone className="h-6 w-6" />
              </span>
              <span className="text-[13px] font-semibold leading-tight">{rotulo}</span>
            </button>
          ))}
        </div>
      </Sheet>
    </>
  );
}
