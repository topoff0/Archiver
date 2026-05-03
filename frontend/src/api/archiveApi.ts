import type { ArchiveMode, ArchiveResult } from '../types/archive';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

export interface ArchiveRequest {
  mode: ArchiveMode;
  file: File;
  maxCodeLength: number;
  password?: string;
}

export async function processArchive(request: ArchiveRequest): Promise<ArchiveResult> {
  const formData = new FormData();
  formData.append('file', request.file);

  if (request.mode === 'compress') {
    formData.append('maxCodeLength', String(request.maxCodeLength));
  }

  if (request.password) {
    formData.append('password', request.password);
  }

  const response = await fetch(`${apiBaseUrl}/api/archive/${request.mode}`, {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    throw new Error(await readError(response));
  }

  const blob = await response.blob();

  return {
    blob,
    fileName: readFileName(response),
    originalSize: readNumberHeader(response, 'X-Original-Size'),
    resultSize: readNumberHeader(response, 'X-Result-Size'),
    compressionRatio: readNumberHeader(response, 'X-Compression-Ratio'),
    maxCodeLength: readNumberHeader(response, 'X-Max-Code-Length'),
    passwordProtected: response.headers.get('X-Password-Protected') === 'True',
  };
}

async function readError(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { message?: string };
    return body.message ?? 'Не удалось обработать файл.';
  } catch {
    return 'Не удалось обработать файл.';
  }
}

function readNumberHeader(response: Response, name: string): number {
  return Number(response.headers.get(name) ?? 0);
}

function readFileName(response: Response): string {
  const headerName = response.headers.get('X-File-Name');

  if (headerName) {
    return decodeURIComponent(headerName);
  }

  const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = disposition.match(/filename="?([^"]+)"?/i);
  return match?.[1] ?? 'result.bin';
}
