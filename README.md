# schedule-solver-platform

Monorepo для генерации расписаний: **web** управляет данными, **solver** (.NET CLI, в разработке) работает по JSON, **shared-contracts** фиксирует границу обмена.

## Структура

```
schedule-solver-platform/
  apps/
    web/                 # Next.js + Prisma + Ant Design
    solver/              # .NET 10 solver (ScheduleSolver.slnx)
  packages/
    shared-contracts/    # JSON Schema + TypeScript types
  data/
    samples/
      synthetic-small/
        input.json
  docs/
    architecture.md
    local-development.md
    solver-contract.md
    data-boundary.md
  scripts/
    validate-contracts.ps1 | .sh
```

## Границы компонентов

| Компонент | Может | Не может |
|-----------|-------|----------|
| **web** | БД (Prisma), UI, audit, export JSON | CP-SAT внутри web, чтение XLSX в solver |
| **solver** | JSON in/out, CLI (.NET) | БД, UI, Prisma, XLSX |
| **shared-contracts** | Схемы и типы | Бизнес-логика |

## Быстрый старт

### Установка

```powershell
cd D:\Files\TimeTableGenerator
npm install
```

### Web

```powershell
cd apps/web
copy .env.example .env
npm run prisma:migrate
npm run dev
```

http://localhost:3000

### Контракты

```powershell
npm run validate:contracts
```

Подробнее: [docs/local-development.md](docs/local-development.md)

## Handoff-данные (локально, не в git)

`data/solver_agent_full_handoff_v2/` — candidate JSON, правила (ПД). См. [docs/agent-handoff.md](docs/agent-handoff.md).

## Что реализовано

- Monorepo, web-заглушки, Prisma (SQLite)
- Shared contracts (JSON Schema + TS)
- Документация границ

## Solver

```powershell
.\scripts\run-solver.ps1 -Mode validate
cd apps/solver && dotnet test ScheduleSolver.slnx -c Release
```

## Что впереди

- CP-SAT solve (phase 1)
- Экспорт/запуск solver из web

## Лицензия

MIT — см. [LICENSE](LICENSE).
