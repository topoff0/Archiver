# Archiver

Веб-приложение для сжатия и распаковки файлов без потерь собственным форматом `.huff`.
Backend реализован на .NET 8 по Clean Architecture, frontend - React + Vite + pnpm.

## Возможности

- Сжатие последовательности байтов алгоритмом Хаффмана.
- Побитовая запись и чтение закодированного потока.
- Собственный формат архива `.huff` с таблицей канонических кодов.
- Ограничение максимальной длины кода от 1 до 32 бит.
- Опциональная защита паролем через AES-GCM и PBKDF2.
- REST API с ограничением файла 100 МБ.
- React-интерфейс с выбором режима, загрузкой файла и сводкой результата.

## Запуск backend

```bash
cd backend
dotnet run --project src/Archiver.Api
```

API будет доступно на `http://localhost:8080`.

## Запуск frontend

```bash
cd frontend
pnpm install
pnpm dev
```

Интерфейс будет доступен на `http://localhost:5173`.

## Запуск через Docker

```bash
docker compose up --build
```

После запуска:

- frontend: `http://localhost:5173`
- backend API: `http://localhost:8080`

## API

### `POST /api/archive/compress`

`multipart/form-data`:

- `file` - исходный файл.
- `maxCodeLength` - максимальная длина кода, по умолчанию `32`.
- `password` - необязательный пароль.

### `POST /api/archive/decompress`

`multipart/form-data`:

- `file` - `.huff` архив.
- `password` - пароль, если архив был защищен.

Ответ возвращает итоговый файл. Метрики передаются в заголовках:

- `X-Original-Size`
- `X-Result-Size`
- `X-Compression-Ratio`
- `X-Max-Code-Length`
- `X-Password-Protected`
- `X-File-Name`
