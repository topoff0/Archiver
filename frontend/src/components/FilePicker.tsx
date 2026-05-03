import { Upload } from 'lucide-react';
import { formatBytes } from '../utils';

interface FilePickerProps {
  file: File | null;
  onChange: (file: File | null) => void;
}

export function FilePicker({ file, onChange }: FilePickerProps) {
  return (
    <label className="file-picker">
      <input
        type="file"
        onChange={(event) => onChange(event.target.files?.[0] ?? null)}
      />
      <span className="file-icon" aria-hidden="true">
        <Upload size={26} />
      </span>
      <span className="file-title">{file ? file.name : 'Выберите файл'}</span>
      <span className="file-meta">{file ? formatBytes(file.size) : 'до 100 МБ'}</span>
    </label>
  );
}
