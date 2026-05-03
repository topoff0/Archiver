import { LockKeyhole } from 'lucide-react';
import type { ArchiveMode } from '../types/archive';

interface PasswordOptionsProps {
  mode: ArchiveMode;
  enabled: boolean;
  password: string;
  onEnabledChange: (value: boolean) => void;
  onPasswordChange: (value: string) => void;
}

export function PasswordOptions({
  mode,
  enabled,
  password,
  onEnabledChange,
  onPasswordChange,
}: PasswordOptionsProps) {
  const isDecompress = mode === 'decompress';

  return (
    <div className="password-block">
      <label className="checkbox-row">
        <input
          type="checkbox"
          checked={enabled}
          onChange={(event) => onEnabledChange(event.target.checked)}
        />
        <span>{isDecompress ? 'Указать пароль' : 'Защитить паролем'}</span>
      </label>

      {enabled && (
        <label className="password-field">
          <LockKeyhole size={18} />
          <input
            type="password"
            value={password}
            placeholder={isDecompress ? 'Пароль архива' : 'Новый пароль'}
            onChange={(event) => onPasswordChange(event.target.value)}
          />
        </label>
      )}
    </div>
  );
}
