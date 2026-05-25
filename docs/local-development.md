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

**Пока не собирается.** Следующий шаг: .NET solution в `apps/solver` (см. design spec).

См. `apps/solver/README.md`.

## Handoff (локально)

`data/solver_agent_full_handoff_v2/` в `.gitignore` (ПД). Не коммитить.

## Что пока не делаем

- Production deploy
- HTTP API для solver
- Импорт XLSX из UI
