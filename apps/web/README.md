# Web (Next.js)

Управляющий слой платформы: справочники, audit, manual review, подготовка JSON для C++ solver.

**Solver не имеет доступа к этой БД.**

## Стек

- Next.js 15 (App Router)
- TypeScript
- Ant Design
- Prisma + SQLite (dev)

## Быстрый старт

```powershell
cd apps/web
copy .env.example .env
npm run prisma:migrate
npm run dev
```

Из корня monorepo:

```powershell
npm install
npm run prisma:migrate
npm run dev:web
```

## Страницы

| Путь | Назначение |
|------|------------|
| `/` | Dashboard |
| `/files` | Список `DataSourceFile` |
| `/audit` | Список `DataAuditRun` |
| `/manual-review` | Таблица `ManualDecision` |
| `/solver-runs` | Список `SolverRun` |

## Prisma модели

Только управляющий слой — без детальной solver-доменной модели в БД.

## TODO

- Загрузка файлов и storage
- Audit pipeline для XLSX
- Экспорт `SolverInput` JSON
- Запуск `schedule_solver` и сохранение `SolverRun` / `SolverArtifact`
- Авторизация

## Границы

- Не смешивать Prisma-модели с C++ структурами solver.
- Нормализованный контракт — `packages/shared-contracts`.
