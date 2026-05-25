# Архитектура schedule-solver-platform

## Компоненты

| Компонент | Роль | Технологии |
|-----------|------|------------|
| `apps/web` | Управляющий слой: справочники, audit, manual review, экспорт JSON | Next.js, Prisma, Ant Design |
| `apps/solver` | Stateless CLI: JSON in → JSON out | **.NET** + OR-Tools (NuGet), *запланировано* |
| `packages/shared-contracts` | Граница контрактов solver input/output | JSON Schema, TypeScript |

## Главная граница

```
[ Web + SQLite/DB ]  --export-->  input.json  --CLI-->  [ .NET solver ]  ---->  output.json
                                      ^                          |
                                      |                          v
                              shared-contracts             import to DB
```

- **Web** владеет данными, файлами, ручными решениями и подготовкой нормализованного input.
- **Solver** не имеет доступа к БД, Prisma, Next.js и XLSX.
- **Shared contracts** — единственное место описания формата обмена.

## Стек solver

**C# console** и NuGet `Google.OrTools` — CP-SAT, Windows-first. См. `docs/superpowers/specs/2026-05-26-csharp-solver-design.md`.

## Solver: stateless

- Отдельный процесс (планируется `dotnet run` / опубликованный exe).
- Вход/выход — только JSON-файлы.
- Diagnostic-first: нарушения правил в отчёте, а не «немой» INFEASIBLE.

## Что пока не реализовано

- Код .NET solver
- Вызов solver из UI
- HTTP-обёртка
- Импорт Excel в web (полный pipeline)
