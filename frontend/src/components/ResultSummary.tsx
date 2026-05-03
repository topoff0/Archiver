import { Download, ShieldCheck } from 'lucide-react';
import type { ArchiveMode, ArchiveResult } from '../types/archive';
import { formatBytes, formatPercent } from '../utils';

interface ResultSummaryProps {
  mode: ArchiveMode;
  result: ArchiveResult;
  downloadUrl: string;
}

export function ResultSummary({ mode, result, downloadUrl }: ResultSummaryProps) {
  const saved = 1 - result.compressionRatio;

  return (
    <section className="result-panel" aria-live="polite">
      <div className="result-header">
        <div>
          <h2>Результат</h2>
          <p>{result.fileName}</p>
        </div>
        <a className="download-button" href={downloadUrl} download={result.fileName}>
          <Download size={18} />
          Скачать
        </a>
      </div>

      <dl className="stats-grid">
        <div>
          <dt>{mode === 'compress' ? 'Исходный файл' : 'Архив'}</dt>
          <dd>{formatBytes(result.originalSize)}</dd>
        </div>
        <div>
          <dt>{mode === 'compress' ? 'Архив' : 'Файл'}</dt>
          <dd>{formatBytes(result.resultSize)}</dd>
        </div>
        <div>
          <dt>Коэффициент</dt>
          <dd>{formatPercent(result.compressionRatio)}</dd>
        </div>
        <div>
          <dt>{mode === 'compress' ? 'Экономия' : 'Изменение'}</dt>
          <dd>{mode === 'compress' ? formatPercent(saved) : formatPercent(result.compressionRatio - 1)}</dd>
        </div>
      </dl>

      <div className="result-tags">
        <span>{result.maxCodeLength} бит</span>
        {result.passwordProtected && (
          <span>
            <ShieldCheck size={15} />
            Пароль
          </span>
        )}
      </div>
    </section>
  );
}
