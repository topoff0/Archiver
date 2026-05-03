interface CompressionOptionsProps {
  maxCodeLength: number;
  onChange: (value: number) => void;
}

export function CompressionOptions({ maxCodeLength, onChange }: CompressionOptionsProps) {
  return (
    <div className="field-group">
      <div className="field-header">
        <label htmlFor="maxCodeLength">Максимальная длина кода</label>
        <output htmlFor="maxCodeLength">{maxCodeLength} бит</output>
      </div>
      <div className="range-row">
        <input
          id="maxCodeLength"
          type="range"
          min="1"
          max="32"
          value={maxCodeLength}
          onChange={(event) => onChange(Number(event.target.value))}
        />
        <input
          className="number-input"
          type="number"
          min="1"
          max="32"
          value={maxCodeLength}
          onChange={(event) => onChange(clamp(Number(event.target.value)))}
        />
      </div>
    </div>
  );
}

function clamp(value: number): number {
  if (Number.isNaN(value)) {
    return 32;
  }

  return Math.min(32, Math.max(1, value));
}
