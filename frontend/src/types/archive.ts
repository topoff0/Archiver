export type ArchiveMode = 'compress' | 'decompress';

export interface ArchiveResult {
  blob: Blob;
  fileName: string;
  originalSize: number;
  resultSize: number;
  compressionRatio: number;
  maxCodeLength: number;
  passwordProtected: boolean;
}
