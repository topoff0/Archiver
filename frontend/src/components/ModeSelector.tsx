import { ArchiveRestore, FileArchive } from 'lucide-react';
import type { ArchiveMode } from '../types/archive';

interface ModeSelectorProps {
  mode: ArchiveMode;
  onChange: (mode: ArchiveMode) => void;
}

export function ModeSelector({ mode, onChange }: ModeSelectorProps) {
  return (
    <div className="segmented-control" role="tablist" aria-label="Режим обработки">
      <button
        className={mode === 'compress' ? 'active' : ''}
        type="button"
        role="tab"
        aria-selected={mode === 'compress'}
        onClick={() => onChange('compress')}
      >
        <FileArchive size={18} />
        Сжатие
      </button>
      <button
        className={mode === 'decompress' ? 'active' : ''}
        type="button"
        role="tab"
        aria-selected={mode === 'decompress'}
        onClick={() => onChange('decompress')}
      >
        <ArchiveRestore size={18} />
        Распаковка
      </button>
    </div>
  );
}
