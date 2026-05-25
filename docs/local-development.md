# Локальная разработка

## Требования

- Node.js 20+
- npm 10+
- Для будущего solver: .NET SDK 8+ (пока не обязателен)

## Monorepo

```powershell
cd D:\Files\TimeTableGenerator
npm install
```

## Shared contracts

```powershell
npm run validate:contracts
```

## Web

```powershell
cd apps/web
copy .env.example .env
npm run prisma:migrate
npm run dev
```

http://localhost:3000

Страницы: `/`, `/files`, `/audit`, `/manual-review`, `/solver-runs`

### Prisma

```powershell
npm run prisma:migrate -w web
npm run prisma:studio -w web
```

## Solver

```powershell
cd D:\Files\TimeTableGenerator\apps\solver
dotnet test ScheduleSolver.slnx -c Release

cd D:\Files\TimeTableGenerator
.\scripts\run-solver.ps1 -Mode validate
```

См. `apps/solver/README.md` и design spec `docs/superpowers/specs/2026-05-26-csharp-solver-design.md`.

## Handoff (локально)

`data/solver_agent_full_handoff_v2/` в `.gitignore` (ПД). Не коммитить.

## Что пока не делаем

- Production deploy
- HTTP API для solver
- Импорт XLSX из UI
