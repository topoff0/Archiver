import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Loader2, Play } from 'lucide-react';
import { processArchive } from './api/archiveApi';
import { CompressionOptions } from './components/CompressionOptions';
import { FilePicker } from './components/FilePicker';
import { ModeSelector } from './components/ModeSelector';
import { PasswordOptions } from './components/PasswordOptions';
import { ResultSummary } from './components/ResultSummary';
import type { ArchiveMode, ArchiveResult } from './types/archive';

const maxFileSize = 100 * 1024 * 1024;

export function App() {
  const [mode, setMode] = useState<ArchiveMode>('compress');
  const [file, setFile] = useState<File | null>(null);
  const [maxCodeLength, setMaxCodeLength] = useState(32);
  const [passwordEnabled, setPasswordEnabled] = useState(false);
  const [password, setPassword] = useState('');
  const [result, setResult] = useState<ArchiveResult | null>(null);
  const [downloadUrl, setDownloadUrl] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const canSubmit = useMemo(() => {
    return Boolean(file) && !isSubmitting && (!passwordEnabled || password.length > 0);
  }, [file, isSubmitting, password, passwordEnabled]);

  useEffect(() => {
    return () => {
      if (downloadUrl) {
        URL.revokeObjectURL(downloadUrl);
      }
    };
  }, [downloadUrl]);

  function handleModeChange(nextMode: ArchiveMode) {
    setMode(nextMode);
    setResult(null);
    setError('');
  }

  function handleFileChange(nextFile: File | null) {
    setFile(nextFile);
    setResult(null);
    setError('');
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (!file) {
      setError('Выберите файл.');
      return;
    }

    if (file.size > maxFileSize) {
      setError('Размер файла не должен превышать 100 МБ.');
      return;
    }

    setIsSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await processArchive({
        mode,
        file,
        maxCodeLength,
        password: passwordEnabled ? password : undefined,
      });

      if (downloadUrl) {
        URL.revokeObjectURL(downloadUrl);
      }

      setDownloadUrl(URL.createObjectURL(response.blob));
      setResult(response);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Не удалось обработать файл.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="app-shell">
      <section className="workspace">
        <div className="panel form-panel">
          <div className="title-row">
            <div>
              <h1>Архиватор Хаффмана</h1>
              <p>.NET REST API + React Vite</p>
            </div>
          </div>

          <form onSubmit={handleSubmit}>
            <ModeSelector mode={mode} onChange={handleModeChange} />
            <FilePicker file={file} onChange={handleFileChange} />

            {mode === 'compress' && (
              <CompressionOptions
                maxCodeLength={maxCodeLength}
                onChange={setMaxCodeLength}
              />
            )}

            <PasswordOptions
              mode={mode}
              enabled={passwordEnabled}
              password={password}
              onEnabledChange={setPasswordEnabled}
              onPasswordChange={setPassword}
            />

            {error && <div className="error-box">{error}</div>}

            <button className="primary-button" type="submit" disabled={!canSubmit}>
              {isSubmitting ? <Loader2 className="spin" size={19} /> : <Play size={19} />}
              {mode === 'compress' ? 'Сжать файл' : 'Распаковать файл'}
            </button>
          </form>
        </div>

        <aside className="panel summary-panel">
          {result && downloadUrl ? (
            <ResultSummary mode={mode} result={result} downloadUrl={downloadUrl} />
          ) : (
            <div className="empty-state">
              <span>HUFF</span>
              <p>Сводка появится после обработки файла.</p>
            </div>
          )}
        </aside>
      </section>
    </main>
  );
}
