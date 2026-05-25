# Локальная разработка

## Требования

- Node.js 20+
- npm 10+
- CMake 3.20+
- C++20 компилятор (MSVC, GCC или Clang)

## Установка (monorepo)

```powershell
cd D:\Files\TimeTableGenerator
npm install
```

```bash
cd /path/to/TimeTableGenerator
npm install
```

## Shared contracts

```powershell
npm run validate:contracts
# или
.\scripts\validate-contracts.ps1
```

## Web (Next.js + Prisma + Ant Design)

```powershell
cd apps/web
copy .env.example .env
npm run prisma:migrate
npm run dev
```

Откройте http://localhost:3000

Страницы-заглушки:

- `/` — dashboard
- `/files`
- `/audit`
- `/manual-review`
- `/solver-runs`

### Prisma

```powershell
npm run prisma:migrate -w web
npm run prisma:studio -w web
```

SQLite файл: `apps/web/prisma/dev.db` (в `.gitignore`).

## Solver (C++ stub)

### Сборка

```powershell
cmake -S apps/solver -B apps/solver/build
cmake --build apps/solver/build --config Release
```

### Запуск

```powershell
.\scripts\run-solver-dev.ps1
```

Или вручную:

```powershell
apps\solver\build\Release\schedule_solver.exe `
  --input data\samples\synthetic-small\input.json `
  --output tmp\solver-output.json `
  --mode diagnostic
```

## Что пока не делаем локально

- Production deploy
- HTTP API для solver
- Реальный импорт XLSX из UI
